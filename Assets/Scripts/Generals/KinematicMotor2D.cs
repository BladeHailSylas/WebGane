using System;
using UnityEngine;
using ActInterfaces;

[Serializable]
public struct CollisionPolicy
{
    public LayerMask wallsMask;    
    public LayerMask enemyMask;  
    public bool enemyAsBlocker;    
    public float radius;            
    public float skin;               
    public bool allowWallSlide;      
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
[RequireComponent(typeof(Collider2D))]
public class KinematicMotor2D : MonoBehaviour, ISweepable
{
    [Header("Defaults")]
    public CollisionPolicy defaultPolicy = new()
    {
        wallsMask = 0,
        enemyMask = 0,
        enemyAsBlocker = true,
        radius = 0.5f,
        skin = 0.1f,
        allowWallSlide = true
    };

    Rigidbody2D rb;
    Collider2D col;
    CollisionPolicy current;
    public Vector2 LastMoveVector { get; private set; }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        current = defaultPolicy;
        col = rb.GetComponent<Collider2D>();
    }

    public IDisposable With(in CollisionPolicy overridePolicy)
    {
        var prev = current;
        current = overridePolicy;
        return new Scope(() => current = prev);
    }
    sealed class Scope : IDisposable { readonly Action onDispose; public Scope(Action a) { onDispose = a; } public void Dispose() { onDispose?.Invoke(); } }
    // [RULE: Depenetrate/MTV] 시작 겹침 탈출(벽+적 모두, 최소 이탈 벡터)
    public void BeginFrameDepenetrate(Vector2 _ignored)
    {
        if (!col) return;
        float worstPen = 0.15f;          // 가장 깊은(가장 음수) 침투량 -> 침투 시에도 worstPen이 0 밑으로 내려가지 않음(대략 0.15 미만), 수치 조정으로 해결
        Vector2 mtv = Vector2.zero;   // 밖으로 나갈 방향(최대 침투의 법선)

        // 벽 + (정책에 따라) 적 모두 스캔
        int mask = current.wallsMask | current.enemyMask;
        var overlapped = Physics2D.OverlapCircleAll(transform.position, current.radius, mask);
        foreach (var other in overlapped)
        {
            if (!other) continue;
            var d = Physics2D.Distance(col, other);
            // d.distance < 0 이면 침투 중, d.normal: myCol에서 바깥으로 향하는 법선
            if (d.distance <= worstPen)
            {
                worstPen = d.distance;
                mtv = d.normal; // 밖으로
            }
		}
        if (worstPen <= 0.15f && mtv != Vector2.zero)
        {
            float moveOut = Mathf.Max(0.01f, current.skin);// + worstPen; // -> worstPen이 들어가면 떨림이 너무 심함
            MoveDiscrete(mtv.normalized * moveOut);
			//Debug.LogError($"HELP! {worstPen} {current.skin} {moveOut}");
		}
    }
    public MoveResult SweepMove(Vector2 desiredDelta)
    {
		var ow = Physics2D.OverlapCircleAll(transform.position, current.radius, current.wallsMask);
		var oe = Physics2D.OverlapCircleAll(transform.position, current.radius, current.enemyMask);
		var result = new MoveResult { actualDelta = Vector2.zero };
        if (desiredDelta.sqrMagnitude <= 0f) return result;

        // (A) 프레임 시작 겹침은 여기서도 한 번 더 안전하게 치운다
        if(ow.Length > 0.25f || oe.Length > 0.25f) BeginFrameDepenetrate(Vector2.zero);
        //Debug.Log("After call Depen");

        Vector2 origin = transform.position;
        float remaining = desiredDelta.magnitude;
        Vector2 wishDir = desiredDelta.normalized;

        const int kMaxSlideIters = 3;
        int iters = 0;

        while (remaining > 1e-5f && iters++ < kMaxSlideIters)
        {
            // 1) 벽 우선
            var wallHit = Physics2D.CircleCast((Vector2)transform.position, current.radius, wishDir, remaining, current.wallsMask);
            if (wallHit.collider)
            {
                float toHit = wallHit.distance;
                if (toHit > 0f)
                {
                    MoveDiscrete(wishDir * toHit);
                }

                result.hitWall = true;
                result.hitTransform = wallHit.transform;
                result.hitNormal = wallHit.normal;
                remaining -= Mathf.Max(0f, toHit);

                // 접선 슬라이드: v' = v - n * (v·n)
                // 법선은? 법선을 0으로 하면 좋지 않을까
                Vector2 n = wallHit.normal;
                Vector2 v = wishDir * remaining;
                Vector2 tangential = v - Vector2.Dot(v, n) * n;
                if (tangential.sqrMagnitude >= 1e-6f )
                {
                    wishDir = tangential.normalized;
                    remaining = tangential.magnitude;
                    continue; // 같은 프레임에 재시도
                }
                remaining = 0f; // 더 못 감
                break;
            }

            // 2) 적 차단(기본값 true). “겹치기 금지” 정책 유지
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
                    remaining = 0f; // 적은 슬라이드 대상 아님
                    break;
                }
            }

            // 3) 충돌 없음 → 남은 거리 전부 이동
            MoveDiscrete(wishDir * remaining);
            remaining = 0f;
        }

        result.actualDelta = (Vector2)transform.position - origin;
        LastMoveVector = result.actualDelta; // ★ 히트/비히트 모든 경로에서 갱신
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
