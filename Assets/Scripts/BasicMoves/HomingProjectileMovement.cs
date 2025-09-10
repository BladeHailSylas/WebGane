using UnityEngine;

public class HomingProjectileMover : MonoBehaviour
{
    HomingProjectileParams P;
    Transform owner, target;
    Vector2 dir;
    float speed, traveled, life;

    public void Init(HomingProjectileParams p, Transform owner, Transform target)
    {
        P = p; this.owner = owner; this.target = target;
        Vector2 start = owner.position;
        Vector2 tgt = target ? (Vector2)target.position : start + Vector2.right;
        dir = (tgt - start).normalized;
        speed = P.speed;

        // (테스트용 시각화) 점 스프라이트
        var sr = gameObject.AddComponent<SpriteRenderer>();
        sr.sprite = GenerateDotSprite();
        sr.sortingOrder = 1000;
        transform.localScale = Vector3.one * (P.radius * 2f);
    }

    void Update()
    {
        float dt = Time.deltaTime;
        life += dt; if (life > P.maxLife) { Destroy(gameObject); return; }

        // 가속
        speed = Mathf.Max(0f, speed + P.acceleration * dt);

        // 타깃 유효성 확인 + 재타깃팅
        if (target == null && P.retargetOnLost)
            TryRetarget();

        // 원하는 방향
        Vector2 pos = transform.position;
        Vector2 desired = target ? ((Vector2)target.position - pos).normalized : dir;

        // 회전 제한(°/s → rad/s)
        float maxTurnRad = P.maxTurnDegPerSec * Mathf.Deg2Rad * dt;
        Vector2 newDir = Vector3.RotateTowards(dir, desired, maxTurnRad, 0f);
        dir = newDir.normalized;

        // 이동 전 충돌 검사
        float step = speed * dt;
        var hitWall = Physics2D.CircleCast(pos, P.radius, dir, step, P.blockerMask);
        if (hitWall.collider)
        {
            Move(hitWall.distance);
            Destroy(gameObject); return;
        }

        var hitEnemy = Physics2D.CircleCast(pos, P.radius, dir, step, P.enemyMask);
        if (hitEnemy.collider)
        {
            Move(hitEnemy.distance);
            var c = hitEnemy.collider;
            if (c.TryGetComponent(out ActInterfaces.IVulnerable v))
                v.TakeDamage(P.damage, P.apRatio);
            if (c.attachedRigidbody)
                c.attachedRigidbody.AddForce(dir * P.knockback, ForceMode2D.Impulse);
            Destroy(gameObject); return;
        }

        // 이동/사거리 체크
        Move(step);
        if (traveled >= P.maxRange) Destroy(gameObject);
    }

    void Move(float d)
    {
        transform.position += (Vector3)(dir * d);
        traveled += d;
    }

    void TryRetarget()
    {
        // 간단: 주변 원형 탐색 후 가장 가까운 적 타깃
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
}