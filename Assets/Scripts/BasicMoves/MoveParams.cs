// MoveParams.cs (REFACTOR/EXTEND)
using System.Collections.Generic;
using UnityEngine;
using SOInterfaces;

[System.Serializable]
public class MeleeParams : ISkillParam, IHasCooldown, IFollowUpProvider
{
    [Header("Area")]
    public float radius = 1.6f;
    [Range(0, 360)] public float angleDeg = 120f;
    public LayerMask enemyMask;

    [Header("Damage")]
    public float attack = 10f, apRatio = 0f, knockback = 6f, attackPercent = 1.0f;

    [Header("Timing")]
    public float windup = 0.05f, recover = 0.08f, cooldown = 0.10f;
    public float Cooldown => cooldown;

    // ★ FollowUp(예: 2타)을 Param에 직접 둠 — 필요 시 인스펙터에서 설정
    public List<MechanicRef> onHit = new();
    public List<MechanicRef> onExpire = new();

    public IEnumerable<(CastOrder, float, bool)> BuildFollowUps(AbilityHook hook, Transform prevTarget)
    {
        var src = hook == AbilityHook.OnHit ? onHit :
                    hook == AbilityHook.OnExpire ? onExpire : null;
        if (src == null) yield break;
        foreach (var mref in src)
            if (mref.TryBuildOrder(prevTarget, out var order))
                yield return (order, mref.delay, mref.respectBusyCooldown);
    }
}

[System.Serializable]
public class HomingParams : ISkillParam, IHasCooldown, IFollowUpProvider
{
    [Header("Projectile")]
    public float speed = 10f;
    public float acceleration = 0f;
    public float maxTurnDegPerSec = 360f;
    public float radius = 0.2f;
    public float maxRange = 12f;
    public float maxLife = 3f;

    [Header("Collision")]
    public LayerMask enemyMask;
    public LayerMask blockerMask;

    [Header("Damage")]
    public float damage = 8f, apRatio = 0f, knockback = 5f;

    [Header("Timing")]
    public float cooldown = 0.35f;
    public float Cooldown => cooldown;

    [Header("Behavior")]
    public bool retargetOnLost = true;
    public float retargetRadius = 3f;

    // ★ FollowUp 예: 맞으면 폭발, 소멸하면 잔류 디버프…
    public List<MechanicRef> onHit = new();
    public List<MechanicRef> onExpire = new();

    public IEnumerable<(CastOrder, float, bool)> BuildFollowUps(AbilityHook hook, Transform prevTarget)
    {
        var src = hook == AbilityHook.OnHit ? onHit :
                    hook == AbilityHook.OnExpire ? onExpire : null;
        if (src == null) yield break;
        foreach (var mref in src)
            if (mref.TryBuildOrder(prevTarget, out var order))
                yield return (order, mref.delay, mref.respectBusyCooldown);
    }
}

[System.Serializable]
public class SwitchControllerParams : ISkillParam, ISwitchPolicy
{
    [Tooltip("교대 실행할 기술 목록(타깃/논타깃 혼재 가능)")]
    public List<MechanicRef> steps = new();

    [Min(0)] public int startIndex = 0;
    public bool advanceOnCast = true; // false면 OnHit 등에서 수동 전진

    // 런타임 커서(캐릭터별로 SerializeReference Param이 보관하므로 인스펙터 전역 영향 없음)
    [System.NonSerialized] int _idx = -1;

    public bool TrySelect(Transform owner, Camera cam, out CastOrder order)
    {
        order = default;
        if (steps == null || steps.Count == 0) return false;

        if (_idx < 0) _idx = Mathf.Clamp(startIndex, 0, steps.Count - 1);
        int cur = _idx;
        if (advanceOnCast) _idx = (_idx + 1) % steps.Count;

        var mref = steps[cur];
        return mref.TryBuildOrder(prevTarget: null, out order);
    }

    // 필요하면 OnHit에서 수동 전진할 수 있도록 헬퍼 제공
    public void AdvanceExplicit()
    {
        if (steps == null || steps.Count == 0) return;
        _idx = (_idx + 1) % steps.Count;
    }
}