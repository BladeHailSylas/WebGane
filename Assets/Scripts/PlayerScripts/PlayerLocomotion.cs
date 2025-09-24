using ActInterfaces;
using UnityEngine;

public class PlayerLocomotion : MonoBehaviour, IMovable, IPullable
{
    [SerializeField] private Rigidbody2D rb;          // Kinematic 본체
    public Vector2 LastMoveDir { get; private set; }
    // Knockback은 Kinematic에서 물리속도가 아닌 "추가 변위"로 처리
    Vector2 _knockbackBudget;                         // 이번 프레임에 소비할 추가 변위(월드 좌표)

    /// <summary>
    /// IMovable: 의도(방향·속도)를 단일 스윕용 "델타"로 환산하여 Motor로 전달합니다.
    /// - 이동 결정은 상위에서(속도/입력), 충돌-절단은 Motor에서.
    /// - 프레임당 단 한 번 호출되도록 컨트롤러(Update)에서만 호출하세요.
    /// </summary>
    public void Move(Vector2 direction, Rigidbody2D rbArg, float speed)
    {
        // 방향 정규화 및 델타 산출
        Vector2 dir = direction.sqrMagnitude > 1e-4f ? direction.normalized : Vector2.zero;
        Vector2 delta = Mathf.Max(0f, speed) * Time.deltaTime * dir;
        // Knockback 추가 변위는 같은 프레임에 소비 후 0으로
        if (_knockbackBudget.sqrMagnitude > 0f)
        {
            delta += _knockbackBudget;
            _knockbackBudget = Vector2.zero;
        }
		// 의도 방향을 선호 방향으로 하여 겹침 청소(모서리 락 방지)  // :contentReference[oaicite:12]{index=12}
		var motor = GetComponentInParent<KinematicMotor2D>();
		if (!motor) return;
		//motor.RemoveComponent();
		motor.Depenetration();
        // 단일 스윕 이동(충돌로 절단/슬라이드는 Motor 정책에 따름)      // :contentReference[oaicite:13]{index=13}
        var res = motor.SweepMove(delta);
		motor.Depenetration();
        // 마지막 실제 이동 벡터 기록(원한다면 실제 속도 등 2차 파생 가능)
        LastMoveDir = direction;//motor.LastMoveVector;
    }

    /// <summary>
    /// IPullable: Kinematic에서는 velocity 변경이 무의미하므로,
    /// "즉시 한 번 밀리는 추가 변위" 예산으로 전환해 다음 Move에서 소비합니다.
    /// force 단위는 '거리'로 간주(필요 시 감쇠/시간기반으로 확장 가능).
    /// </summary>
    public void ApplyKnockback(Vector2 direction, float force)
    {
        Vector2 dir = direction.sqrMagnitude > 1e-4f ? direction.normalized : Vector2.zero;
        _knockbackBudget += dir * Mathf.Max(0f, force);
    }

    // (참고) 기존 Jump/Coroutine은 그대로 두되, 실제 수직 이동이 필요하면 별도 모터/레이어로 분리 권장 -> Jump를 계속 사용해야 할지 모르겠음
}