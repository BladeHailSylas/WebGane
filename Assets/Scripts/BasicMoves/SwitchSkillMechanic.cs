// Assets/Scripts/Mechanics/SwitchControllerMechanic.cs
using System.Collections;
using UnityEngine;
using SOInterfaces;
using ActInterfaces;             // ITargetable

[CreateAssetMenu(menuName = "Mechanics/Switch Controller")]
public class SwitchSkillMechanic : SkillMechanicBase<SwitchControllerParams>
{
    [Header("순서대로 교대 실행할 기술들")]
    public MechanicRef[] steps;

    public override IEnumerator Cast(Transform owner, Camera cam, SwitchControllerParams p)
    {
        if (steps == null || steps.Length == 0) yield break;

        // 1) 커서 가져오기/증가
        var state = owner.GetComponent<SwitchCursorState>();
        if (!state) state = owner.gameObject.AddComponent<SwitchCursorState>();

        int idx = state.GetAndAdvance(steps.Length, p.startIndex, p.advanceOnCast);
        var step = steps[idx];

        if (!step.TryGet(out var next)) yield break;

        // 2) 내부 쿨다운을 컨트롤러 파라미터에 미리 반영 (Runner가 종료 후 읽음)
        if (step.param is IHasCooldown h) p.runtimeCooldown = h.Cooldown;
        else p.runtimeCooldown = 0f;

        // 3) 타깃형/비타깃형 분기 처리(컨트롤러 내부에서 타깃 확보)
        if (next is ITargetedMechanic tnext)
        {
            Transform target = null;
            var provider = owner.GetComponent<ITargetable>() ?? owner.GetComponentInChildren<ITargetable>();
            provider?.TryGetTarget(out target);

            if (target == null) yield break; // 타깃 없으면 취소(기획에 따라 우회 가능)
            yield return tnext.Cast(owner, cam, step.param, target);
        }
        else
        {
            yield return next.Cast(owner, cam, step.param);
        }

        // (선택) 맞아야 다음으로 넘기고 싶으면 advanceOnCast=false로 두고
        //         각 내부 스킬의 OnHit에서 state.AdvanceExplicit()를 호출하도록 1줄만 추가하세요.
    }
}

// 커서 상태만 관리하는 초소형 컴포넌트
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

    // OnHit에서 수동 증가가 필요할 때 호출
    public void AdvanceExplicit(int count) => _idx = (_idx + 1 + count) % count;
}
