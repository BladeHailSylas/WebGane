using SOInterfaces;
using ActInterfaces;
//using UnityEditor.ShaderGraph.Configuration;
using UnityEngine;

public class SkillRunner : MonoBehaviour, ISkillRunner
{
    ISkillMechanic mech;
    ISkillParam param;
    Camera cam;
    bool busy; float cd;

    public bool IsBusy => busy;
    public bool IsOnCooldown => cd > 0f;

    public void Init(ISkillMechanic mechanic, ISkillParam param)
    { mech = mechanic; this.param = param; cam = Camera.main; }

    public void TryCast()
    {
        if (busy || cd > 0f || mech == null || param == null) return;
        StartCoroutine(CoCast());
    }

    System.Collections.IEnumerator CoCast()
    {
        busy = true;

        // �� Ÿ�����̸� Ŀ�� Ÿ���� ���� ����
        if (mech is ITargetedMechanic tgtMech)
        {
            var provider = GetComponent<ITargetable>() ?? GetComponentInChildren<ITargetable>();
            if (provider == null || !provider.TryGetTarget(out Transform target) || target == null)
            {
                busy = false; yield break; // Ÿ�� ������ ���� ���(��å�� ���� ���� ���� ��)
            }
            yield return tgtMech.Cast(transform, cam, param, target);
        }
        else
        {
            // �� �Ϲ� ��ų�� ���� �ڷ�ƾ ���
            yield return mech.Cast(transform, cam, param);
        }

        if (param is IHasCooldown hasCd) cd = hasCd.Cooldown;
        busy = false;
    }

    void Update() { if (cd > 0f) cd -= Time.deltaTime; }
}