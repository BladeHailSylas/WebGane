using System;
using UnityEngine;
using ActInterfaces;

[Serializable]
public struct CollisionPolicy
{
    public LayerMask wallsMask;      // 항상 차단
    public LayerMask enemyMask;      // 적 레이어
    public bool enemyAsBlocker;      // true: 적을 '벽처럼' 차단, false: 적은 차단 대상 아님
    public float radius;             // 본체 반경
    public float skin;               // 면 재포착 방지 여유
}

public struct MoveResult
{
    public Vector2 actualDelta;      // 실제 이동량(충돌로 줄어들 수 있음)
    public bool hitWall;
    public bool hitEnemy;
    public Transform hitTransform;
    public Vector2 hitNormal;
}

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
public class KinematicMotor2D : MonoBehaviour, ISweepable
{
    [Header("Defaults")]
    public CollisionPolicy defaultPolicy = new()
    {
        wallsMask = 0,
        enemyMask = 0,
        enemyAsBlocker = true,
        radius = 0.5f,
        skin = 0.05f
    };

    Rigidbody2D rb;
    CollisionPolicy current;
    public Vector2 LastMoveVector { get; private set; } = Vector2.zero;
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        // [RULE: Kinematic] 항상 Kinematic을 전제로 운용
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        current = defaultPolicy;
    }

    // [RULE: PolicyScope] 일시 정책 오버라이드 스코프
    public IDisposable With(in CollisionPolicy overridePolicy)
    {
        var prev = current;
        current = overridePolicy;
        return new Scope(() => current = prev);
    }
    sealed class Scope : IDisposable { readonly Action onDispose; public Scope(Action a) { onDispose = a; } public void Dispose() { onDispose?.Invoke(); } }

    // [RULE: Depenetrate] 시작 겹침 탈출(의도 방향으로 radius+skin만큼)
    public void BeginFrameDepenetrate(Vector2 preferredDir)
    {
        // 벽 겹침만 대상(적 겹침까지 밀어낼지 여부는 정책에 따라 선택 가능)
        if (Physics2D.OverlapCircle(transform.position, current.radius, current.wallsMask))
        {
            var dir = preferredDir.sqrMagnitude > 0.0001f ? preferredDir.normalized : (Vector2)transform.right;
            MoveDiscrete(dir * (current.radius + Mathf.Max(0.01f, current.skin)));
        }
    }

    // [RULE: SweepClamp] 충돌-안전 이동(단일 관문)
    public MoveResult SweepMove(Vector2 desiredDelta)
    {
        var result = new MoveResult { actualDelta = Vector2.zero };
        if (desiredDelta.sqrMagnitude <= 0f) return result;

        Vector2 origin = transform.position;
        Vector2 dir = desiredDelta.normalized;
        float dist = desiredDelta.magnitude;

        // 1) 벽 우선 캐스트
        var wallHit = Physics2D.CircleCast(origin, current.radius, dir, dist, current.wallsMask);
        if (wallHit.collider)
        {
            float toHit = wallHit.distance;
            if (toHit > 0f) MoveDiscrete(dir * toHit);
            // [RULE: SKIN] 면 재포착 방지용 소량 전진(선택)
            // MoveDiscrete(dir * Mathf.Min(current.skin, 0.01f));
            result.actualDelta = (Vector2)transform.position - origin;
            result.hitWall = true;
            result.hitTransform = wallHit.transform;
            result.hitNormal = wallHit.normal;
            return result;
        }

        // 2) 적 차단 정책
        if (current.enemyAsBlocker && current.enemyMask.value != 0)
        {
            var enemyHit = Physics2D.CircleCast(origin, current.radius, dir, dist, current.enemyMask);
            if (enemyHit.collider)
            {
                float toHit = enemyHit.distance;
                if (toHit > 0f) MoveDiscrete(dir * toHit);
                result.actualDelta = (Vector2)transform.position - origin;
                result.hitEnemy = true;
                result.hitTransform = enemyHit.transform;
                result.hitNormal = enemyHit.normal;
                return result;
            }
        }

        // 3) 충돌 없음 → 전부 이동
        MoveDiscrete(desiredDelta);
        result.actualDelta = desiredDelta;
        LastMoveVector = result.actualDelta;
        return result;
    }

    // 내부 이동 구현: Rigidbody2D.Kinematic 기반 MovePosition
    void MoveDiscrete(Vector2 delta)
    {
        if (delta.sqrMagnitude <= 0f) return;
        if (rb) rb.MovePosition((Vector2)transform.position + delta);
        else transform.position += (Vector3)delta;
    }

    // 현재 정책을 참조할 수 있도록 Getter 제공(선택)
    public CollisionPolicy CurrentPolicy => current;
}
