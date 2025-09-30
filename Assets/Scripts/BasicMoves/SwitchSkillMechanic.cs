using System.Collections;
using UnityEngine;
using SkillInterfaces;

[CreateAssetMenu(menuName = "Mechanics/Switch Controller")]
public class SwitchSkillMechanic : SkillMechanismBase<SwitchControllerParams>
{
    protected override IEnumerator Execute(MechanismContext ctx, SwitchControllerParams p)
    {
        var owner = ctx.Owner;
        if (p.TrySelect(owner, ctx.Camera, out var reference, out var order))
        {
            ctx.ScheduleFollowUp(order, reference.delay, reference.respectBusyCooldown, AbilityHook.OnCastEnd, nameof(SwitchSkillMechanic));
        }
        else
        {
            Debug.LogWarning("[SwitchSkillMechanic] 선택 가능한 주문이 없습니다.");
        }

        Vector2? hookPoint = owner ? (Vector2)owner.position : (Vector2?)null;
        ctx.EmitHook(AbilityHook.OnCastEnd, ctx.Target, hookPoint, nameof(SwitchSkillMechanic));
        yield break;
    }
}
