// SkillRunner.cs (REFACTOR)
using ActInterfaces;
using SOInterfaces;
using System.Collections;
using UnityEditor.UIElements;
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
        if (respect)
        {
            while (busy || cd > 0f)
            {
                yield return null;
            }
            if (delay > 0f) yield return new WaitForSeconds(delay);
            yield return CoCast(order);
        }
    }

    IEnumerator CoCast(CastOrder order)
    {
        busy = true;
        BroadcastHook(AbilityHook.OnCastStart, null);
        var meta = Create(transform, channel: "combat"); // 공통 메타
        var skillRef = new SkillRef(order.Mech as Object); // SO 참조 기반
        Transform target = order.TargetOverride;
        Publish(new CastStarted(meta, skillRef, order.Param, transform, target));
        // 타깃 분기는 Runner만 담당 (혼재 OK)
        if (order.Mech is ITargetedMechanic tgt)
        {
            /*
            //타깃 획득 로직
            var provider = GetComponent<ITargetable>() ?? GetComponentInChildren<ITargetable>();
                    if (provider == null || !provider.TryGetTarget(out t) || t == null)
                    {
                        Publish(new CastEnded(meta, skillRef, transform, interrupted: true));
                        busy = false; yield break;
                    }
                    // 실패 시: 이벤트 TargetNotFound + 종료 (현 시스템과 일관) :contentReference[oaicite:3]{index=3}
                    // (비타깃인데 적이 가까이 있어도 '무시'해야 하므로 needEnemy=false 로 분기됩니다)
             */
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
                            needEnemy = true;
                            break;

                        case TargetMode.TowardsCursor:
                            needEnemy = false;
                            var cursor = CursorWorld2D(cam, transform, depthFallback: 10f);
                            desired = transform.position + (cursor - transform.position).normalized * Mathf.Max(0f, td.FallbackRange);
                            break;

                        case TargetMode.TowardsMovement:
                            needEnemy = false;
                            var mv = GetMoveDirOrFacing(transform); // 아래 헬퍼 참고
                            desired = transform.position + (Vector3)(mv * Mathf.Max(0f, td.FallbackRange));
                            break;

                        case TargetMode.TowardsOffset:
                            needEnemy = false;
                            desired = transform.TransformPoint((Vector3)td.LocalOffset);
                            break;
                    }
                }

                if (needEnemy)
                {
                    //타깃 획득 로직
                    var provider = GetComponent<ITargetable>() ?? GetComponentInChildren<ITargetable>();
                    if (provider == null || !provider.TryGetTarget(out t) || t == null)
                    {
                        Publish(new CastEnded(meta, skillRef, transform, interrupted: true));
                        busy = false; yield break;
                    }
                    // 실패 시: 이벤트 TargetNotFound + 종료 (현 시스템과 일관) :contentReference[oaicite:3]{index=3}
                    // (비타깃인데 적이 가까이 있어도 '무시'해야 하므로 needEnemy=false 로 분기됩니다)
                }
                else
                {
                    // 1) 벽 앞까지 보정
                    var clamped = ResolveReachablePoint2D(transform.position, desired, walls, rad, skin);

                    // 2) 너무 가까운 점 방지(방향 0되는 것 방지)
                    var v = (clamped - transform.position);
                    if (v.sqrMagnitude < 0.0001f)
                        clamped = transform.position + transform.right * Mathf.Max(0.5f, rad + skin);

                    // 3) 앵커 생성
                    t = TargetAnchorPool.Acquire(clamped);
                    // 캐스트가 끝나면 Release/파괴는 메커닉이 아니라 Runner가 책임지는 편이 안정적
                    // (캐스트 종료 시점에서 Release 해 주세요)
                }
            }
            Publish(new TargetAcquired(meta, skillRef, transform, t));
            // 타깃(적 또는 앵커)을 향해 캐스트
            yield return tgt.Cast(transform, cam, order.Param, t);
        }
        else
        {
            yield return order.Mech.Cast(transform, cam, order.Param);
        }

        if (order.Param is IHasCooldown h) cd = Mathf.Max(cd, h.Cooldown);
        Publish(new CastEnded(meta, skillRef, transform, false));
        // 시전 완료 후 Hook 공급자들의 FollowUp을 같은 경로로 스케줄해도 됨(원한다면 OnAfterCast 훅 추가)
        busy = false;
    }

    // === 훅 엔드포인트(메커닉에서 콜백) ===
    public void NotifyHookOnHit(Transform target, Vector2 point) => BroadcastHook(AbilityHook.OnHit, target);
    public void NotifyHookOnExpire(Vector2 point) => BroadcastHook(AbilityHook.OnExpire, null);

    void BroadcastHook(AbilityHook hook, Transform prevTarget)
    {
        // Param이 FollowUp 제공 시 → 주문서 수집 → Schedule
        if (param is IFollowUpProvider p)
            foreach (var (order, delay, respect) in p.BuildFollowUps(hook, prevTarget))
                Schedule(order, delay, respect);
    }

    // 바인딩
    public void Init(ISkillMechanic m, ISkillParam p) { mech = m; param = p; cam = Camera.main; }
}
