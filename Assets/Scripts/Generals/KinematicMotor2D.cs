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
        if (worstPen < 0.15f && mtv != Vector2.zero)
        {
			float moveOut = Mathf.Max(0.01f, current.skin + Mathf.Abs(worstPen));// - worstPen; // -> worstPen이 들어가면 떨림이 너무 심함
            MoveDiscrete(mtv.normalized * moveOut);
			Debug.LogError($"HELP! {worstPen} {current.skin} {moveOut}");
		}
    }
    public MoveResult SweepMove(Vector2 desiredDelta)
    {
		var ow = Physics2D.OverlapCircleAll(transform.position, current.radius, current.wallsMask);
		var oe = Physics2D.OverlapCircleAll(transform.position, current.radius, current.enemyMask);
		var result = new MoveResult { actualDelta = Vector2.zero };
        if (desiredDelta.sqrMagnitude <= 0f) return result;

		// (A) 프레임 시작 겹침은 여기서도 한 번 더 안전하게 치운다
		if (ow.Length > 0 || oe.Length > 0)
		{
			BeginFrameDepenetrate(Vector2.zero); //Overlap이 좀 심할 경우에 호출하기 -> 일단 비활성화
		}
        //Debug.Log("After call Depen");

        Vector2 origin = transform.position;
        float remaining = desiredDelta.magnitude;
        Vector2 wishDir = desiredDelta.normalized;

        const int kMaxSlideIters = 3;
        int iters = 0;

        while (remaining > 1e-5f && iters++ < kMaxSlideIters)
        {
			// 1) 벽 우선
			Vector2 vfinal = wishDir * remaining;
			var wallHit = Physics2D.CircleCastAll((Vector2)transform.position, current.radius, wishDir, remaining, current.wallsMask);
            foreach(RaycastHit2D hit in wallHit)
			{
				if (hit.collider)
				{
					Debug.LogWarning($"Wall {hit.transform.name}");
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
			// 2) 적 차단(기본값 true). “겹치기 금지” 정책 유지
			if (current.enemyAsBlocker && current.enemyMask.value != 0)
            {
				var enemyHit = Physics2D.CircleCastAll((Vector2)transform.position, current.radius, wishDir, remaining, current.enemyMask);
				foreach (RaycastHit2D hit in enemyHit)
				{
					if (hit.collider)
					{
						Debug.LogWarning($"Enemy {hit.transform.name}");
						if(wallHit.Length > 0) Debug.LogError($"Enemy after wall; Beware");
						// --- 이동하지 않는다. 법선 성분만 무효화하고 방향/잔여만 재설정 ---
						result.hitWall = true;
						result.hitTransform = hit.transform;
						result.hitNormal = hit.normal.normalized;
						Vector2 n = hit.normal.normalized;
						Vector2 v = vfinal;
						float dot = Vector2.Dot(v, n);
						var hitPt = hit.point;
						if (Mathf.Abs(dot) > 0f && vfinal != Vector2.zero) //Abs?
						{
							v -= dot * n;            // v' = v - max(0,dot) * n
							vfinal = v;
						}
					}
				}
			}
			if (vfinal.sqrMagnitude > 1e-6f)            // 접선(or 바깥) 성분이 남아 있으면 계속
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
			Debug.Log(wishDir * remaining);
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
