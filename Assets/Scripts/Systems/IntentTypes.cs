using System;
using System.Collections.Generic;
using SkillInterfaces;
using UnityEngine;

namespace Intents
{
        /// <summary>
        ///     실행 타이밍 구분 값입니다. Immediate는 즉시 처리하며, Delayed는 지정된 틱 이후 실행합니다.
        ///     Periodic은 실행 후 자동으로 재등록됩니다.
        /// </summary>
        public enum IntentTiming
        {
                Immediate,
                Delayed,
                Periodic,
        }

        /// <summary>
        ///     FollowUp 훅 지점입니다. 기존 시스템의 시그니처를 유지하여 호환성을 보장합니다.
        /// </summary>
        public enum FollowUpMoment
        {
                OnExecute,
                OnApply,
                OnFinalize,
        }

        /// <summary>
        ///     대상 탐색 정책.
        /// </summary>
        public enum TargetPolicy
        {
                SameAsCast,
                UseHitTarget,
                PickClosest,
        }

        /// <summary>
        ///     대상 요청 구조체. Determinism을 위해 모든 옵션을 명시적으로 담습니다.
        /// </summary>
        [Serializable]
        public struct TargetRequest
        {
                public TargetPolicy Policy;
                public float Radius;
                public int TeamMask;
                public Vector3? ExplicitPoint;
                public Transform ExplicitActor;

                public static TargetRequest SameActor(Transform actor) => new()
                {
                        Policy = TargetPolicy.SameAsCast,
                        ExplicitActor = actor,
                        Radius = 0f,
                };
        }

        /// <summary>
        ///     Unity 뷰 계층과 Intent 사이를 연결하는 바인딩 구조체입니다.
        /// </summary>
        [Serializable]
        public struct IntentViewBinding
        {
                [Tooltip("실행 기준 Transform")]
                public Transform Owner;
                [Tooltip("카메라 참조")]
                public Camera Camera;

                public IntentViewBinding(Transform owner, Camera camera)
                {
                        Owner = owner;
                        Camera = camera;
                }

                public Transform ResolveOwner(Transform fallback)
                {
                        return Owner != null ? Owner : fallback;
                }

                public Camera ResolveCamera(Camera fallback)
                {
                        return Camera != null ? Camera : fallback;
                }
        }

        /// <summary>
        ///     CastIntent 데이터 오브젝트. Origin/Depth/Guard/Dedup를 모두 포함합니다.
        ///     IIntent 인터페이스를 구현하여 공통 처리 파이프라인과 구조를 맞춥니다.
        /// </summary>
        public sealed class CastIntent : IIntent
        {
                public enum IntentOrigin
                {
                        Root,
                        FollowUp,
                }

                public IntentOrigin Origin;
                public EntityId OriginActorId;
                public int RootCastId;
                public ushort ChainDepth;
                public ISkillMechanism Mechanism;
                public ISkillParam Param;
                public TargetRequest TargetRequest;
                public IntentTiming Timing;
                public int DelayTicks;
                public int IntervalTicks;
                public bool RespectBusyCooldown;
                public IntentViewBinding ViewBinding;
                public string GuardKey;
                public string DedupKey;
                public int PriorityLevel;
                public int ScheduledTick;
                public string SourceHook;
                public string TemplateId;

                /// <summary>
                ///     인터페이스 규격을 위한 Owner 식별자. EntityId가 유효하지 않으면 0으로 반환합니다.
                /// </summary>
                public ushort OwnerID => OriginActorId.IsValid ? OriginActorId.Value : (ushort)0;

                /// <summary>
                ///     인터페이스 규격을 위한 Intent 고유 ID. RootCastId를 그대로 노출합니다.
                /// </summary>
                public int IntentID => RootCastId;

                /// <summary>
                ///     IIntent에서 요구하는 타입 구분자. CastIntent는 항상 Cast 타입입니다.
                /// </summary>
                public IntentType Type => IntentType.Cast;

                /// <summary>
                ///     의도 생성 시점(틱)을 보관합니다. 후속 Intent에서는 부모의 값을 그대로 승계합니다.
                /// </summary>
                public ushort GeneratedTick { get; private set; }

                public static CastIntent Root(
                        EntityId actorId,
                        int rootCastId,
                        ISkillMechanism mechanism,
                        ISkillParam param,
                        TargetRequest target,
                        bool respectBusyCooldown,
                        int priorityLevel,
                        Transform originTransform,
                        Camera camera,
                        ushort generatedTick = 0)
                {
                        return new CastIntent
                        {
                                Origin = IntentOrigin.Root,
                                OriginActorId = actorId,
                                RootCastId = rootCastId,
                                ChainDepth = 0,
                                Mechanism = mechanism,
                                Param = param,
                                TargetRequest = target,
                                Timing = IntentTiming.Immediate,
                                DelayTicks = 0,
                                IntervalTicks = 0,
                                RespectBusyCooldown = respectBusyCooldown,
                                ViewBinding = new IntentViewBinding(originTransform, camera),
                                PriorityLevel = priorityLevel,
                                ScheduledTick = 0,
                                SourceHook = "<ROOT>",
                                TemplateId = "root",
                                GeneratedTick = generatedTick,
                        };
                }

