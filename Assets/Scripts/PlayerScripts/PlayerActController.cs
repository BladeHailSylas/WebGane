using ActInterfaces;
using StatsInterfaces;
using UnityEngine;
using Intents;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerActController : IVulnerable, IPullable
{
	PlayerStats stats;
	PlayerLocomotion locomotion;
	PlayerEffects effects;

	[Header("Move")]
	int moveUnits;
	FixedVector2 _moveInput;                        // 입력 버퍼(이벤트 → 프레임 단일 처리)
	private readonly byte _mySId = 1;// = BattleCore.Manager.playerInfo.sid;
	void Awake()
	{
		Debug.Log("Hello again");
		moveUnits = 8000; //Temporarily use
	}
	// 입력 이벤트에서 방향만 갱신(즉시 이동 금지)
	public void MakeMove(FixedVector2 move, byte mySid = 1)
	{
		MakeMove(new NormalMoveData(move), mySid);
	}
	public void MakeMove(IMoveData move, byte mySid = 1)
	{
		mySid = _mySId;
		Debug.Log($"[MakeMove] frame={Time.frameCount} move={move.Type}");
		//_moveInput = move * moveUnits;
	}
	/*
	 private void TickHandler(ushort tick)
	{
		if(tick % 60 == 0) Debug.Log("Ticker");
		// 스킬(대시 등) 진행 중엔 기본 이동을 잠시 억제해 프레임 이중 스윕을 차단
		if (_controls is null) return;
		if (!effects || !effects.IsMovable) return;
		// 프레임당 단일 이동 실행: 방향·속도를 델타로 환산해 Locomotion으로 전달
		locomotion.MoveIntent(_moveInput, _mySId, tick);
	}
	 */
	/*void FixedUpdate()
	{
		// 스킬(대시 등) 진행 중엔 기본 이동을 잠시 억제해 프레임 이중 스윕을 차단
		//if (skillRunner && skillRunner.IsBusy) return;
		if (!effects || !effects.IsMovable) return;

		// 프레임당 단일 이동 실행: 방향·속도를 델타로 환산해 Locomotion으로 전달
		locomotion.MoveIntent(_moveInput, moveSpeed);
	}*/

	// --- IVulnerable ---
	public void TakeDamage(int damage, int apratio, DamageType type)
		=> stats.ReduceStat(ReduceType.Health, damage, apratio, type);
	public void TakeDamage(float damage, float apratio, DamageType type)
	{
		TakeDamage((int)damage, (int)apratio, type);
	}

	public void Die()
	{
		
	}

	// --- IPullable ---
	public void ApplyKnockback(Vector2 direction, float force)
	{
		// Kinematic에서는 velocity/Force가 먹지 않으므로 Locomotion 버퍼로 위임
		//locomotion.ApplyKnockback(direction, force);
	}
}