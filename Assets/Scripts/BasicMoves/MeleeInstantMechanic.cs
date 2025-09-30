using SkillInterfaces;
using System.Collections;
using UnityEngine;

[CreateAssetMenu(menuName = "Mechanics/Melee Instant")]
public class MeleeInstantMechanic : SkillMechanismBase<MeleeParams>
{
    protected override IEnumerator Execute(MechanismContext ctx, MeleeParams p)
    {
        var owner = ctx.Owner;
        if (!owner)
        {
            Debug.LogWarning("[MeleeInstantMechanic] Owner transform가 존재하지 않습니다.");
            yield break;
        }

        if (p.windup > 0f) yield return new WaitForSeconds(p.windup);

        Vector2 origin = owner.position;
        Vector2 fwd = GetAimDir(ctx.Camera, origin);
        float half = p.angleDeg * 0.5f;

        var hits = Physics2D.OverlapCircleAll(origin, p.radius, p.enemyMask);
        foreach (var c in hits)
        {
            if (p.angleDeg < 359f)
            {
                Vector2 to = (Vector2)c.bounds.ClosestPoint(origin) - origin;
                if (to.sqrMagnitude > 1e-6f)
                {
                    float ang = Vector2.SignedAngle(fwd, to.normalized);
                    if (Mathf.Abs(ang) > half) continue;
                }
            }

            if (c.TryGetComponent(out ActInterfaces.IVulnerable v))
            {
                v.TakeDamage(p.attack * p.attackPercent, p.apRatio);
                ctx.EmitHook(AbilityHook.OnHit, c.transform, c.bounds.ClosestPoint(origin), nameof(MeleeInstantMechanic));
            }

            if (c.attachedRigidbody)
            {
                var dir = ((Vector2)c.bounds.center - origin).normalized;
                c.attachedRigidbody.AddForce(dir * p.knockback, ForceMode2D.Impulse);
            }
        }

        if (p.recover > 0f) yield return new WaitForSeconds(p.recover);

        ctx.EmitHook(AbilityHook.OnCastEnd, null, origin, nameof(MeleeInstantMechanic));
    }

    static Vector2 GetAimDir(Camera cam, Vector2 from)
    {
        if (!cam) return Vector2.right;
        var m = cam.ScreenToWorldPoint(Input.mousePosition);
        m.z = 0f;
        Vector2 dir = ((Vector2)m - from);
        return dir.sqrMagnitude < 1e-6f ? Vector2.right : dir.normalized;
    }
}
