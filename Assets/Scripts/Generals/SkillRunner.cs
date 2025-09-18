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

    // ������ �⺻ ��ų ���� (�Է¿��� ȣ��)
    public void TryCast()
    {
        if (busy || cd > 0f || mech == null || param == null) return;

        // (A) ����ġ ��å�� ������ ���� �ֹ��� ���� �õ� �� �����ϸ� �װ� ����
        if (param is ISwitchPolicy sp && sp.TrySelect(transform, cam, out var switched))
        {
            Schedule(switched, 0f, respectBusyCooldown: true);
            return;
        }

        // (B) ���ó�� ������ ��Ŀ���� ����
        Schedule(new CastOrder(mech, param), 0f, true);
    }

    // === ǥ�� ������ API (FollowUp/�Ϲ�/����ġ ��� ���� ���) ===
    public void Schedule(CastOrder order, float delay, bool respectBusyCooldown)
    {
        StartCoroutine(CoSchedule(order, delay, respectBusyCooldown));
    }

    IEnumerator CoSchedule(CastOrder order, float delay, bool respect)
    {
        // [RULE: FollowUpWait] �ٻ�/��ٿ��̸� ������� ���� ����� �� ���࡯ (�޺� �ϰ�)
        while (respect && (busy || cd > 0f)) yield return null;

        if (delay > 0f) yield return new WaitForSeconds(delay);
        yield return CoCast(order);
    }

    IEnumerator CoCast(CastOrder order)
    {
        busy = true;

        // [RULE: Events] ���� �̺�Ʈ�� Runner ���� ��ο��� ����
        var meta = Create(transform, channel: "combat");
        var skillRef = new SkillRef(order.Mech as Object);
        Transform target = order.TargetOverride;
        Publish(new CastStarted(meta, skillRef, order.Param, transform, target));

        if (order.Mech is ITargetedMechanic tgt)
        {
            bool createdAnchor = false;   // [RULE: AnchorLifecycle] Runner�� ���� ��Ŀ�� Runner�� å������ ����
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
                            // [RULE: TargetedSelect] �ִ� ��Ÿ� ���� ���� ������� ����(���н� �ߴ�)
                            needEnemy = true;
                            break;

                        case TargetMode.TowardsCursor:
                            // [RULE: NonTargetedCursor] Ŀ�� ���� ��Ŀ ����
                            needEnemy = false;
                            var cursor = CursorWorld2D(cam, transform, depthFallback: 10f);
                            desired = transform.position
                                    + (cursor - transform.position).normalized * Mathf.Max(0f, td.FallbackRange);
                            break;

                        case TargetMode.TowardsMovement:
                            // [RULE: NonTargetedMoveDir] �ֱ� �̵� �������� ��Ŀ ����
                            needEnemy = false;
                            var mv = GetMoveDirOrFacing(transform);
                            desired = transform.position + (Vector3)(mv * Mathf.Max(0f, td.FallbackRange));
                            break;

                        case TargetMode.TowardsOffset:
                            // [RULE: NonTargetedOffset] ���� ���������� ��Ŀ ����
                            needEnemy = false;
                            desired = transform.TransformPoint((Vector3)td.LocalOffset);
                            break;

                        /*case TargetMode.FixedForward:
                            // [RULE: NonTargetedFixedForward] ���� ���� �Ÿ� ��Ŀ ����
                            needEnemy = false;
                            desired = transform.position + transform.right * Mathf.Max(0f, td.FallbackRange);
                            break;*/
                    }
                }

                if (needEnemy)
                {
                    // [RULE: TargetedSelect] Ÿ�� ȹ�� ���� �� ��� ���� + �̺�Ʈ
                    var provider = GetComponent<ITargetable>() ?? GetComponentInChildren<ITargetable>();
                    if (provider == null || !provider.TryGetTarget(out t) || t == null)
                    {
                        Publish(new TargetNotFound(meta, skillRef, transform)); // ����: ���̷ε尡 �ִٸ�
                        Publish(new CastEnded(meta, skillRef, transform, interrupted: true));
                        busy = false; yield break;
                    }
                }
                else
                {
                    // [RULE: AnchorClamp] �� �ձ��� ���� + �ּҰŸ� ����
                    var clamped = ResolveReachablePoint2D(transform.position, desired, walls, rad, skin);
                    var v = (clamped - transform.position);
                    if (v.sqrMagnitude < 0.0001f)
                        clamped = transform.position + transform.right * Mathf.Max(0.5f, rad + skin);

                    // [RULE: AnchorCreate] ��Ŀ ������ Runner å��
                    t = TargetAnchorPool.Acquire(clamped);
                    createdAnchor = true;
                }
            }

            Publish(new TargetAcquired(meta, skillRef, transform, t)); // ����: Ÿ�� ���� �̺�Ʈ

            // Ÿ��(�� �Ǵ� ��Ŀ)�� ���� ĳ��Ʈ
            yield return tgt.Cast(transform, cam, order.Param, t);

            // [RULE: AnchorLifecycle] ĳ��Ʈ ���� �� Runner�� ��Ŀ ����
            if (createdAnchor) TargetAnchorPool.Release(t);
        }
        else
        {
            // ��Ÿ�� ��Ŀ��
            yield return order.Mech.Cast(transform, cam, order.Param);
        }

        if (order.Param is IHasCooldown h) cd = Mathf.Max(cd, h.Cooldown);

        // [RULE: Events] ���� �̺�Ʈ�� Runner ���� ��ο��� ����
        Publish(new CastEnded(meta, skillRef, transform, false));

        busy = false;
    }

    // === �� ��������Ʈ(��Ŀ�п��� �ݹ�) ===
    public void NotifyHookOnHit(Transform target, Vector2 point) => BroadcastHook(AbilityHook.OnHit, target);
    public void NotifyHookOnExpire(Vector2 point) => BroadcastHook(AbilityHook.OnExpire, null);
    public void NotifyHookOnExpire() => BroadcastHook(AbilityHook.OnExpire, null);

    void BroadcastHook(AbilityHook hook, Transform prevTarget)
    {
        // [RULE: FollowUpProvider] FollowUp�� Param���� ���������� ������ ���� ��η� Schedule
        if (param is IFollowUpProvider p)
            foreach (var (order, delay, respect) in p.BuildFollowUps(hook, prevTarget))
                Schedule(order, delay, respect);
    }

    // ���ε�
    public void Init(ISkillMechanic m, ISkillParam p) { mech = m; param = p; cam = Camera.main; }
}