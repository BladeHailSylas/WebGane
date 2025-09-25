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
        skin = 0.125f,
        allowWallSlide = true
    };

    Rigidbody2D rb;
    Collider2D col;
    CollisionPolicy current;
	//bool movable = true;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        current = defaultPolicy;
        col = rb.GetComponent<Collider2D>();
		Debug.Log(col.isTrigger);
    }

    public IDisposable With(in CollisionPolicy overridePolicy)
    {
        var prev = current;
        current = overridePolicy;
        return new Scope(() => current = prev);
    }
    sealed class Scope : IDisposable { readonly Action onDispose; public Scope(Action a) { onDispose = a; } public void Dispose() { onDispose?.Invoke(); } }
	// [RULE: Depenetrate/MTV] 시작 겹침 탈출(벽+적 모두, 최소 이탈 벡터)
	public Vector2 RemoveNormalComponent(Vector2 vector, LayerMask mask, MoveResult result)
	{
		Vector2 vfinal = vector;
		// 1) 벽 우선
		var maskHit = Physics2D.CircleCastAll((Vector2)transform.position, current.radius, vector.normalized, vector.magnitude, mask);
		foreach (var hit in maskHit)
		{
			if(mask == current.enemyMask && !current.enemyAsBlocker)
			{
				//Debug.Log("Hello again");
				vfinal = vector;
			}
			else if (hit.collider)
			{
				//법선 성분을 무효로 한다
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
					vfinal = v; 			//벡터 연산을 중첩시키기 위해서 vfinal에 대입
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

		const int kMaxSlideIters = 4;
        int iters = 0;

        while (remaining > 1e-5f && iters++ < kMaxSlideIters)
        {
			Vector2 vfinal = wishDir * remaining;
			// 1) 벽 우선
			//vfinal = wishDir * remaining;
			vfinal = RemoveNormalComponent(vfinal, current.wallsMask, result);
			// 2) 적 차단(기본값 true). “겹치기 금지” 정책 유지
			vfinal = RemoveNormalComponent(vfinal, current.enemyMask, result);
			//↓RemoveComponent를 2번 수행했을 때 제거되는 성분이 있다면(즉, 벡터가 수평도 수직도 아니라서 어느 한쪽의 접선이 다른 쪽을 넘을 경우) 이동을 무효로
			if(vfinal != RemoveNormalComponent(vfinal, current.wallsMask, result) || vfinal != RemoveNormalComponent(vfinal, current.enemyMask, result))
			{
				//vfinal = Vector2.zero;
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
			// 실제 이동을 여기서 수행, 이동할 수 없는 이유가 있다면 위의 분기점에서 break가 호출되어 실행되지 않음
			MoveDiscrete(vfinal);
            remaining = 0f;
        }
        result.actualDelta = (Vector2)transform.position - origin;
        return result;
    }

    void MoveDiscrete(Vector2 delta)
    {
        if (delta.sqrMagnitude <= 0f) return;
        if (rb) rb.MovePosition((Vector2)transform.position + delta);
        else transform.position += (Vector3)delta;
    }

    public CollisionPolicy CurrentPolicy => current;
	/// <summary>
	/// 현재 위치에서 Blocker(환경)들과의 겹침을 검사하여
	/// "한 번"의 최소 이동 벡터(MTD)를 계산해 반환합니다.
	/// - 실제 위치 이동은 하지 않습니다. (Depenetration()이 적용 담당)
	/// - 합성형(여러 침투벡터 합산) 방식으로 단일 MTD를 구합니다.
	/// </summary>
	/// <param name="blockersMask">Walls&Obstacles (+ 선택적으로 Enemy)</param>
	/// <param name="maxIterations">미사용(반복은 Depenetration에서 수행). 시그니처 유지용.</param>
	/// <param name="skin">스킨 마진. 기본 0.03125(=2^-5)</param>
	/// <param name="minEps">보정 문턱. 이보다 작으면 0으로 간주</param>
	/// <param name="maxTotal">총 보정 상한(Depenetration에서 사용). 시그니처 유지용.</param>
	/// <returns>이번 스텝에서 적용할 단일 MTD(보정 벡터). 없으면 (0,0)</returns>
	public Vector2 DepenVector(LayerMask blockersMask, int maxIterations = 4, float skin = 0.125f, float minEps = 0.001f, float maxTotal = 0.5f)
	{
		// 안전 체크
		if (rb == null || col == null)
			return Vector2.zero;

		// ContactFilter2D 구성: Blocker 레이어만, Trigger 무시
		ContactFilter2D filter = new() { useLayerMask = true };
		filter.SetLayerMask(blockersMask);
		filter.useTriggers = false;

		// 겹침 수집 버퍼 (필드 추가 금지 조건에 맞춰 지역 배열 사용)
		Collider2D[] hits = new Collider2D[16];
		int count = col.Overlap(filter, hits);
		if (count <= 0)
			return Vector2.zero;

		// --- 합성형 MTD 누적 ---
		Vector2 accum = Vector2.zero;
		int validContacts = 0;

		for (int i = 0; i < count; i++)
		{
			var other = hits[i];
			if (!other) continue;

			// 현재 Player 콜라이더(col) 기준으로 거리/법선 계산
			// d.isOverlapped == true 이면 d.distance 는 "겹침 해소에 필요한 양(>0)"
			ColliderDistance2D d = col.Distance(other);
			if (!d.isOverlapped) continue;

			// 누적(법선 * 거리) — 합성형
			accum += d.normal * d.distance;
			validContacts++;
		}

		if (validContacts == 0)
			return Vector2.zero;

		float mag = accum.magnitude;
		if (mag < minEps)
			return Vector2.zero;

		// 스킨 마진을 더해 "살짝 더" 바깥으로 밀어냄
		Vector2 mtd = (accum / mag) * (mag + skin);

		/*** 디버그(기본 비활성, 필요시 주석 해제)
        // 개별 접촉 법선 시각화
        for (int i = 0; i < count; i++)
        {
            var other = hits[i];
            if (!other) continue;
            var d = col.Distance(other);
            if (!d.isOverlapped) continue;

            Vector2 p = rb.position; // 대략적인 기준점
            Debug.DrawRay(p, d.normal * Mathf.Max(d.distance, 0.02f), Color.cyan, 0.02f);
        }
        // 최종 MTD(주황)
        Debug.DrawRay(rb.position, mtd, new Color(1f, 0.5f, 0f), 0.02f);
        ***/

		return mtd;
	}

	/// <summary>
	/// DepenVector()를 반복 호출하여 실제로 위치 보정(MovePosition)을 수행합니다.
	/// - 최대 반복 4회
	/// - 스킨 0.03125
	/// - 문턱 0.001
	/// - 총 보정 상한 0.5m
	/// - 마스크: current.wallsMask | (current.enemyAsBlocker ? current.enemyMask : 0)
	/// </summary>
	public void Depenetration()
	{
		if (rb == null || col == null)
			return;

		// 1) Blocker 마스크 구성
		LayerMask blockersMask = current.wallsMask;
		if (current.enemyAsBlocker)
			blockersMask |= current.enemyMask;

		// 2) 반복 보정 파라미터(요구사항 고정값 반영)
		int maxIterations = 4;
		float skin = 0.125f;
		float minEps = 0.001f;
		float maxTotal = 0.5f;

		Vector2 total = Vector2.zero;

		for (int it = 0; it < maxIterations; it++)
		{
			// 현재 위치에서 "한 번"의 MTD 계산
			Vector2 mtd = DepenVector(blockersMask, maxIterations, skin, minEps, maxTotal);
			if (mtd.sqrMagnitude <= (minEps * minEps))
				break;

			// 총 상한 체크 (안전장치)
			if ((total + mtd).magnitude > maxTotal)
			{
				// 필요시 로그를 남기세요 (지오메트리/레이어 설정 문제일 수 있습니다)
				// Debug.LogWarning($"Depenetration exceeded maxTotal({maxTotal}). Clamping.");
				// 상한을 넘지 않도록 mtd를 클램프
				Vector2 dir = mtd.normalized;
				float remain = Mathf.Max(0f, maxTotal - total.magnitude);
				mtd = dir * remain;
			}

			// 실제 위치 보정 — MovePosition 사용(요구사항 #3)
			rb.MovePosition(rb.position + mtd);

			total += mtd;

			// 총 상한에 도달하면 중단
			if (Mathf.Abs(maxTotal - total.magnitude) <= 1e-5f)
				break;
		}

		/*** 디버그(기본 비활성, 필요시 주석 해제)
        if (total.sqrMagnitude > 0f)
        {
            Debug.DrawRay(rb.position - total, total, Color.yellow, 0.05f); // 전체 보정(노랑)
        }
        ***/
	}

	/*
    // 참고: "최심 침투 우선형" 알고리즘 개요(요구사항 4 — 실제 구현 X, 주석만)
    //
    // for (it=0; it<maxIterations; it++):
    //   overlaps = OverlapCollider(...)
    //   if overlaps.empty: break
    //   pick = argmax(overlaps, by d.distance)   // 가장 깊은 침투 1개 선택
    //   mtd  = pick.normal * (pick.distance + skin)
    //   rb.MovePosition(rb.position + mtd)
    //   // 다음 반복에서 겹침 재평가
    //
    // 장점: 안정적(최심 해소부터) / 단점: 반복 횟수가 늘 수 있음
    */
}
