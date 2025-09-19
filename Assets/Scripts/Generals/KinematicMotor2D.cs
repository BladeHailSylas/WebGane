using System;
using UnityEngine;
using ActInterfaces;

[Serializable]
public struct CollisionPolicy
{
    public LayerMask wallsMask;      // 항상 차단
    public LayerMask enemyMask;      // 적 레이어
    public bool enemyAsBlocker;      // true: 적을 '벽처럼' 차단
    public float radius;             // 본체 반경
    public float skin;               // 면 재포착 방지 여유
    public bool allowWallSlide;      // 벽 접촉 시 같은 프레임에 접선 슬라이드 허용
}

public struct MoveResult
{
    public Vector2 actualDelta;
    public bool hitWall, hitEnemy;
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
        enemyAsBlocker = false,   // 일반 이동은 적을 '벽'으로 보지 않음(정지 방지)
        radius = 0.5f,
        skin = 0.05f,
        allowWallSlide = true
    };

    Rigidbody2D rb;
    CollisionPolicy current;
    public Vector2 LastMoveVector { get; private set; } = Vector2.zero;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        current = defaultPolicy;
    }

    public IDisposable With(in CollisionPolicy overridePolicy)
    {
        var prev = current;
        current = overridePolicy;
        return new Scope(() => current = prev);
    }
    sealed class Scope : IDisposable { readonly Action onDispose; public Scope(Action a) { onDispose = a; } public void Dispose() { onDispose?.Invoke(); } }

    /// <summary>
    /// 프레임 시작 겹침 탈출(센서가 제공한 MTV 방향을 사용).
    /// - 임의 +X/의도 방향 '밀기'를 없애, 얇은 벽 반대편으로 빠져나가는 문제를 차단.
    /// - 거리(침투량)는 ColliderDistance 샘플로 추정 후 '겹침량 + skin' 만큼만 이탈.
    /// </summary>
    public void BeginFrameDepenetrate(Vector2 mtvDir)
    {
        if (mtvDir.sqrMagnitude <= 1e-6f) return;

        // 겹침 후보 수집
        var cols = Physics2D.OverlapCircleAll(transform.position, current.radius, current.wallsMask);
        float worstPenetration = 0f; // 음수일수록 깊은 침투(절대값 최대)

        foreach (var c in cols)
        {
            var dist = Physics2D.Distance(c, GetComponent<Collider2D>()); // c(벽) vs 나(본체) 순서 주의
            if (dist.distance < worstPenetration) worstPenetration = dist.distance; // 가장 음수(가장 깊은 침투)
        }

        if (worstPenetration < 0f)
        {
            float moveOut = (-worstPenetration) + Mathf.Max(0.01f, current.skin);
            MoveDiscrete(mtvDir.normalized * moveOut);
        }
    }

    /// <summary>
    /// 충돌-안전 이동(단일 관문).
    /// - 벽에 닿으면 '감속'이 아니라 '절단 or 같은 프레임 슬라이드'만 수행.
    /// - 슬라이드는 작은 반복(2~3회)으로 남은 예산을 같은 프레임에 최대한 소진.
    /// </summary>
    public MoveResult SweepMove(Vector2 desiredDelta)
    {
        MoveResult result = default;
        if (desiredDelta.sqrMagnitude <= 0f) return result;

        Vector2 startPos = transform.position;
        float remaining = desiredDelta.magnitude;
        Vector2 wishDir = desiredDelta.normalized;

        int iterations = 0;
        const int kMaxSlideIters = 3;

        while (remaining > 1e-5f && iterations < kMaxSlideIters)
        {
            iterations++;

            // 1) 벽 우선
            var wallHit = Physics2D.CircleCast((Vector2)transform.position, current.radius, wishDir, remaining, current.wallsMask);
            if (wallHit.collider)
            {
                float toHit = wallHit.distance;
                if (toHit > 0f) MoveDiscrete(wishDir * toHit);

                result.hitWall = true;
                result.hitTransform = wallHit.transform;
                result.hitNormal = wallHit.normal;

                remaining -= Mathf.Max(0f, toHit);

                if (current.allowWallSlide)
                {
                    // 접선 성분 추출: t = v - n*(v·n)
                    Vector2 n = wallHit.normal;
                    Vector2 v = wishDir * remaining;
                    Vector2 tangential = v - Vector2.Dot(v, n) * n;

                    if (tangential.sqrMagnitude > 1e-6f)
                    {
                        wishDir = tangential.normalized;
                        remaining = tangential.magnitude;
                        continue; // 같은 프레임에 접선으로 재시도
                    }
                }

                // 슬라이드 못하면 그 프레임 절단
                remaining = 0f;
                break;
            }

            // 2) 적 차단(정책)
            if (current.enemyAsBlocker && current.enemyMask.value != 0)
            {
                var enemyHit = Physics2D.CircleCast((Vector2)transform.position, current.radius, wishDir, remaining, current.enemyMask);
                if (enemyHit.collider)
                {
                    float toHit = enemyHit.distance;
                    if (toHit > 0f) MoveDiscrete(wishDir * toHit);
                    result.hitEnemy = true;
                    result.hitTransform = enemyHit.transform;
                    result.hitNormal = enemyHit.normal;
                    remaining -= Mathf.Max(0f, toHit);
                    remaining = 0f; // 적은 슬라이드 대상 아님(정책에 따라 필요 시 확장)
                    break;
                }
            }

            // 3) 충돌 없음 → 남은 거리 전부 이동
            MoveDiscrete(wishDir * remaining);
            remaining = 0f;
        }

        result.actualDelta = (Vector2)transform.position - startPos;
        LastMoveVector = result.actualDelta;
        return result;
    }

    void MoveDiscrete(Vector2 delta)
    {
        if (delta.sqrMagnitude <= 0f) return;
        if (rb) rb.MovePosition((Vector2)transform.position + delta);
        else transform.position += (Vector3)delta;
    }

    public CollisionPolicy CurrentPolicy => current;
}
