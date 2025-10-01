using System;
using System.Collections.Generic;
using UnityEngine;
using SkillInterfaces;

namespace Intents
{
    /// <summary>
    ///     캐스트 타이밍 종류. Periodic은 Interval을 통해 재스케줄되며 Immediate는 동틱 꼬리 큐에 적재됩니다.
    /// </summary>
    public enum IntentTiming
    {
        Immediate,
        Delayed,
        Periodic,
    }

    /// <summary>
    ///     FollowUp 템플릿이 실행되는 지점. Begin/Apply/Finalize 훅을 확장할 때 사용합니다.
    /// </summary>
    public enum FollowUpMoment
    {
        OnExecute,
        OnApply,
        OnFinalize,
    }

    /// <summary>
    ///     대상 탐색 정책. SameAsCast는 기존 Context를 재사용합니다.
    /// </summary>
    public enum TargetPolicy
    {
        SameAsCast,
        UseHitTarget,
        PickClosest,
    }

    /// <summary>
    ///     대상 요청 구조체. Determinism을 위해 모든 입력은 명시적으로 담습니다.
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
    ///     FollowUp 템플릿. Mechanism/Param 순서를 유지하여 Determinism을 보장합니다.
    /// </summary>
    [Serializable]
    public struct FollowUpTemplate
    {
        public FollowUpMoment When;
        public int Order;
        public IntentTiming Timing;
        public TargetPolicy TargetPolicy;
        public bool RespectBusyCooldown;
        public string GuardKey;
        public string DedupKey;
        public int PriorityLevel;
        public ISkillMechanic Mechanism;
        public ISkillParam Param;
        public TargetRequest TargetOverride;
    }

    /// <summary>
    ///     CastIntent 데이터 오브젝트. Origin/Depth/Guard/Dedup를 모두 포함합니다.
    /// </summary>
    public sealed class CastIntent
    {
        public enum IntentOrigin
        {
            Root,
            FollowUp,
        }

        public IntentOrigin Origin;
        public int OriginActorId;
        public int RootCastId;
        public int ChainDepth;
        public ISkillMechanic Mechanism;
        public ISkillParam Param;
        public TargetRequest TargetRequest;
        public IntentTiming Timing;
        public int DelayTicks;
        public int IntervalTicks;
        public bool RespectBusyCooldown;
        public Transform OriginTransform;
        public Camera Camera;
        public string TargetRequestSource;
        public string GuardKey;
        public string DedupKey;
        public int PriorityLevel;
        public int ScheduledTick;
        public string SourceHook;
        public string TemplateId;
        public Guid InternalId = Guid.NewGuid();

        public static CastIntent Root(
            int actorId,
            int rootCastId,
            ISkillMechanic mechanism,
            ISkillParam param,
            TargetRequest target,
            bool respectBusyCooldown,
            int priorityLevel,
            Transform originTransform,
            Camera camera)
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
                RespectBusyCooldown = respectBusyCooldown,
                PriorityLevel = priorityLevel,
                OriginTransform = originTransform,
                Camera = camera,
                SourceHook = "<ROOT>",
                TemplateId = "root",
            };
        }

        public static CastIntent FollowUp(
            CastIntent parent,
            string hook,
            string templateId,
            ISkillMechanic mechanism,
            ISkillParam param,
            IntentTiming timing,
            int delayTicks,
            bool respectBusyCooldown,
            TargetRequest targetOverride,
            string guardKey,
            string dedupKey,
            int priority)
        {
            return new CastIntent
            {
                Origin = IntentOrigin.FollowUp,
                OriginActorId = parent.OriginActorId,
                RootCastId = parent.RootCastId,
                ChainDepth = parent.ChainDepth + 1,
                Mechanism = mechanism,
                Param = param,
                TargetRequest = targetOverride,
                Timing = timing,
                DelayTicks = Mathf.Max(0, delayTicks),
                RespectBusyCooldown = respectBusyCooldown,
                OriginTransform = parent.OriginTransform,
                Camera = parent.Camera,
                GuardKey = guardKey,
                DedupKey = dedupKey,
                PriorityLevel = priority,
                SourceHook = hook,
                TemplateId = templateId,
            };
        }

        public CastIntent Clone()
        {
            var clone = (CastIntent)MemberwiseClone();
            clone.InternalId = Guid.NewGuid();
            return clone;
        }

        public override string ToString()
        {
            return $"[Intent #{RootCastId} d={ChainDepth} {Mechanism?.GetType().Name ?? "<null>"} timing={Timing} prio={PriorityLevel}]";
        }
    }

    /// <summary>
    ///     FollowUp 스코프용 Sink 인터페이스. 실행 중인 메커니즘이 FollowUp을 수집할 때 사용합니다.
    /// </summary>
    public interface IIntentSink
    {
        void AddIntent(CastIntent intent);
    }

    /// <summary>
    ///     CastScope는 전역 스택 기반으로 동작하여 메커니즘 코드가 의존성을 알지 못해도 FollowUp을 수집할 수 있게 합니다.
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
            public readonly CastContext Context;
            public readonly Transform ResolvedTarget;

            public ScopeFrame(IIntentSink sink, CastIntent intent, CastContext context, Transform target)
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

        public static IDisposable Enter(IIntentSink sink, CastIntent intent, CastContext context, Transform target)
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

        public static bool TryGetContext(out CastIntent intent, out CastContext context)
        {
            if (Current == null)
            {
                intent = null;
                context = null;
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
    ///     실행 결과 데이터. Apply/Schedule 단계에서 활용됩니다.
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
