using UnityEngine;
using UnityEngine.InputSystem;

public sealed class InputBinder : MonoBehaviour
{
    private InputSystem_Actions controls;

    [SerializeField] private PlayerController playerController;           // �̵�
    [SerializeField] private PlayerAttackController attackController;     // ����/��ų

    void Awake()
    {
        controls = new InputSystem_Actions();
    }

    void OnEnable()
    {
        controls.Player.Enable();

        // �̵� �� PlayerController
        controls.Player.Move.performed += ctx => playerController.MakeMove(ctx.ReadValue<Vector2>());
        controls.Player.Move.canceled += _ => playerController.MakeMove(Vector2.zero);

        // ����/��ų �� AttackController
        controls.Player.Attack.performed += _ => attackController.OnAttack(_);
        controls.Player.Skill1.performed += _ => attackController.OnSkill1(_);
        controls.Player.Skill2.performed += _ => attackController.OnSkill2(_);
        //controls.Player.Skill3.performed += _ => attackController.OnSkill3(_);
        //controls.Player.Ultimate.performed += _ => attackController.Ultimate(_);
        //controls.Player.GeneralSkill.performed += _ => attackController.GeneralSkill(_);
    }

    void OnDisable()
    {
        controls.Player.Disable();
        // (����) �̺�Ʈ �ڵ鷯 �������� �Ĳ��� �ϸ� ��Ȱ��ȭ �� �ߺ� ����
        controls.Player.Move.performed -= ctx => playerController.MakeMove(ctx.ReadValue<Vector2>()); // ���� ���� �ø� ����
        // ���������� ���� ��� �޼��� ĳ�ø� ������ (�Ʒ� ����)
    }
}