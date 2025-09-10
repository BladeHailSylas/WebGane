using UnityEngine;
using UnityEngine.InputSystem;

public sealed class InputBinder : MonoBehaviour
{
    private InputSystem_Actions controls;

    [SerializeField] private PlayerActController playerControl;           // �̵�
    [SerializeField] private PlayerAttackController attackController;     // ����/��ų

    void Awake()
    {
        controls = new InputSystem_Actions();
    }

    void OnEnable()
    {
        controls.Player.Enable();

        // �̵� �� PlayerController
        controls.Player.Move.performed += ctx => playerControl.MakeMove(ctx.ReadValue<Vector2>());
        controls.Player.Move.canceled += _ => playerControl.MakeMove(Vector2.zero);

        // ����/��ų �� AttackController
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
        // (����) �̺�Ʈ �ڵ鷯 �������� �Ĳ��� �ϸ� ��Ȱ��ȭ �� �ߺ� ����
        //controls.Player.Move.performed -= ctx => playerController.MakeMove(ctx.ReadValue<Vector2>()); // ���� ���� �ø� ����
        // ���������� ���� ��� �޼��� ĳ�ø� ������ (�Ʒ� ����)
    }
}
/* Todo
 ����� ICommand�� ǥ��ȭ�ϰ� CommandBus.Flush������ �����Ѵ�

 PlayerActs�� IMovable/IJumpable/IVulnerable/... �������θ� �����Ѵ�

 ���� ���� ��Ģ�� IStateRule�� �߾����� �����Ѵ�

 SO�� ������, ���� ScriptableObject �������� �и��Ѵ�

 PlayerFactory�� ���ؽ�Ʈ ����(ĳ���� ��ü�� SO ��ü��)

 �Է��� Ŀ�ǵ� ��ȯ�� �ϰ� ������ ����/���ؽ�Ʈ�� �帣�� �Ѵ�

 ������ ������ MonoBehaviour ������ ���� �׽�Ʈ �ۼ�

 Update ���� ����ȭ(ITick) + GC.Alloc(������) �����

 �̺�Ʈ ����/������ �޼��� �׷�/ĳ�ø� ���

 README�� �����/����/������ �帮�� ���衱�� 1�������� ����
*/