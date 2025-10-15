using ActInterfaces;
using StatsInterfaces;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerActController : MonoBehaviour, IVulnerable, IPullable
{
	[SerializeField] PlayerStats stats;
	[SerializeField] PlayerLocomotion locomotion;
	[SerializeField] PlayerEffects effects;

	[Header("Wiring")]
	[SerializeField] Rigidbody2D rig;          // Kinematic 본체(모터는 이 트랜스폼을 이동)
												//[SerializeField] SkillRunner skillRunner;  // 스킬 진행 중 기본 이동 억제용 (Dash 등)  // :contentReference[oaicite:6]{index=6}*/

	[Header("Move")]
	[SerializeField] int moveSpeed;
	FixedVector2 _moveInput;                        // 입력 버퍼(이벤트 → 프레임 단일 처리)

	private InputSystem_Actions controls;
	void Awake()
	{
		if (!rig) rig = GetComponent<Rigidbody2D>();
		moveSpeed = stats.Speed;
		controls = new InputSystem_Actions();
	}
	void OnEnable()
	{
		controls.Player.Enable();
		controls.Player.Move.performed += ctx => MakeMove(ctx.ReadValue<Vector2>());
		controls.Player.Move.canceled += _ => MakeMove(Vector2.zero);
		BattleCore.Ticker.OnTick += TickHandler;
	}

	void OnDisable()
	{
		controls.Player.Move.performed -= ctx => MakeMove(ctx.ReadValue<Vector2>());
		controls.Player.Move.canceled -= _ => MakeMove(Vector2.zero);
		controls.Player.Disable();
		BattleCore.Ticker.OnTick -= TickHandler;
	}
	public void TickHandler(ushort tick)
	{
		// 스킬(대시 등) 진행 중엔 기본 이동을 잠시 억제해 프레임 이중 스윕을 차단
		if (controls is null) return;             // :contentReference[oaicite:7]{index=7}
		if (!effects || !effects.IsMovable) return;
		// 프레임당 단일 이동 실행: 방향·속도를 델타로 환산해 Locomotion으로 전달
		locomotion.MoveIntent(_moveInput, moveSpeed);               // :contentReference[oaicite:8]{index=8}
	}
	// 입력 이벤트에서 방향만 갱신(즉시 이동 금지)
	public void MakeMove(Vector2 move)
	{
		//Debug.Log($"[MakeMove] frame={Time.frameCount} move={move}");
		_moveInput = new FixedVector2(move);
		//locomotion.Move(move, rig, moveSpeed);
	}
	public void MakeMove(FixedVector2 move)
	{
		_moveInput = move;
	}
	/*void FixedUpdate()
	{
		// 스킬(대시 등) 진행 중엔 기본 이동을 잠시 억제해 프레임 이중 스윕을 차단
		//if (skillRunner && skillRunner.IsBusy) return;             // :contentReference[oaicite:7]{index=7}
		if (!effects || !effects.IsMovable) return;

		// 프레임당 단일 이동 실행: 방향·속도를 델타로 환산해 Locomotion으로 전달
		locomotion.MoveIntent(_moveInput, moveSpeed);               // :contentReference[oaicite:8]{index=8}
	}*/

	// --- IVulnerable ---
	public void TakeDamage(int damage, int apratio, DamageType type)
		=> stats.ReduceStat(ReduceType.Health, damage, apratio, type);
	public void TakeDamage(float damage, float apratio, DamageType type)
	{
		TakeDamage((int)damage, (int)apratio, type);
	}
	public void Die() => Destroy(gameObject);

	// --- IPullable ---
	public void ApplyKnockback(Vector2 direction, float force)
	{
		// Kinematic에서는 velocity/Force가 먹지 않으므로 Locomotion 버퍼로 위임
		//locomotion.ApplyKnockback(direction, force);               // :contentReference[oaicite:9]{index=9}
	}
}