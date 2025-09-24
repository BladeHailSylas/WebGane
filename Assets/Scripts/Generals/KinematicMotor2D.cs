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
	bool movable = true;

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
	public Vector2 RemoveComponent(Vector2 vector, LayerMask mask, MoveResult result) //어쩌면 이게 vfinal을?
	{
		Vector2 vfinal = vector;
		// 1) 벽 우선
		var maskHit = Physics2D.CircleCastAll((Vector2)transform.position, current.radius, vector.normalized, vector.magnitude, mask);
		if (mask == current.enemyMask && !current.enemyAsBlocker)
		{
			Debug.Log("Enemy is not blocker now; penetrating");
			return vector;
		}
		foreach (var hit in maskHit)
		{
			if (hit.collider)
			{
				// --- 이동하지 않는다. 법선 성분만 무효화하고 방향/잔여만 재설정 ---
				result.hitWall = true;
				result.hitTransform = hit.transform;
				result.hitNormal = hit.normal.normalized;

				Vector2 n = hit.normal.normalized;
				Vector2 v = vfinal;
				float dot = Vector2.Dot(v, n);
				var hitPt = hit.point;
				if (Mathf.Abs(dot) > 0f) //Abs?
				{
					v -= dot * n;            // v' = v - max(0,dot) * n
					vfinal = v;
				}
			}
		}
		return vfinal;
	}
    public MoveResult SweepMove(Vector2 desiredDelta)
    {
		var result = new MoveResult { actualDelta = Vector2.zero };
        if (desiredDelta.sqrMagnitude <= 0f) return result;
        Vector2 origin = transform.position;
        float remaining = desiredDelta.magnitude;
		Vector2 wishDir = desiredDelta.normalized;

		const int kMaxSlideIters = 3;
        int iters = 0;

        while (remaining > 1e-5f && iters++ < kMaxSlideIters)
        {
			Vector2 vfinal = wishDir * remaining;
			// 1) 벽 우선
			//vfinal = wishDir * remaining;
			vfinal = RemoveComponent(vfinal, current.wallsMask, result);
			// 2) 적 차단(기본값 true). “겹치기 금지” 정책 유지
			vfinal = RemoveComponent(vfinal, current.enemyMask, result);
			if(vfinal != RemoveComponent(vfinal, current.wallsMask, result) || vfinal != RemoveComponent(vfinal, current.enemyMask, result))
			{
				vfinal = Vector2.zero;
				break;
			}
			else if (vfinal.sqrMagnitude > 1e-6f)            // 접선(or 바깥) 성분이 남아 있으면 계속
			{
				wishDir = vfinal.normalized;
				remaining = vfinal.magnitude;
				//continue;                           // 같은 프레임 내 다음 캐스트로
			}
			// 더 진행할 수 없으면 그 프레임 종료
			else
			{
				break;
			}
			// 3) 충돌 없음 → 남은 거리 전부 이동
			//Debug.Log(wishDir * remaining);
			MoveDiscrete(vfinal);
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