                public static CastIntent FollowUp(
                        CastIntent parent,
                        string hook,
                        string templateId,
                        ISkillMechanism mechanism,
                        ISkillParam param,
                        IntentTiming timing,
                        int delayTicks,
                        bool respectBusyCooldown,
                        TargetRequest targetOverride,
                        string guardKey,
                        string dedupKey,
                        int priority)
                {
                        if (parent == null) throw new ArgumentNullException(nameof(parent));
                        return new CastIntent
                        {
                                Origin = IntentOrigin.FollowUp,
                                OriginActorId = parent.OriginActorId,
                                RootCastId = parent.RootCastId,
                                ChainDepth = parent.ChainDepth < ushort.MaxValue ? (ushort)(parent.ChainDepth + 1) : parent.ChainDepth,
                                Mechanism = mechanism,
                                Param = param,
                                TargetRequest = targetOverride,
                                Timing = timing,
                                DelayTicks = Mathf.Max(0, delayTicks),
                                IntervalTicks = parent.IntervalTicks,
                                RespectBusyCooldown = respectBusyCooldown,
                                ViewBinding = parent.ViewBinding,
                                GuardKey = guardKey,
                                DedupKey = dedupKey,
                                PriorityLevel = priority,
                                ScheduledTick = parent.ScheduledTick,
                                SourceHook = hook,
                                TemplateId = templateId,
                                GeneratedTick = parent.GeneratedTick,
                        };
                }

                public CastIntent Clone()
                {
                        return new CastIntent
                        {
                                Origin = Origin,
                                OriginActorId = OriginActorId,
                                RootCastId = RootCastId,
                                ChainDepth = ChainDepth,
                                Mechanism = Mechanism,
                                Param = Param,
                                TargetRequest = TargetRequest,
                                Timing = Timing,
                                DelayTicks = DelayTicks,
                                IntervalTicks = IntervalTicks,
                                RespectBusyCooldown = RespectBusyCooldown,
                                ViewBinding = ViewBinding,
                                GuardKey = GuardKey,
                                DedupKey = DedupKey,
                                PriorityLevel = PriorityLevel,
                                ScheduledTick = ScheduledTick,
                                SourceHook = SourceHook,
                                TemplateId = TemplateId,
                                GeneratedTick = GeneratedTick,
                        };
                }

                public override string ToString()
                {
                        return $"[Intent #{RootCastId} d={ChainDepth} {Mechanism?.GetType().Name ?? "<null>"} timing={Timing} pri={PriorityLevel}]";
                }
        }

        public interface IIntentSink
        {
                void AddIntent(CastIntent intent);
        }

        /// <summary>
        ///     Intent 실행 컨텍스트입니다.
        /// </summary>
        public readonly struct IntentExecutionContext
        {
                        public CastIntent Intent { get; }
                        public int Tick { get; }
                        public System.Random Rng { get; }
                        public Transform Owner { get; }
                        public Camera Camera { get; }

                        public IntentExecutionContext(CastIntent intent, int tick, System.Random rng)
                        {
                                Intent = intent ?? throw new ArgumentNullException(nameof(intent));
                                Tick = tick;
                                Rng = rng ?? throw new ArgumentNullException(nameof(rng));
                                Owner = intent.ViewBinding.Owner;
                                Camera = intent.ViewBinding.Camera != null ? intent.ViewBinding.Camera : Camera.main;
                        }
        }

        /// <summary>
        ///     CastScope는 전역 스택 기반으로 FollowUp을 수집합니다.
        /// </summary>
        public static class CastScope
        {
                private sealed class ScopeToken : IDisposable
                {
                        private readonly ScopeFrame _previous;
                        public ScopeToken(ScopeFrame previous) => _previous = previous;
                        public void Dispose()
                        {
                                Current = _previous;
                        }
                }

                private sealed class ScopeFrame
                {
                        public readonly IIntentSink Sink;
                        public readonly CastIntent Intent;
                        public readonly IntentExecutionContext Context;
                        public readonly Transform ResolvedTarget;

                        public ScopeFrame(IIntentSink sink, CastIntent intent, IntentExecutionContext context, Transform target)
                        {
                                Sink = sink;
                                Intent = intent;
                                Context = context;
                                ResolvedTarget = target;
                        }
                }

                [ThreadStatic] private static ScopeFrame _current;

                private static ScopeFrame Current
                {
                        get => _current;
                        set => _current = value;
                }

                public static IDisposable Enter(IIntentSink sink, CastIntent intent, IntentExecutionContext context, Transform target)
                {
                        var prev = Current;
                        Current = new ScopeFrame(sink, intent, context, target);
                        return new ScopeToken(prev);
                }

                public static void AddIntent(CastIntent intent)
                {
                        if (Current == null)
                        {
                                Debug.LogWarning("CastScope.AddIntent 호출 시 유효한 Sink가 없습니다. FollowUp이 유실됩니다.");
                                return;
                        }
                        Current.Sink.AddIntent(intent);
                }

                public static bool TryGetContext(out CastIntent intent, out IntentExecutionContext context)
                {
                        if (Current == null)
                        {
                                intent = null;
                                context = default;
                                return false;
                        }

                        intent = Current.Intent;
                        context = Current.Context;
                        return true;
                }

                public static Transform CurrentTarget => Current?.ResolvedTarget;

                public static void Reset()
                {
                        Current = null;
                }
        }

        /// <summary>
        ///     실행 결과 데이터.
        /// </summary>
        public readonly struct ExecutionResult
        {
                public readonly bool Succeeded;
                public readonly IReadOnlyList<CastIntent> FollowUps;

                public ExecutionResult(bool success, IReadOnlyList<CastIntent> followUps)
                {
                        Succeeded = success;
                        FollowUps = followUps ?? Array.Empty<CastIntent>();
                }
        }
}
