using UnityEngine;
using SkillInterfaces;
using ActInterfaces;
using System;
using System.Collections.Generic;

public class ProjectileMovement : MonoBehaviour, IExpirable
{
    MissileParams P;
    MechanismContext ctx;
    Transform owner, target;
    Vector2 dir;
    float speed, traveled, life;
    bool ended;
    public float Lifespan => life;

    readonly HashSet<int> _hitIds = new();
    const float SKIN = 0.01f;

    public void Init(MissileParams p, MechanismContext context)
    {
        P = p;
        ctx = context;
        owner = context.Owner;
        target = context.Target;

        Vector2 start = owner ? (Vector2)owner.position : Vector2.zero;
        Vector2 tgt = target ? (Vector2)target.position : start + Vector2.right;
        dir = (tgt - start).sqrMagnitude > 1e-6f ? (tgt - start).normalized : Vector2.right;
        speed = P.speed;

        var sr = gameObject.AddComponent<SpriteRenderer>();
        sr.sprite = GenerateDotSprite();
        sr.sortingOrder = 1000;
        transform.localScale = Vector3.one * (P.radius * 2f);
    }

    void Update()
    {
        float dt = Time.deltaTime;
        life += dt;
        if (life > P.maxLife)
        {
            Expire();
            return;
        }

        speed = Mathf.Max(0f, speed + P.acceleration * dt);

        if (target == null && P.retargetOnLost)
            TryRetarget();

        Vector2 pos = transform.position;
        Vector2 desired = target ? (Vector2)target.position - pos : dir;
        float maxTurnRad = P.maxTurnDegPerSec * Mathf.Deg2Rad * dt;
        dir = Vector3.RotateTowards(dir, desired.normalized, maxTurnRad, 0f).normalized;

        float remaining = speed * dt;

        while (remaining > 0f)
        {
            pos = transform.position;

            var wallHit = Physics2D.CircleCast(pos, P.radius, dir, remaining, P.blockerMask);
            if (wallHit.collider)
            {
                Move(wallHit.distance);
                Expire();
                return;
            }

            var enemyHit = Physics2D.CircleCast(pos, P.radius, dir, remaining, P.enemyMask);
            if (enemyHit.collider)
            {
                var c = enemyHit.collider;
                int id = c.GetInstanceID();
                if (!_hitIds.Contains(id))
                {
                    Move(enemyHit.distance);

                    if (c.TryGetComponent(out IVulnerable v))
                    {
                        v.TakeDamage(P.damage, P.apRatio);
                        ctx.EmitHook(AbilityHook.OnHit, c.transform, enemyHit.point, nameof(ProjectileMovement));
                    }
                    if (c.attachedRigidbody)
                        c.attachedRigidbody.AddForce(dir * P.knockback, ForceMode2D.Impulse);

                    _hitIds.Add(id);

                    if (!P.CanPenetrate || (target != null && c.transform == target))
                    {
                        Expire();
                        return;
                    }
                }
                else
                {
                    Move(enemyHit.distance);
                }

                Move(SKIN);
                remaining -= enemyHit.distance + SKIN;
                continue;
            }

            Move(remaining);
            remaining = 0f;
        }

        if (traveled >= P.maxRange)
            Expire();
    }

    void Move(float d)
    {
        transform.position += (Vector3)(dir * d);
        traveled += d;
    }

    void TryRetarget()
    {
        var hits = Physics2D.OverlapCircleAll(transform.position, P.retargetRadius, P.enemyMask);
        float best = float.PositiveInfinity;
        Transform bestT = null;
        foreach (var h in hits)
        {
            float dist = Vector2.SqrMagnitude((Vector2)h.bounds.center - (Vector2)transform.position);
            if (dist < best)
            {
                best = dist;
                bestT = h.transform;
            }
        }
        if (bestT) target = bestT;
    }

    Sprite GenerateDotSprite()
    {
        int s = 8;
        var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
        var col = new Color32[s * s];
        for (int i = 0; i < col.Length; i++) col[i] = new Color32(255, 255, 255, 255);
        tex.SetPixels32(col);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), s);
    }

    public void Expire()
    {
        if (ended) return;
        ended = true;
        ctx.EmitHook(AbilityHook.OnCastEnd, target, transform.position, nameof(ProjectileMovement));
        Destroy(gameObject);
    }
}
