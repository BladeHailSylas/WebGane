using UnityEngine;
using UnityEngine.InputSystem;

public sealed class InputBinder : MonoBehaviour
{
    private InputSystem_Actions controls;

    [SerializeField] private PlayerController playerController;           // 이동
    [SerializeField] private PlayerAttackController attackController;     // 공격/스킬

    void Awake()
    {
        controls = new InputSystem_Actions();
    }

    void OnEnable()
    {
        controls.Player.Enable();

        // 이동 → PlayerController
        controls.Player.Move.performed += ctx => playerController.MakeMove(ctx.ReadValue<Vector2>());
        controls.Player.Move.canceled += _ => playerController.MakeMove(Vector2.zero);

        // 공격/스킬 → AttackController
        /*controls.Player.Attack.performed += _ => attackController.Attack();
        controls.Player.Skill1.performed += _ => attackController.Cast(1);
        controls.Player.Skill2.performed += _ => attackController.Cast(2);
        controls.Player.Skill3.performed += _ => attackController.Cast(3);
        controls.Player.Ultimate.performed += _ => attackController.Cast(99);
        controls.Player.GeneralSkill.performed += _ => attackController.Cast(0);*/
    }

    void OnDisable()
    {
        controls.Player.Disable();
        // (선택) 이벤트 핸들러 해제까지 꼼꼼히 하면 재활성화 시 중복 방지
        controls.Player.Move.performed -= ctx => playerController.MakeMove(ctx.ReadValue<Vector2>()); // 람다 저장 시만 가능
        // 실전에서는 람다 대신 메서드 캐시를 쓰세요 (아래 참고)
    }
}