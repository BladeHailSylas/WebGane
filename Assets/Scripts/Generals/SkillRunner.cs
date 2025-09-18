using ActInterfaces;
using SkillInterfaces;
using System.Collections;
using UnityEngine;
using static EventBus;
using static GameEventMetaFactory;
using static TargetAnchorUtil2D;

public class SkillRunner : MonoBehaviour, ISkillRunner
{
    [SerializeField] ISkillMechanic mech;
    [SerializeReference] ISkillParam param;
    Camera cam;
    bool busy; float cd;

    public bool IsBusy => busy;
    public bool IsOnCooldown => cd > 0f;

    void Awake() { cam = Camera.main; }
    void Update() { if (cd > 0f) cd -= Time.deltaTime; }

    // 슬롯의 기본 스킬 시전 (입력에서 호출)
    public void TryCast()
    {
        if (busy || cd > 0f || mech == null || param == null) return;

        // (A) 스위치 정책이 있으면 먼저 주문서 선택 시도 → 성공하면 그걸 시전
        if (param is ISwitchPolicy sp && sp.TrySelect(transform, cam, out var switched))
        {
            Schedule(switched, 0f, respectBusyCooldown: true);
            return;
        }

        // (B) 평소처럼 슬롯의 메커닉을 시전
        Schedule(new CastOrder(mech, param), 0f, true);
    }

    // === 표준 스케줄 API (FollowUp/일반/스위치 모두 동일 경로) ===
    public void Schedule(CastOrder order, float delay, bool respectBusyCooldown)
    {
        StartCoroutine(CoSchedule(order, delay, respectBusyCooldown));
    }

    IEnumerator CoSchedule(CastOrder order, float delay, bool respect)
    {
        // [RULE: FollowUpWait] 바쁨/쿨다운이면 드롭하지 말고 ‘대기 후 실행’ (콤보 일관)
        while (respect && (busy || cd > 0f)) yield return null;

        if (delay > 0f) yield return new WaitForSeconds(delay);
        yield return CoCast(order);
    }

    IEnumerator CoCast(CastOrder order)
    {
        busy = true;

        // [RULE: Events] 시작 이벤트는 Runner 단일 경로에서 발행
        var meta = Create(transform, channel: "combat");
        var skillRef = new SkillRef(order.Mech as Object);
        Transform target = order.TargetOverride;
        Publish(new CastStarted(meta, skillRef, order.Param, transform, target));

        if (order.Mech is ITargetedMechanic tgt)
        {
            bool createdAnchor = false;   // [RULE: AnchorLifecycle] Runner가 만든 앵커는 Runner가 책임지고 정리
            Transform t = order.TargetOverride;

            if (t == null)
            {
                Vector3 desired = default;
                bool needEnemy = true;
                float rad = 0f, skin = 0.05f;
                LayerMask walls = 0;

                if (order.Param is ITargetingData td)
                {
                    walls = td.WallsMask;
                    rad = td.CollisionRadius;
                    skin = Mathf.Max(0.01f, td.AnchorSkin);

                    switch (td.Mode)
                    {
                        case TargetMode.TowardsEnemy:
                            // [RULE: TargetedSelect] 최대 사거리 내의 적을 대상으로 지정(실패시 중단)
                            needEnemy = true;
                            break;

                        case TargetMode.TowardsCursor:
                            // [RULE: NonTargetedCursor] 커서 방향 앵커 생성
                            needEnemy = false;
                            var cursor = CursorWorld2D(cam, transform, depthFallback: 10f);
                            desired = transform.position
                                    + (cursor - transform.position).normalized * Mathf.Max(0f, td.FallbackRange);
                            break;

                        case TargetMode.TowardsMovement:
                            // [RULE: NonTargetedMoveDir] 최근 이동 방향으로 앵커 생성
                            needEnemy = false;
                            var mv = GetMoveDirOrFacing(transform);
                            desired = transform.position + (Vector3)(mv * Mathf.Max(0f, td.FallbackRange));
                            break;

                        case TargetMode.TowardsOffset:
                            // [RULE: NonTargetedOffset] 로컬 오프셋으로 앵커 생성
                            needEnemy = false;
                            desired = transform.TransformPoint((Vector3)td.LocalOffset);
                            break;

                        /*case TargetMode.FixedForward:
                            // [RULE: NonTargetedFixedForward] 정면 고정 거리 앵커 생성
                            needEnemy = false;
                            desired = transform.position + transform.right * Mathf.Max(0f, td.FallbackRange);
                            break;*/
                    }
                }

                if (needEnemy)
                {
                    // [RULE: TargetedSelect] 타깃 획득 실패 → 즉시 종료 + 이벤트
                    var provider = GetComponent<ITargetable>() ?? GetComponentInChildren<ITargetable>();
                    if (provider == null || !provider.TryGetTarget(out t) || t == null)
                    {
                        Publish(new TargetNotFound(meta, skillRef, transform)); // 선택: 페이로드가 있다면
                        Publish(new CastEnded(meta, skillRef, transform, interrupted: true));
                        busy = false; yield break;
                    }
                }
                else
                {
                    // [RULE: AnchorClamp] 벽 앞까지 보정 + 최소거리 보장
                    var clamped = ResolveReachablePoint2D(transform.position, desired, walls, rad, skin);
                    var v = (clamped - transform.position);
                    if (v.sqrMagnitude < 0.0001f)
                        clamped = transform.position + transform.right * Mathf.Max(0.5f, rad + skin);

                    // [RULE: AnchorCreate] 앵커 생성은 Runner 책임
                    t = TargetAnchorPool.Acquire(clamped);
                    createdAnchor = true;
                }
            }

            Publish(new TargetAcquired(meta, skillRef, transform, t)); // 선택: 타깃 성공 이벤트

            // 타깃(적 또는 앵커)을 향해 캐스트
            yield return tgt.Cast(transform, cam, order.Param, t);

            // [RULE: AnchorLifecycle] 캐스트 종료 후 Runner가 앵커 정리
            if (createdAnchor) TargetAnchorPool.Release(t);
        }
        else
        {
            // 논타깃 메커닉
            yield return order.Mech.Cast(transform, cam, order.Param);
        }

        if (order.Param is IHasCooldown h) cd = Mathf.Max(cd, h.Cooldown);

        // [RULE: Events] 종료 이벤트는 Runner 단일 경로에서 발행
        Publish(new CastEnded(meta, skillRef, transform, false));

        busy = false;
    }

    // === 훅 엔드포인트(메커닉에서 콜백) ===
    public void NotifyHookOnHit(Transform target, Vector2 point) => BroadcastHook(AbilityHook.OnHit, target);
    public void NotifyHookOnExpire(Vector2 point) => BroadcastHook(AbilityHook.OnExpire, null);
    public void NotifyHookOnExpire() => BroadcastHook(AbilityHook.OnExpire, null);

    void BroadcastHook(AbilityHook hook, Transform prevTarget)
    {
        // [RULE: FollowUpProvider] FollowUp은 Param에서 선언적으로 가져와 동일 경로로 Schedule
        if (param is IFollowUpProvider p)
            foreach (var (order, delay, respect) in p.BuildFollowUps(hook, prevTarget))
                Schedule(order, delay, respect);
    }

    // 바인딩
    public void Init(ISkillMechanic m, ISkillParam p) { mech = m; param = p; cam = Camera.main; }
}