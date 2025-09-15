// SkillRunner.cs (�߰�/����)
using SOInterfaces;
using ActInterfaces;
using UnityEngine;
using System.Collections;

// �� CharacterSpec.FollowUpBinding ����� ���� using �߰�
using static CharacterSpec;

public class SkillRunner : MonoBehaviour, ISkillRunner
{
    ISkillMechanic mech;
    ISkillParam param;
    Camera cam;
    bool busy; float cd;

    public bool IsBusy => busy;
    public bool IsOnCooldown => cd > 0f;

    // �� ĳ���ͺ� �޺� ���ε� ����
    FollowUpBinding _onCastStart, _onHit, _onExpire;

    // ���� Init Ȯ��: �ź� follow-up�� �Բ� ����
    public void Init(ISkillMechanic mechanic, ISkillParam param,
                    FollowUpBinding onCastStart = default,
                    FollowUpBinding onHit = default,
                    FollowUpBinding onExpire = default)
    {
        mech = mechanic; this.param = param; cam = Camera.main;
        _onCastStart = onCastStart; _onHit = onHit; _onExpire = onExpire;
    }

    public void TryCast()
    {
        if (busy || cd > 0f || mech == null || param == null) return;
        StartCoroutine(CoCast());
    }

    System.Collections.IEnumerator CoCast()
    {
        busy = true;

        // �� ���� ���� �� follow-up
        TryFollowUp(_onCastStart, null, default);

        // ���� ���� ���� (Ÿ����/�Ϲ� �б�)
        if (mech is ITargetedMechanic tgtMech)
        {
            var provider = GetComponent<ITargetable>() ?? GetComponentInChildren<ITargetable>();
            if (provider == null || !provider.TryGetTarget(out Transform target) || target == null)
            { busy = false; yield break; }
            yield return tgtMech.Cast(transform, cam, param, target);
        }
        else
        {
            yield return mech.Cast(transform, cam, param);
        }

        if (param is IHasCooldown hasCd) cd = hasCd.Cooldown;
        busy = false;
    }

    void Update() { if (cd > 0f) cd -= Time.deltaTime; }

    // �� ��Ŀ�п��� �� ���� �� ȣ��
    public void NotifyHookOnHit(Transform target, Vector2 point) => TryFollowUp(_onHit, target, point);
    public void NotifyHookOnExpire(Vector2 point) => TryFollowUp(_onExpire, null, point);

    // �� follow-up ���� ���� ó��
    void TryFollowUp(FollowUpBinding fu, Transform tgt, Vector2 hitPoint)
    {
        if (fu.mechanic is not ISkillMechanic) return;
        if (!fu.IsValid(out ISkillMechanic next)) return;

        StartCoroutine(Co());
        IEnumerator Co()
        {
            if (fu.respectBusyCooldown && (busy || cd > 0f)) yield break;
            if (fu.delay > 0f) yield return new WaitForSeconds(fu.delay);

            if (next is ITargetedMechanic tnext)
            {
                // Ÿ���� follow-up: ���� Ÿ�� ���� �Ǵ� ���� ����
                Transform pass = fu.passSameTarget ? tgt : null;
                if (pass == null)
                {
                    var provider = GetComponent<ITargetable>() ?? GetComponentInChildren<ITargetable>();
                    if (provider == null || !provider.TryGetTarget(out pass) || pass == null) yield break;
                }
                yield return tnext.Cast(transform, cam, fu.param, pass);
            }
            else
            {
                // �Ϲ��� follow-up
                yield return next.Cast(transform, cam, fu.param);
            }

            // follow-up ��Ÿ�� �ݿ�(�ִٸ�)
            if (fu.param is IHasCooldown h) cd = Mathf.Max(cd, h.Cooldown);
        }
    }
}
