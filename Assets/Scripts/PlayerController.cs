using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] private PlayerActs acts;
    [SerializeField] private PlayerStats stats;
    [SerializeField] private PlayerAttackController atkctrl;
    private Rigidbody2D rb;

    private Vector2 moveInput;
    private InputSystem_Actions controls;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        controls = new InputSystem_Actions();
    }

    private void OnEnable()
    {
        controls.Player.Enable();
        controls.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        controls.Player.Move.canceled += ctx => moveInput = Vector2.zero;
        // Attack (버튼 눌렀을 때)
        controls.Player.Attack.performed += _ => OnAttack();
        controls.Player.Skill1.performed += _ => OnSkill1();
        controls.Player.Skill2.performed += _ => OnSkill2();
        controls.Player.Skill3.performed += _ => OnSkill3();
        controls.Player.Ultimate.performed += _ => OnUltimate();
        controls.Player.GeneralSkill.performed += _ => OnGeneral();
    }
    private void OnDisable()
    {
        controls.Player.Disable();
    }

    private void OnAttack()
    {
        atkctrl.Attack(stats.AttackDamage);
    }
    private void OnSkill1() => Debug.Log("Skill1 pressed!");
    private void OnSkill2() => Debug.Log("Skill2 pressed!");
    private void OnSkill3() => Debug.Log("Skill3 pressed!");
    private void OnUltimate() => Debug.Log("Ultimate pressed!");
    private void OnGeneral() => Debug.Log("General pressed!");
    /*void Update()
    {
        // Get input
        moveInput.x = Input.GetAxisRaw("Horizontal");
        moveInput.y = Input.GetAxisRaw("Vertical");
    }*/

    void FixedUpdate()
    {
        acts.Move(moveInput, rb);
    }
}