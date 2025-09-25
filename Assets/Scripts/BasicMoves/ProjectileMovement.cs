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

    // �� �̹� ���� �ݶ��̴� ��Ÿ�� ����
    readonly HashSet<int> _hitIds = new();
    const float SKIN = 0.01f; // �浹���� ��¦ �Ѿ����

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

        // ����
        speed = Mathf.Max(0f, speed + P.acceleration * dt);

        // Ÿ�� ��ȿ�� Ȯ�� + ��Ÿ����
        if (target == null && P.retargetOnLost)
            TryRetarget();

        Vector2 pos = transform.position;

		// ���ϴ� ����(����)
		//Vector2 desired = target ? ((Vector2)target.position - pos).normalized : dir;
		Vector2 desired = target?.name == "Anchor" ? dir : ((Vector2)target.position - pos).normalized;
		float maxTurnRad = P.maxTurnDegPerSec * Mathf.Deg2Rad * dt;
        dir = Vector3.RotateTowards(dir, desired, maxTurnRad, 0f).normalized;

        // === �̵�/�浹(���� ��) ó�� ===
        float remaining = speed * dt;

        while (remaining > 0f)
        {
            pos = transform.position;

            // 1) �� üũ
            var wallHit = Physics2D.CircleCast(pos, P.radius, dir, remaining, P.blockerMask);
            if (wallHit.collider)
            {
                // ������ �̵� �� �Ҹ�
                Move(wallHit.distance);
                Expire();
                return;
            }

            // 2) �� üũ
            var enemyHit = Physics2D.CircleCast(pos, P.radius, dir, remaining, P.enemyMask);
            if (enemyHit.collider)
            {
                var c = enemyHit.collider;

                // ���� �ݶ��̴� �ߺ� Ÿ�� ����
                int id = c.GetInstanceID();
                if (!_hitIds.Contains(id))
                {
                    // �浹������ �̵�
                    Move(enemyHit.distance);

                    // ����/�˹� ����
                    if (c.TryGetComponent(out IVulnerable v))
                        v.TakeDamage(P.damage, P.apRatio);
                    if (c.attachedRigidbody)
                        c.attachedRigidbody.AddForce(dir * P.knockback, ForceMode2D.Impulse);

                    _hitIds.Add(id); // ���

                    // ���� �Ұ��̰ų�(=���� ��� �Ҹ�) / Ÿ�� �� ��ü�� �Ҹ�
                    if (!P.CanPenetrate || (target != null && c.transform == target))
                    {
                        Expire();
                        return;
                    }
                }
                else
                {
                    // �̹� ���� ����̸� �浹�������� ���� �� ���߰� ��� ó��
                    Move(enemyHit.distance);
                }

                // �浹���� ��¦ �Ѿ ���� ĳ��Ʈ���� ���� �鿡 �ɸ��� �ʰ�
                Move(SKIN);

                // �ܿ� �Ÿ� ����
                remaining -= enemyHit.distance + SKIN;
                continue; // ���� �浹/�̵� ó��
            }

            // 3) �浹 ������ ���� �Ÿ���ŭ �̵��ϰ� ����
            Move(remaining);
            remaining = 0f;
        }

        // ��Ÿ� üũ
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
		//���ŵ� �� ���� �ؾ� �Ѵ�?
        Destroy(gameObject);
    }
}