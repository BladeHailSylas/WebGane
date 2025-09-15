using UnityEngine;
using ActInterfaces;

public class HomingProjectileMovement : MonoBehaviour
{
    HomingParams P;
    Transform owner, target;
    Vector2 dir;
    float speed, traveled, life;

    public void Init(HomingParams p, Transform owner, Transform target)
    {
        P = p; this.owner = owner; this.target = target;
        Vector2 start = owner.position;
        Vector2 tgt = target ? (Vector2)target.position : start + Vector2.right;
        dir = (tgt - start).normalized;
        speed = P.speed;

        // (테스트용 시각화) 점 스프라이트
        var sr = gameObject.AddComponent<SpriteRenderer>(); //Prefab을 쓰고 싶다면 어떻게 해야 하는가? HomingParams에서 Prefab을 참조하고 여기서 호출?

        sr.sprite = GenerateDotSprite();
        sr.sortingOrder = 1000;
        transform.localScale = Vector3.one * (P.radius * 2f);
    }

    void Update()
    {
        float dt = Time.deltaTime; //시간 재기, IExpirable을 붙여야 할까
        life += dt; if (life > P.maxLife) { Destroy(gameObject); return; }

        // 가속, 최저 속력 보정 필요?
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
            if (c.TryGetComponent(out IVulnerable v))
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
    /// <summary>
    /// 타깃이 사라졌을 경우 대상을 다시 타깃하는 메서드,
    /// "필드의 대상을 추적" 하는 경우, 대상이 필드에서 사라지면(순간이동 등) 무효가 되므로 NO,
    /// 한편 "대상을 절대 추적" 하는 경우 로직을 수정해야 됨(대상을 식별할 수 있는 무언가가 필요)
    /// 지금은 가장 가까운 적을 타깃하지만, 그것이 내가 처음에 지목한 대상과 같은 엔터티가 아닐 수 있음(그리고 아마 아닐 것)
    /// </summary>
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
    /// <summary>
    /// 스프라이트 생성, 그런데 무슨 하얀 네모가 나와서 그냥 임시로 써야 함,
    /// 투사체가 사라질 때 Callback이 존재하는 경우도 있고 해서 투사체는 Prefab이 필요할 듯,
    /// 물론 그 Prefab을 어떻게 만드느냐는 또 다른 문제
    /// </summary>
    /// <returns></returns>
    Sprite GenerateDotSprite()
    {
        int s = 8; var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
        var col = new Color32[s * s]; for (int i = 0; i < col.Length; i++) col[i] = new Color32(255, 255, 255, 255);
        tex.SetPixels32(col); tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), s);
    }
}