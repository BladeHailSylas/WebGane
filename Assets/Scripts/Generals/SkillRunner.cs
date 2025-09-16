// SkillRunner.cs (REFACTOR)
using ActInterfaces;
using SOInterfaces;
using System.Collections;
using UnityEngine;
using static EventBus;
using static GameEventMetaFactory;

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
                var provider = GetComponent<ITargetable>() ?? GetComponentInChildren<ITargetable>();
                if (provider == null || !provider.TryGetTarget(out t) || t == null) 
                {
                    Publish(new CastEnded(meta, skillRef, transform, interrupted: true));
                    busy = false; yield break; 
                }
            }
            yield return tgt.Cast(transform, cam, order.Param, t);
        }
        else
        {
            yield return order.Mech.Cast(transform, cam, order.Param);
        }

        if (order.Param is IHasCooldown h) cd = Mathf.Max(cd, h.Cooldown);

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
