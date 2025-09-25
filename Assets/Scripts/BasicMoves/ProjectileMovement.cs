using UnityEngine;
using ActInterfaces;
using System;
using System.Collections.Generic;

public class ProjectileMovement : MonoBehaviour, IExpirable
{
    MissileParams P;
    Transform owner, target;
    Vector2 dir;
    float speed, traveled, life;
    public float Lifespan => life;

    // ★ 이미 맞춘 콜라이더 재타격 방지
    readonly HashSet<int> _hitIds = new();
    const float SKIN = 0.01f; // 충돌면을 살짝 넘어가도록

    public void Init(MissileParams p, Transform owner, Transform target)
    {
        P = p; this.owner = owner; this.target = target;
        Vector2 start = owner.position;
        Vector2 tgt = target ? (Vector2)target.position : start + Vector2.right;
        dir = (tgt - start).normalized;
        speed = P.speed;

        var sr = gameObject.AddComponent<SpriteRenderer>();
        sr.sprite = GenerateDotSprite();
        sr.sortingOrder = 1000;
        transform.localScale = Vector3.one * (P.radius * 2f);
		//Debug.Log($"target {this.target.name}");
    }

    void Update()
    {
        float dt = Time.deltaTime;
        life += dt; if (life > P.maxLife) { Expire(); }

        // 가속
        speed = Mathf.Max(0f, speed + P.acceleration * dt);

        // 타깃 유효성 확인 + 재타깃팅
        if (target == null && P.retargetOnLost)
            TryRetarget();

        Vector2 pos = transform.position;

		// 원하는 방향(유도)
		//Vector2 desired = target ? ((Vector2)target.position - pos).normalized : dir;
		Vector2 desired = target?.name == "Anchor" ? dir : ((Vector2)target.position - pos).normalized;
		float maxTurnRad = P.maxTurnDegPerSec * Mathf.Deg2Rad * dt;
        dir = Vector3.RotateTowards(dir, desired, maxTurnRad, 0f).normalized;

        // === 이동/충돌(여러 번) 처리 ===
        float remaining = speed * dt;

        while (remaining > 0f)
        {
            pos = transform.position;

            // 1) 벽 체크
            var wallHit = Physics2D.CircleCast(pos, P.radius, dir, remaining, P.blockerMask);
            if (wallHit.collider)
            {
                // 벽까지 이동 후 소멸
                Move(wallHit.distance);
                Expire();
                return;
            }

            // 2) 적 체크
            var enemyHit = Physics2D.CircleCast(pos, P.radius, dir, remaining, P.enemyMask);
            if (enemyHit.collider)
            {
                var c = enemyHit.collider;

                // 같은 콜라이더 중복 타격 방지
                int id = c.GetInstanceID();
                if (!_hitIds.Contains(id))
                {
                    // 충돌점까지 이동
                    Move(enemyHit.distance);

                    // 피해/넉백 적용
                    if (c.TryGetComponent(out IVulnerable v))
                        v.TakeDamage(P.damage, P.apRatio);
                    if (c.attachedRigidbody)
                        c.attachedRigidbody.AddForce(dir * P.knockback, ForceMode2D.Impulse);

                    _hitIds.Add(id); // 기록

                    // 관통 불가이거나(=명중 즉시 소멸) / 타깃 그 자체면 소멸
                    if (!P.CanPenetrate || (target != null && c.transform == target))
                    {
                        Expire();
                        return;
                    }
                }
                else
                {
                    // 이미 맞춘 대상이면 충돌점까지는 굳이 안 멈추고 통과 처리
                    Move(enemyHit.distance);
                }

                // 충돌면을 살짝 넘어가 다음 캐스트에서 같은 면에 걸리지 않게
                Move(SKIN);

                // 잔여 거리 갱신
                remaining -= enemyHit.distance + SKIN;
                continue; // 다음 충돌/이동 처리
            }

            // 3) 충돌 없으면 남은 거리만큼 이동하고 종료
            Move(remaining);
            remaining = 0f;
        }

        // 사거리 체크
        if (traveled >= P.maxRange) Expire();
    }

    void Move(float d)
    {
        transform.position += (Vector3)(dir * d);
        traveled += d;
    }

    void TryRetarget()
    {
        var hits = Physics2D.OverlapCircleAll(transform.position, P.retargetRadius, P.enemyMask);
        float best = float.PositiveInfinity; Transform bestT = null;
        foreach (var h in hits)
        {
            float d = Vector2.SqrMagnitude((Vector2)h.bounds.center - (Vector2)transform.position);
            if (d < best) { best = d; bestT = h.transform; }
        }
        if (bestT) target = bestT;
    }

    Sprite GenerateDotSprite()
    {
        int s = 8; var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
        var col = new Color32[s * s]; for (int i = 0; i < col.Length; i++) col[i] = new Color32(255, 255, 255, 255);
        tex.SetPixels32(col); tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), s);
    }

    public void Expire()
    {
		//제거될 때 뭔가 해야 한다?
        Destroy(gameObject);
    }
}