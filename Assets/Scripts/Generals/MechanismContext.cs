using UnityEngine;
using SkillInterfaces;

/// <summary>
/// SkillRunner가 메커니즘 실행 시 제공하는 컨텍스트입니다.
/// - FollowUp 실행, 훅 브로드캐스트, 대상/주문 정보 등을 한 곳에서 참조합니다.
/// - 구조체이지만 참조 필드를 보유하므로 값 복사에 유의하십시오.
/// </summary>
public readonly struct MechanismContext
{
    public Transform Owner { get; }
    public Camera Camera { get; }
    public Transform Target { get; }
    public SkillRunner Runner { get; }
    public IntentOrchestrator Orchestrator { get; }
    public CastOrder Order { get; }
    public GameEventMeta Meta { get; }
    public SkillRef SkillRef { get; }

    public bool HasTarget => Target != null;
    public string MechanismName => Order.Mech?.GetType().Name ?? "<Mechanism>";

    public MechanismContext(Transform owner, Camera camera, Transform target,
        SkillRunner runner, IntentOrchestrator orchestrator,
        CastOrder order, GameEventMeta meta, SkillRef skillRef)
    {
        Owner = owner;
        Camera = camera;
        Target = target;
        Runner = runner;
        Orchestrator = orchestrator;
        Order = order;
        Meta = meta;
        SkillRef = skillRef;
    }

    public MechanismContext WithTarget(Transform target)
        => new MechanismContext(Owner, Camera, target, Runner, Orchestrator, Order, Meta, SkillRef);

    /// <summary>
    /// AbilityHook을 전파합니다. FollowUpProvider가 설정되어 있다면 자동으로 후속 주문이 예약됩니다.
    /// </summary>
    public void EmitHook(AbilityHook hook, Transform prevTarget = null, Vector2? worldPoint = null, string debugTag = null)
    {
        var source = Order.Param;
        if (source == null) return;

        if (Orchestrator)
        {
            Orchestrator.EmitHook(hook, source, prevTarget ?? Target, worldPoint, debugTag ?? MechanismName);
        }
        else
        {
            Runner?.EmitHook(hook, prevTarget ?? Target, source, worldPoint, debugTag ?? MechanismName);
        }
    }

    /// <summary>
    /// FollowUp 주문을 직접 예약합니다. Hook 단계 외의 커스텀 로직에서 활용하십시오.
    /// </summary>
    public void ScheduleFollowUp(CastOrder order, float delay, bool respectBusyCooldown, AbilityHook reason, string debugTag = null)
    {
        if (order.Mech == null || order.Param == null) return;
        if (Orchestrator)
        {
            Orchestrator.ScheduleFollowUp(order, delay, respectBusyCooldown, reason, debugTag ?? MechanismName);
        }
        else
        {
            Runner?.ScheduleFollowUp(order, delay, respectBusyCooldown, reason, debugTag ?? MechanismName);
        }
    }

    /**
     * TODO: Owner/Camera 외에도 CombatStatContext, AnimationController 등 확장 의존성이 필요하다면
     *       해당 필드와 유틸리티 메서드를 추가하고 SkillRunner에서 값을 세팅하십시오.
     */
}
