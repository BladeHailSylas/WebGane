using System;
using System.Collections.Generic;
using SkillInterfaces;
using UnityEngine;

namespace Intents
{
        /// <summary>
        ///     IntentRouter는 고전적인 IntentOrchestrator를 경량화한 순수 C# 버전입니다.
        ///     <para>MonoBehaviour 의존성을 제거하고, 외부 시스템이 틱을 명시적으로 구동하도록 설계했습니다.</para>
        /// </summary>
        public sealed class IntentRouter : IIntentSink
        {
                /// <summary>
                ///     틱 처리 결과를 요약한 구조체입니다. 디버그 UI나 리플레이 검증에 활용하십시오.
                /// </summary>
                public struct TickReport
                {
                        public int Tick;
                        public int Dequeued;
                        public int Executed;
                        public int BlockedByDepth;
                        public int BlockedByDedup;
                        public int BlockedByGuard;
                        public int ValidationFailed;
                        public int Rescheduled;
                        public int RemainingGlobal;
                        public int RemainingImmediate;
                }

                /// <summary>
                ///     라우터 설정 값입니다.
                /// </summary>
                public readonly struct Config
                {
                        public int MatchSeed { get; init; }
                        public int MaxChainDepth { get; init; }
                        public int PerTickBudget { get; init; }
                        public bool EnablePerTickDedup { get; init; }

                        public static Config Default => new()
                        {
                                MatchSeed = 0x13572468,
                                MaxChainDepth = 8,
                                PerTickBudget = 64,
                                EnablePerTickDedup = true,
                        };
                }

                /// <summary>
                ///     Intent 실행을 담당할 델리게이트입니다.
                /// </summary>
                public delegate ExecutionResult IntentExecutor(CastIntent intent, in IntentExecutionContext context, Transform target);

                /// <summary>
                ///     대상 해석을 위한 콜백입니다.
                /// </summary>
                public delegate Transform TargetResolver(CastIntent intent, in IntentExecutionContext context);

                private readonly Config _config;
                private readonly IntentExecutor _executor;
                private readonly TargetResolver _targetResolver;

                private readonly Queue<CastIntent> _globalQueue = new();
                private readonly Queue<CastIntent> _immediateQueue = new();
                private readonly List<CastIntent> _tickBuffer = new();
                private readonly List<CastIntent> _followUpBuffer = new();
                private readonly HashSet<string> _guardSet = new();
                private readonly HashSet<string> _tickDedup = new();
                private readonly Dictionary<EntityId, int> _busyUntilTick = new();
                private readonly Dictionary<EntityId, int> _cooldownUntilTick = new();

                private int _tick;
                private int _bufferIndex;
                private bool _processingImmediate;

                /// <summary>
                ///     외부에서 주입 가능한 IntentRouter 생성자입니다.
                /// </summary>
                public IntentRouter(Config config, IntentExecutor executor, TargetResolver targetResolver = null)
                {
                        _config = config;
                        _executor = executor ?? throw new ArgumentNullException(nameof(executor));
                        _targetResolver = targetResolver;
                }

                /// <summary>
                ///     새로운 Intent를 큐에 등록합니다.
                /// </summary>
                public void Enqueue(CastIntent intent)
                {
                        if (intent == null) throw new ArgumentNullException(nameof(intent));
                        if (intent.ChainDepth > _config.MaxChainDepth)
                        {
                                Debug.LogWarning($"Intent {intent} MaxChainDepth({_config.MaxChainDepth}) 초과로 폐기되었습니다.");
                                return;
                        }
                        intent.ScheduledTick = Math.Max(intent.ScheduledTick, _tick);
                        _globalQueue.Enqueue(intent);
                }

