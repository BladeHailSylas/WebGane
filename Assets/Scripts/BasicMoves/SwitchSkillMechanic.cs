using System.Collections;
using UnityEngine;
using SkillInterfaces;

[CreateAssetMenu(menuName = "Mechanics/Switch Controller")]
public class SwitchSkillMechanic : SkillMechanicBase<SwitchControllerParams>
{
    // 본문은 아무 것도 하지 않습니다.
    // 실행/선택은 Runner가 Param(ISwitchPolicy)에게 위임하여 수행합니다.
    public override IEnumerator Cast(Transform owner, Camera cam, SwitchControllerParams p)
    {
        yield break;
    }
}