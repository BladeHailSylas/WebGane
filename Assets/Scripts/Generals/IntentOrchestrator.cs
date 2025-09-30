using SkillInterfaces;
using UnityEngine;

/// <summary>
/// IntentOrchestrator는 메커니즘 실행 중 발생하는 의도(Intent)를 중앙에서 중재합니다.
/// - 현재는 FollowUp 스케줄링과 훅(AbilityHook) 브로드캐스트를 SkillRunner에게 위임합니다.
/// - 차후 VFX/사운드/카메라 셰이크 등 부가 연출을 연결하려면 이 클래스를 확장하십시오.
/// </summary>
public sealed class IntentOrchestrator : MonoBehaviour
{
    [SerializeField, Tooltip("동일 게임오브젝트 혹은 부모에서 탐색되는 SkillRunner 참조")]
    SkillRunner runner;

    SkillRunner Runner
    {
        get
        {
            if (!runner)
                runner = GetComponent<SkillRunner>() ?? GetComponentInParent<SkillRunner>();
            return runner;
        }
    }

    /// <summary>
    /// AbilityHook을 SkillRunner에게 위임하여 FollowUp을 처리합니다.
    /// worldPoint는 선택 사항이며 추후 VFX/카메라 연동 시 활용 가능합니다.
    /// </summary>
    public void EmitHook(AbilityHook hook, ISkillParam sourceParam, Transform prevTarget, Vector2? worldPoint, string debugTag)
    {
        if (sourceParam == null) return;
        Runner?.EmitHook(hook, prevTarget, sourceParam, worldPoint, debugTag ?? name);
    }

    /// <summary>
    /// 메커니즘이 명시적으로 FollowUp 주문을 예약하고자 할 때 사용합니다.
    /// reason은 디버깅 로그를 위한 Hook 분류 값입니다.
    /// </summary>
    public void ScheduleFollowUp(CastOrder order, float delay, bool respectBusyCooldown, AbilityHook reason, string debugTag)
    {
        Runner?.ScheduleFollowUp(order, delay, respectBusyCooldown, reason, debugTag ?? name);
    }

    /**
     * TODO: Intent 단계에서 별도의 버프/디버프, 상태이상, VFX, SFX 등을 일괄 처리하려면
     *       ExecuteEffect, PlayVfx, PlaySfx 등의 API를 여기에 추가하십시오.
     */
}
