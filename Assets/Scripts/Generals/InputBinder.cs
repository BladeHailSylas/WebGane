using UnityEngine;
using UnityEngine.InputSystem;

public sealed class InputBinder : MonoBehaviour
{
    private InputSystem_Actions controls;

    [SerializeField] private PlayerActController playerControl;           // 이동
    [SerializeField] private PlayerAttackController attackController;     // 공격/스킬

    void Awake()
    {
        controls = new InputSystem_Actions();
    }

    void OnEnable()
    {
        controls.Player.Enable();

        // 이동 → PlayerController
        controls.Player.Move.performed += ctx => playerControl.MakeMove(ctx.ReadValue<Vector2>());
        controls.Player.Move.canceled += _ => playerControl.MakeMove(Vector2.zero);

        // 공격/스킬 → AttackController
        /*controls.Player.Attack.performed += _ => attackController.OnAttack(_);
        controls.Player.Skill1.performed += _ => attackController.OnSkill1(_);
        controls.Player.Skill2.performed += _ => attackController.OnSkill2(_);
        controls.Player.Ultimate.performed += _ => attackController.Ultimate(_);;*/
    }

    void OnDisable()
    {
        controls.Player.Move.performed -= ctx => playerControl.MakeMove(ctx.ReadValue<Vector2>());
        controls.Player.Move.canceled -= _ => playerControl.MakeMove(Vector2.zero);
        controls.Player.Disable();
        // (선택) 이벤트 핸들러 해제까지 꼼꼼히 하면 재활성화 시 중복 방지
        //controls.Player.Move.performed -= ctx => playerController.MakeMove(ctx.ReadValue<Vector2>()); // 람다 저장 시만 가능
        // 실전에서는 람다 대신 메서드 캐시를 쓰세요 (아래 참고)
    }
}
/* Todo
 명령을 ICommand로 표준화하고 CommandBus.Flush에서만 실행한다

 PlayerActs는 IMovable/IJumpable/IVulnerable/... 구현으로만 노출한다

 상태 경합 규칙은 IStateRule로 중앙집중 관리한다

 SO는 데이터, 룰은 ScriptableObject 전략으로 분리한다

 PlayerFactory로 컨텍스트 조립(캐릭터 교체는 SO 교체로)

 입력은 커맨드 변환만 하고 로직은 버스/컨텍스트로 흐르게 한다

 도메인 로직을 MonoBehaviour 밖으로 빼서 테스트 작성

 Update 루프 단일화(ITick) + GC.Alloc(프레임) 모니터

 이벤트 구독/해제는 메서드 그룹/캐시만 사용

 README에 “명령/상태/데이터 드리븐 설계”를 1페이지로 설명
*/