                /// <summary>
                ///     한 틱을 처리하고 보고서를 반환합니다.
                /// </summary>
                public TickReport ProcessTick(int worldTick)
                {
                        _tick = worldTick;
                        PrepareTickBuffer();
                        CastScope.Reset();
                        _tickDedup.Clear();
                        _bufferIndex = 0;
                        var report = new TickReport { Tick = _tick };

                        int budget = _config.PerTickBudget <= 0 ? int.MaxValue : _config.PerTickBudget;
                        while (budget-- > 0 && TryFetchIntent(out var intent))
                        {
                                report.Dequeued++;

                                if (intent.ChainDepth > _config.MaxChainDepth)
                                {
                                        report.BlockedByDepth++;
                                        continue;
                                }

                                if (_config.EnablePerTickDedup && !string.IsNullOrEmpty(intent.DedupKey))
                                {
                                        if (!_tickDedup.Add(intent.DedupKey))
                                        {
                                                report.BlockedByDedup++;
                                                continue;
                                        }
                                }

                                if (intent.RespectBusyCooldown && (IsActorBusy(intent.OriginActorId) || IsActorOnCooldown(intent.OriginActorId)))
                                {
                                        intent.ScheduledTick = _tick + 1;
                                        _globalQueue.Enqueue(intent);
                                        report.Rescheduled++;
                                        continue;
                                }

                                if (!HandleTiming(intent))
                                {
                                        continue;
                                }

                                var context = BuildContext(intent);
                                if (!Validate(intent))
                                {
                                        report.ValidationFailed++;
                                        continue;
                                }

                                if (!BeginCost(intent, context))
                                {
                                        report.BlockedByGuard++;
                                        continue;
                                }

                                _followUpBuffer.Clear();
                                var target = ResolveTarget(intent, context);
                                var execResult = Execute(intent, context, target);
                                ScheduleFollowUps(intent, execResult);
                                Finalize(intent);
                                report.Executed++;
                        }

                        CarryOverTickBuffer();
                        CarryOverImmediate();
                        report.RemainingGlobal = _globalQueue.Count;
                        report.RemainingImmediate = _immediateQueue.Count;
                        return report;
                }

                /// <summary>
                ///     상태를 초기화합니다.
                /// </summary>
                public void Reset()
                {
                        _globalQueue.Clear();
                        _immediateQueue.Clear();
                        _tickBuffer.Clear();
                        _followUpBuffer.Clear();
                        _guardSet.Clear();
                        _tickDedup.Clear();
                        _busyUntilTick.Clear();
                        _cooldownUntilTick.Clear();
                        _bufferIndex = 0;
                        _tick = 0;
                        _processingImmediate = false;
                }

                /// <summary>
                ///     액터 Busy 상태를 조회합니다.
                /// </summary>
                public bool IsActorBusy(EntityId actorId)
                {
                        if (!actorId.IsValid) return false;
                        return _busyUntilTick.TryGetValue(actorId, out var until) && until > _tick;
                }

                /// <summary>
                ///     액터 쿨다운 상태를 조회합니다.
                /// </summary>
                public bool IsActorOnCooldown(EntityId actorId)
                {
                        if (!actorId.IsValid) return false;
                        return _cooldownUntilTick.TryGetValue(actorId, out var until) && until > _tick;
                }

                void IIntentSink.AddIntent(CastIntent intent)
                {
                        if (intent != null)
                        {
                                _followUpBuffer.Add(intent);
                        }
                }

                private void PrepareTickBuffer()
                {
                        _tickBuffer.Clear();
                        int count = _globalQueue.Count;
                        for (int i = 0; i < count; i++)
                        {
                                var intent = _globalQueue.Dequeue();
                                if (intent.ScheduledTick > _tick)
                                {
                                        _globalQueue.Enqueue(intent);
                                        continue;
                                }
                                _tickBuffer.Add(intent);
                        }
                        _tickBuffer.Sort((a, b) => b.PriorityLevel.CompareTo(a.PriorityLevel));
                }

                private bool TryFetchIntent(out CastIntent intent)
                {
                        if (_immediateQueue.Count > 0)
                        {
                                intent = _immediateQueue.Dequeue();
                                _processingImmediate = true;
                                return true;
                        }

                        _processingImmediate = false;
                        if (_bufferIndex < _tickBuffer.Count)
                        {
                                intent = _tickBuffer[_bufferIndex++];
                                return true;
                        }

                        intent = null;
                        return false;
                }

                private bool HandleTiming(CastIntent intent)
                {
                        if (intent.Timing == IntentTiming.Periodic)
                        {
                                if (intent.IntervalTicks <= 0)
                                {
                                        Debug.LogWarning("Periodic Intent에 IntervalTicks가 0입니다.");
                                }
                                else
                                {
                                        var next = intent.Clone();
                                        next.ScheduledTick = _tick + intent.IntervalTicks;
                                        _globalQueue.Enqueue(next);
                                }
                        }
                        return true;
                }

                private IntentExecutionContext BuildContext(CastIntent intent)
                {
                        var rngSeed = _config.MatchSeed ^ _tick ^ intent.RootCastId;
                        return new IntentExecutionContext(intent, _tick, new System.Random(rngSeed));
                }

