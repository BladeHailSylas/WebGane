using ActInterfaces;
using StatsInterfaces;
using System.Collections;
using UnityEngine;

public class PlayerActController : MonoBehaviour, IVulnerable, IPullable
{
    [SerializeField] PlayerStats stats;
    [SerializeField] PlayerLocomotion locomotion;
    [SerializeField] PlayerEffects effects;

    [Header("Wiring")]
    [SerializeField] Rigidbody2D rig;          // Kinematic 본체(모터는 이 트랜스폼을 이동)
    //[SerializeField] SkillRunner skillRunner;  // 스킬 진행 중 기본 이동 억제용 (Dash 등)  // :contentReference[oaicite:6]{index=6}*/

    [Header("Move")]
    [SerializeField] float moveSpeed = 8f;     // TODO: stats로 치환 예정
    Vector2 _moveInput;                        // 입력 버퍼(이벤트 → 프레임 단일 처리)

    void Awake()
    {
        if (!rig) rig = GetComponent<Rigidbody2D>();
    }

    // 입력 이벤트에서 방향만 갱신(즉시 이동 금지)
    public void MakeMove(Vector2 move) => _moveInput = move;

    void Update()
    {
        // 스킬(대시 등) 진행 중엔 기본 이동을 잠시 억제해 프레임 이중 스윕을 차단
        //if (skillRunner && skillRunner.IsBusy) return;             // :contentReference[oaicite:7]{index=7}
        if (!effects || !effects.IsMovable) return;

        // 프레임당 단일 이동 실행: 방향·속도를 델타로 환산해 Locomotion으로 전달
        locomotion.Move(_moveInput, rig, moveSpeed);               // :contentReference[oaicite:8]{index=8}
    }

    // --- IVulnerable ---
    public void TakeDamage(float damage, float apratio, bool isFixed)
        => stats.ReduceStat(ReduceType.Health, damage, apratio, isFixed);

    public void Die() => Destroy(gameObject);

    // --- IPullable ---
    public void ApplyKnockback(Vector2 direction, float force)
    {
        // Kinematic에서는 velocity/Force가 먹지 않으므로 Locomotion 버퍼로 위임
        locomotion.ApplyKnockback(direction, force);               // :contentReference[oaicite:9]{index=9}
    }
}