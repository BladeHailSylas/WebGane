// MoveParams.cs (REFACTOR/EXTEND)
using System.Collections.Generic;
using UnityEngine;
using SkillInterfaces;
using UnityEditor;

[System.Serializable]
public class MeleeParams : ISkillParam, IHasCooldown, IFollowUpProvider
{
    [Header("Area")]
    public float radius = 1.6f;
    [Range(0, 360)] public float angleDeg = 120f;
    public LayerMask enemyMask;

    [Header("Damage")]
    public float attack = 10f, apRatio = 0f, knockback = 0f, attackPercent = 1.0f;

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
public class MissileParams : ISkillParam, IHasCooldown, IFollowUpProvider, ITargetingData
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
    public float damage = 8f, apRatio = 0f, knockback = 0f;

    [Header("Timing")]
    public float cooldown = 0.35f;
    public float Cooldown => cooldown;

    [Header("Behavior")]
    public bool retargetOnLost = true;
    public float retargetRadius = 3f;
    [Header("Targeter")]
    public TargetMode mode; public float fallback; public Vector2 local; public float col; public float skin; public bool canpen;
    public TargetMode Mode => mode; public float FallbackRange => fallback; public Vector2 LocalOffset => local; public LayerMask WallsMask => blockerMask;
    public float CollisionRadius => col; public float AnchorSkin => skin; public bool CanPenetrate => canpen;


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
public class DashParams :
    ISkillParam, IHasCooldown, IFollowUpProvider, ITargetingData, IAnchorClearance
{
    // ── Targeting (Runner가 해석) ──────────────────────────────────────────
    [Header("Targeting (interpreted by Runner)")]
    [Tooltip("대상/조준 정책")]
    [SerializeField] TargetMode _mode = TargetMode.TowardsMovement;

    [Tooltip("비타깃(정면/커서/이동방향/오프셋)일 때 이동할 기준 거리")]
    [Min(0)][SerializeField] float _fallbackRange = 4f;

    [Tooltip("TowardsOffset 모드에서 사용할 로컬 오프셋")]
    [SerializeField] Vector2 _localOffset = Vector2.zero;

    [Tooltip("앵커 보정(벽 차단)용 레이어 마스크")]
    [SerializeField] LayerMask _wallsMask;

    [Tooltip("적 관통 허용(적은 대시를 멈추지 않음). 타깃 그 자체는 예외.")]
    [SerializeField] bool _canPenetrate = true;

    // ITargetingData
    public TargetMode Mode => _mode;                 // [RULE: Targeted/NonTargeted]
    public float FallbackRange => _fallbackRange;        // [RULE: DistanceBudget, FixedSpeed]
    public Vector2 LocalOffset => _localOffset;
    public LayerMask WallsMask => _wallsMask;            // [RULE: AnchorClamp]
    public bool CanPenetrate => _canPenetrate;         // [RULE: PenetrationPolicy]

    // ── Motion ──────────────────────────────────────────────────────────────
    [Header("Motion")]
    [Tooltip("대시 총 시간(속도 커브의 시간 기준)")]
    [Min(0.01f)] public float duration = 0.18f;

    [Tooltip("0→1 시간에 대한 속도 배율 커브(예산 소모는 Runner/메커닉에서 보정)")]
    public AnimationCurve speedCurve = AnimationCurve.Linear(0, 1, 1, 1);

    [Tooltip("벽 충돌 시 즉시 종료(거짓이면 이후 확장: 슬라이드 등)")]
    public bool stopOnWall = true; // [RULE: WallStopsDash]

    // ── Collision Volume(본체 충돌 형상) ────────────────────────────────────
    [Header("Collision Volume")]
    [Tooltip("대시 중 본체 서클 캐스트 반경(벽/적 판정 공통)")]
    [Min(0.01f)] public float radius = 0.5f;               // [RULE: CollisionOrder]

    [Tooltip("충돌면을 살짝 넘기는 여유 거리(면 재포착/끼임 방지)")]
    [Range(0.01f, 0.2f)] public float skin = 0.05f;        // [RULE: SKIN]

    // IAnchorClearance
    public float CollisionRadius => radius;                // [RULE: AnchorClamp]
    public float AnchorSkin => Mathf.Max(0.01f, skin);// [RULE: AnchorClamp]

    // ── Combat During Dash ─────────────────────────────────────────────────
    [Header("Combat During Dash")]
    [Tooltip("대시 경로에서 적을 가격할지 여부")]
    public bool dealDamage = true;                          // [RULE: DealDamage]

    [Tooltip("대시 중 적 판정 레이어")]
    public LayerMask enemyMask;

    [Tooltip("대시 1회 가격 피해량(스탯 시스템이 따로 있으면 그쪽과 합산)")]
    public float damage = 8f;

    [Tooltip("AP 비율")]
    public float apRatio = 0f;

    [Tooltip("넉백 강도(Impulse)")]
    public float knockback = 0f;

    // ── I-Frame / Status ───────────────────────────────────────────────────
    [Header("I-Frame / Status")]
    [Tooltip("대시 시작 시 무적 프레임 부여")]
    public bool grantIFrame = true;

    [Tooltip("무적 지속(보통 duration과 동일)")]
    [Min(0)] public float iFrameDuration = 0.18f;          // [RULE: IFrame]

    // ── Timing ─────────────────────────────────────────────────────────────
    [Header("Timing")]
    [Tooltip("시전 쿨다운(후속기 포함, Runner에서 최종 반영)")]
    [Min(0)] public float cooldown = 0.35f;
    public float Cooldown => cooldown;

    // ── FollowUps(대시 종료 후) ────────────────────────────────────────────
    [Header("FollowUps (OnExpire)")]
    [Tooltip("대시 종료 시 자동 시전할 후속 기술 목록(예: 원형 베기)")]
    public List<MechanicRef> onExpire = new();              // [RULE: FollowUpHook]

    public IEnumerable<(CastOrder, float, bool)> BuildFollowUps(AbilityHook hook, Transform prevTarget)
    {
        if (hook != AbilityHook.OnExpire || onExpire == null) yield break;
        foreach (var m in onExpire)
            if (m.TryBuildOrder(null, out var order))
                yield return (order, m.delay, m.respectBusyCooldown); // [RULE: FollowUpWait]
    }

    // ── Validation / UX ────────────────────────────────────────────────────
#if UNITY_EDITOR
    void OnValidate()
    {
        // [RULE: SafetyClamp] 파라미터 방어적 정규화
        duration = Mathf.Max(0.01f, duration);
        radius = Mathf.Max(0.01f, radius);
        skin = Mathf.Clamp(skin, 0.01f, 0.2f);
        iFrameDuration = Mathf.Max(0f, iFrameDuration);
        cooldown = Mathf.Max(0f, cooldown);
        _fallbackRange = Mathf.Max(0f, _fallbackRange);
    }
#endif
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