                private bool Validate(CastIntent intent)
                {
                        if (intent.Mechanism == null)
                        {
                                Debug.LogWarning($"{intent} 메커니즘이 없습니다.");
                                return false;
                        }

                        if (intent.Param == null)
                        {
                                Debug.LogWarning($"{intent} 파라미터가 없습니다.");
                                return false;
                        }

                        if (!intent.Mechanism.ParamType.IsInstanceOfType(intent.Param))
                        {
                                Debug.LogError($"Intent {intent.RootCastId} ParamType mismatch");
                                return false;
                        }

                        return true;
                }

                private bool BeginCost(CastIntent intent, IntentExecutionContext context)
                {
                        if (!string.IsNullOrEmpty(intent.GuardKey))
                        {
                                if (!_guardSet.Add(intent.GuardKey))
                                {
                                        return false;
                                }
                        }

                        _busyUntilTick[intent.OriginActorId] = _tick + 1;

                        if (intent.Param is ICooldownParam cooldown)
                        {
                                int cdTicks = Mathf.CeilToInt(cooldown.Cooldown * 60f);
                                _cooldownUntilTick[intent.OriginActorId] = _tick + cdTicks;
                        }

                        return true;
                }

                private Transform ResolveTarget(CastIntent intent, IntentExecutionContext context)
                {
                        if (_targetResolver != null)
                        {
                                var resolved = _targetResolver(intent, context);
                                if (resolved != null)
                                {
                                        return resolved;
                                }
                        }

                        switch (intent.TargetRequest.Policy)
                        {
                                case TargetPolicy.SameAsCast:
                                        return intent.TargetRequest.ExplicitActor;
                                case TargetPolicy.UseHitTarget:
                                case TargetPolicy.PickClosest:
                                        break; // TODO: 세계 데이터 기반 구현 필요
                        }

                        return intent.TargetRequest.ExplicitActor;
                }

                private ExecutionResult Execute(CastIntent intent, IntentExecutionContext context, Transform target)
                {
                        try
                        {
                                using (CastScope.Enter(this, intent, context, target))
                                {
                                        return _executor(intent, context, target);
                                }
                        }
                        catch (Exception ex)
                        {
                                Debug.LogError($"Intent 실행 중 예외: {ex}");
                                return new ExecutionResult(false, Array.Empty<CastIntent>());
                        }
                }

                private void ScheduleFollowUps(CastIntent parent, ExecutionResult result)
                {
                        if (result.FollowUps != null)
                        {
                                foreach (var follow in result.FollowUps)
                                {
                                        EnqueueFollowUp(parent, follow);
                                }
                        }

                        if (_followUpBuffer.Count > 0)
                        {
                                foreach (var follow in _followUpBuffer)
                                {
                                        EnqueueFollowUp(parent, follow);
                                }
                                _followUpBuffer.Clear();
                        }
                }

                private void EnqueueFollowUp(CastIntent parent, CastIntent follow)
                {
                        if (follow == null) return;
                        if (follow.ChainDepth > _config.MaxChainDepth)
                        {
                                Debug.LogWarning($"FollowUp Depth 초과: {follow.ChainDepth} > {_config.MaxChainDepth}");
                                return;
                        }

                        follow.ScheduledTick = _processingImmediate ? _tick : parent.ScheduledTick;
                        if (follow.Timing == IntentTiming.Immediate)
                        {
                                _immediateQueue.Enqueue(follow);
                        }
                        else
                        {
                                if (follow.Timing == IntentTiming.Delayed && follow.DelayTicks > 0)
                                {
                                        follow.ScheduledTick = _tick + follow.DelayTicks;
                                }
                                _globalQueue.Enqueue(follow);
                        }
                }

                private void Finalize(CastIntent intent)
                {
                        if (!string.IsNullOrEmpty(intent.GuardKey))
                        {
                                _guardSet.Remove(intent.GuardKey);
                        }

                        _busyUntilTick.Remove(intent.OriginActorId);
                }

                private void CarryOverTickBuffer()
                {
                        for (int i = _bufferIndex; i < _tickBuffer.Count; i++)
                        {
                                var carry = _tickBuffer[i];
                                carry.ScheduledTick = _tick + 1;
                                _globalQueue.Enqueue(carry);
                        }

                        _tickBuffer.Clear();
                        _bufferIndex = 0;
                }

                private void CarryOverImmediate()
                {
                        while (_immediateQueue.Count > 0)
                        {
                                var carry = _immediateQueue.Dequeue();
                                carry.ScheduledTick = _tick + 1;
                                _globalQueue.Enqueue(carry);
                        }
                }
        }
}
