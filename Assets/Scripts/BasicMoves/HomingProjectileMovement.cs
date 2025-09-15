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

        // (�׽�Ʈ�� �ð�ȭ) �� ��������Ʈ
        var sr = gameObject.AddComponent<SpriteRenderer>(); //Prefab�� ���� �ʹٸ� ��� �ؾ� �ϴ°�? HomingParams���� Prefab�� �����ϰ� ���⼭ ȣ��?

        sr.sprite = GenerateDotSprite();
        sr.sortingOrder = 1000;
        transform.localScale = Vector3.one * (P.radius * 2f);
    }

    void Update()
    {
        float dt = Time.deltaTime; //�ð� ���, IExpirable�� �ٿ��� �ұ�
        life += dt; if (life > P.maxLife) { Destroy(gameObject); return; }

        // ����, ���� �ӷ� ���� �ʿ�?
        speed = Mathf.Max(0f, speed + P.acceleration * dt);

        // Ÿ�� ��ȿ�� Ȯ�� + ��Ÿ����
        if (target == null && P.retargetOnLost)
            TryRetarget();

        // ���ϴ� ����
        Vector2 pos = transform.position;
        Vector2 desired = target ? ((Vector2)target.position - pos).normalized : dir;

        // ȸ�� ����(��/s �� rad/s)
        float maxTurnRad = P.maxTurnDegPerSec * Mathf.Deg2Rad * dt;
        Vector2 newDir = Vector3.RotateTowards(dir, desired, maxTurnRad, 0f);
        dir = newDir.normalized;

        // �̵� �� �浹 �˻�
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

        // �̵�/��Ÿ� üũ
        Move(step);
        if (traveled >= P.maxRange) Destroy(gameObject);
    }

    void Move(float d)
    {
        transform.position += (Vector3)(dir * d);
        traveled += d;
    }
    /// <summary>
    /// Ÿ���� ������� ��� ����� �ٽ� Ÿ���ϴ� �޼���,
    /// "�ʵ��� ����� ����" �ϴ� ���, ����� �ʵ忡�� �������(�����̵� ��) ��ȿ�� �ǹǷ� NO,
    /// ���� "����� ���� ����" �ϴ� ��� ������ �����ؾ� ��(����� �ĺ��� �� �ִ� ���𰡰� �ʿ�)
    /// ������ ���� ����� ���� Ÿ��������, �װ��� ���� ó���� ������ ���� ���� ����Ƽ�� �ƴ� �� ����(�׸��� �Ƹ� �ƴ� ��)
    /// </summary>
    void TryRetarget()
    {
        // ����: �ֺ� ���� Ž�� �� ���� ����� �� Ÿ��
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
    /// ��������Ʈ ����, �׷��� ���� �Ͼ� �׸� ���ͼ� �׳� �ӽ÷� ��� ��,
    /// ����ü�� ����� �� Callback�� �����ϴ� ��쵵 �ְ� �ؼ� ����ü�� Prefab�� �ʿ��� ��,
    /// ���� �� Prefab�� ��� ������Ĵ� �� �ٸ� ����
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