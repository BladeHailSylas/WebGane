// Assets/Scripts/Mechanics/SwitchControllerMechanic.cs
using System.Collections;
using UnityEngine;
using SOInterfaces;
using ActInterfaces;             // ITargetable

[CreateAssetMenu(menuName = "Mechanics/Switch Controller")]
public class SwitchSkillMechanic : SkillMechanicBase<SwitchControllerParams>
{
    [Header("������� ���� ������ �����")]
    public MechanicRef[] steps;

    public override IEnumerator Cast(Transform owner, Camera cam, SwitchControllerParams p)
    {
        if (steps == null || steps.Length == 0) yield break;

        // 1) Ŀ�� ��������/����
        var state = owner.GetComponent<SwitchCursorState>();
        if (!state) state = owner.gameObject.AddComponent<SwitchCursorState>();

        int idx = state.GetAndAdvance(steps.Length, p.startIndex, p.advanceOnCast);
        var step = steps[idx];

        if (!step.TryGet(out var next)) yield break;

        // 2) ���� ��ٿ��� ��Ʈ�ѷ� �Ķ���Ϳ� �̸� �ݿ� (Runner�� ���� �� ����)
        if (step.param is IHasCooldown h) p.runtimeCooldown = h.Cooldown;
        else p.runtimeCooldown = 0f;

        // 3) Ÿ����/��Ÿ���� �б� ó��(��Ʈ�ѷ� ���ο��� Ÿ�� Ȯ��)
        if (next is ITargetedMechanic tnext)
        {
            Transform target = null;
            var provider = owner.GetComponent<ITargetable>() ?? owner.GetComponentInChildren<ITargetable>();
            provider?.TryGetTarget(out target);

            if (target == null) yield break; // Ÿ�� ������ ���(��ȹ�� ���� ��ȸ ����)
            yield return tnext.Cast(owner, cam, step.param, target);
        }
        else
        {
            yield return next.Cast(owner, cam, step.param);
        }

        // (����) �¾ƾ� �������� �ѱ�� ������ advanceOnCast=false�� �ΰ�
        //         �� ���� ��ų�� OnHit���� state.AdvanceExplicit()�� ȣ���ϵ��� 1�ٸ� �߰��ϼ���.
    }
}

// Ŀ�� ���¸� �����ϴ� �ʼ��� ������Ʈ
public class SwitchCursorState : MonoBehaviour
{
    int _idx = -1;

    public int GetAndAdvance(int count, int startIndex, bool advanceNow)
    {
        if (count <= 0) return 0;
        if (_idx < 0) _idx = Mathf.Clamp(startIndex, 0, count - 1);
        int cur = _idx;
        if (advanceNow) _idx = (_idx + 1) % count;
        return cur;
    }

    // OnHit���� ���� ������ �ʿ��� �� ȣ��
    public void AdvanceExplicit(int count) => _idx = (_idx + 1 + count) % count;
}
