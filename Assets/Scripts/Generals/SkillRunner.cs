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
        if (respect && (busy || cd > 0f)) yield break;
        if (delay > 0f) yield return new WaitForSeconds(delay);
        yield return CoCast(order);
    }

    IEnumerator CoCast(CastOrder order)
    {
        busy = true;
        BroadcastHook(AbilityHook.OnCastStart, null);
        var meta = Create(transform, channel: "combat"); // ���� ��Ÿ
        var skillRef = new SkillRef(order.Mech as Object); // SO ���� ���
        Transform target = order.TargetOverride;
        Publish(new CastStarted(meta, skillRef, order.Param, transform, target));
        // Ÿ�� �б�� Runner�� ��� (ȥ�� OK)
        if (order.Mech is ITargetedMechanic tgt)
        {
            Transform t = order.TargetOverride;

            if (t == null)
            {
                bool needEnemy = true;
                Vector3 desired = Vector3.zero;
                LayerMask walls = 0; float rad = 0f; float skin = 0.05f;

                if (order.Param is ITargetingData td)
                {
                    walls = td.WallsMask; rad = td.CollisionRadius; skin = Mathf.Max(0.01f, td.AnchorSkin);

                    switch (td.Mode)
                    {
                        case TargetMode.AcquireEnemy:
                            needEnemy = true;
                            break;

                        case TargetMode.CursorWorld:
                            needEnemy = false;
                            desired = GetCursorWorld2D(cam, transform);
                            break;

                        case TargetMode.FixedForward:
                            needEnemy = false;
                            desired = transform.position + transform.right * Mathf.Max(0f, td.FallbackRange);
                            break;

                        case TargetMode.FixedOffset:
                            needEnemy = false;
                            desired = transform.TransformPoint((Vector3)td.LocalOffset);
                            break;
                    }
                }

                if (needEnemy)
                {
                    var provider = GetComponent<ITargetable>() ?? GetComponentInChildren<ITargetable>();
                    if (provider == null || !provider.TryGetTarget(out t) || t == null)
                    {
                        Publish(new CastEnded(meta, skillRef, transform, interrupted: true));
                        busy = false; yield break;
                    }
                    // ���� ��: �̺�Ʈ TargetNotFound + ���� (�� �ý��۰� �ϰ�) :contentReference[oaicite:3]{index=3}
                    // (��Ÿ���ε� ���� ������ �־ '����'�ؾ� �ϹǷ� needEnemy=false �� �б�˴ϴ�)
                }
                else
                {
                    // ---- ��Ÿ��: �������� �� �ձ��� ���� �� ��Ŀ ���� ----
                    Vector3 clamped = ResolveReachablePoint2D(transform.position, desired, walls, rad, skin);
                    Debug.Log($"anchor has generated at {clamped.x} + {clamped.y}");
                    // �ʹ� ����� ���� ���Ͱ� 0�� �Ǵ� ���� ����
                    Vector2 dir = (Vector2)(clamped - transform.position);
                    if (dir.sqrMagnitude < 0.0001f)
                    {
                        // ���콺�� ���� ���ڸ��� �������� �ּ� �Ÿ���ŭ �о���
                        Vector2 safeDir = (Vector2)transform.right;
                        clamped = transform.position + (Vector3)(safeDir * Mathf.Max(0.5f, rad + skin));
                    }

                    t = TargetAnchorPool.Acquire(clamped);
                }
            }
            Publish(new TargetAcquired(meta, skillRef, transform, t));
            // Ÿ��(�� �Ǵ� ��Ŀ)�� ���� ĳ��Ʈ
            yield return tgt.Cast(transform, cam, order.Param, t);
        }
        else
        {
            yield return order.Mech.Cast(transform, cam, order.Param);
        }

        if (order.Param is IHasCooldown h) cd = Mathf.Max(cd, h.Cooldown);
        Publish(new CastEnded(meta, skillRef, transform, false));
        // ���� �Ϸ� �� Hook �����ڵ��� FollowUp�� ���� ��η� �������ص� ��(���Ѵٸ� OnAfterCast �� �߰�)
        busy = false;
    }

    // === �� ��������Ʈ(��Ŀ�п��� �ݹ�) ===
    public void NotifyHookOnHit(Transform target, Vector2 point) => BroadcastHook(AbilityHook.OnHit, target);
    public void NotifyHookOnExpire(Vector2 point) => BroadcastHook(AbilityHook.OnExpire, null);

    void BroadcastHook(AbilityHook hook, Transform prevTarget)
    {
        // Param�� FollowUp ���� �� �� �ֹ��� ���� �� Schedule
        if (param is IFollowUpProvider p)
            foreach (var (order, delay, respect) in p.BuildFollowUps(hook, prevTarget))
                Schedule(order, delay, respect);
    }

    // ���ε�
    public void Init(ISkillMechanic m, ISkillParam p) { mech = m; param = p; cam = Camera.main; }
}
