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

    // ★추가: 벽과의 접촉 시 '감속' 대신 접선 슬라이드를 허용할지(대시·이동 개별 정책으로 제어)
    public bool allowWallSlide;
}

public struct MoveResult
{
    public Vector2 actualDelta;      // 실제 이동량
    public bool hitWall;
    public bool hitEnemy;
    public Transform hitTransform;
    public Vector2 hitNormal;        // 첫 충돌의 법선(슬라이드 시 유용)
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
        enemyAsBlocker = false,     // ★기본 이동은 적을 '차단하지 않음'으로 전환(멈춤 현상 방지)
        radius = 0.5f,
        skin = 0.05f,
        allowWallSlide = true       // 기본은 슬라이드 허용(대시/이동에서 개별 오버라이드)
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
    /// 프레임 시작 겹침 해소. 
    /// - 이전 코드의 "preferredDir 없으면 transform.right(+X)로 민다"를 제거: 원치 않는 +X 밀림 방지.
    /// - 호출자가 명확한 선호 방향(최근 이동/대시 방향)을 전달할 때만 사용하세요.
    /// </summary>
    public void BeginFrameDepenetrate(Vector2 preferredDir)
    {
        if (preferredDir.sqrMagnitude <= 1e-6f) return; // ★임의 +X 밀림 제거
        if (Physics2D.OverlapCircle(transform.position, current.radius, current.wallsMask))
        {
            var dir = preferredDir.normalized;
            MoveDiscrete(dir * (current.radius + Mathf.Max(0.01f, current.skin)));
        }
    }

    /// <summary>
    /// 충돌-안전 이동(단일 관문).
    /// - 벽과의 첫 충돌에서 이동을 '감속'하지 않고, 정책이 허용하면 동일 프레임 내에서 '접선 슬라이드'를 시도.
    /// - 적 차단은 정책으로만 결정(기본 이동은 false, 대시 비관통은 true로 오버라이드).
    /// </summary>
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

            // ★슬라이드: 감속 대신 첫 법선에 대한 접선 성분을 '같은 프레임'에 소비 (모터 내부에서만 2차 캐스트 허용)
            result.hitWall = true;
            result.hitTransform = wallHit.transform;
            result.hitNormal = wallHit.normal;

            Vector2 moved = (Vector2)transform.position - origin;
            float remaining = Mathf.Max(0f, dist - moved.magnitude);

            if (current.allowWallSlide && remaining > 1e-5f)
            {
                // 접선 성분 계산: t = desiredDelta - n*(desiredDelta·n)
                Vector2 n = wallHit.normal.normalized;
                Vector2 tangential = desiredDelta - Vector2.Dot(desiredDelta, n) * n;

                if (tangential.sqrMagnitude > 1e-6f)
                {
                    Vector2 tdir = tangential.normalized;
                    float tdist = Mathf.Min(remaining, tangential.magnitude);

                    // 접선으로 한 번 더 캐스트(벽 → 적 순)
                    var w2 = Physics2D.CircleCast((Vector2)transform.position, current.radius, tdir, tdist, current.wallsMask);
                    if (w2.collider)
                    {
                        float tToHit = w2.distance;
                        if (tToHit > 0f) MoveDiscrete(tdir * tToHit);
                        // 벽에 다시 막히면 여기서 슬라이드 종료(감속 없음, '절단' 규칙)
                    }
                    else
                    {
                        if (current.enemyAsBlocker && current.enemyMask.value != 0)
                        {
                            var e2 = Physics2D.CircleCast((Vector2)transform.position, current.radius, tdir, tdist, current.enemyMask);
                            if (e2.collider)
                            {
                                float tToHit = e2.distance;
                                if (tToHit > 0f) MoveDiscrete(tdir * tToHit);
                                result.hitEnemy = true;
                                result.hitTransform = e2.transform;
                                result.hitNormal = e2.normal;
                            }
                            else
                            {
                                MoveDiscrete(tdir * tdist);
                            }
                        }
                        else
                        {
                            MoveDiscrete(tdir * tdist);
                        }
                    }
                }
            }

            result.actualDelta = (Vector2)transform.position - origin;
            LastMoveVector = result.actualDelta;
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
                LastMoveVector = result.actualDelta;
                return result;
            }
        }

        // 3) 충돌 없음 → 전부 이동
        MoveDiscrete(desiredDelta);
        result.actualDelta = desiredDelta;
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