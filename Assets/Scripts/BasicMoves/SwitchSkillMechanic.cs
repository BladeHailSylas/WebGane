using System.Collections;
using UnityEngine;
using SkillInterfaces;

[CreateAssetMenu(menuName = "Mechanics/Switch Controller")]
public class SwitchSkillMechanic : SkillMechanismBase<SwitchControllerParams>
{
    // ������ �ƹ� �͵� ���� �ʽ��ϴ�.
    // ����/������ Runner�� Param(ISwitchPolicy)���� �����Ͽ� �����մϴ�.
    public override IEnumerator Cast(Transform owner, Camera cam, SwitchControllerParams p)
    {
        yield break;
    }
}