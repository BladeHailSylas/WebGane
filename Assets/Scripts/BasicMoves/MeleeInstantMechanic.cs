using SOInterfaces;
using System.Collections;
using UnityEngine;

[System.Serializable]
public class MeleeParams : ISkillParam, IHasCooldown
{
    [Header("Area")]
    public float radius = 1.6f;
    [Range(0, 360)] public float angleDeg = 120f;  // 360이면 원형
    public LayerMask enemyMask;

    [Header("Damage")]
    public float attack = 10f, apRatio = 0f, knockback = 6f, attackPercent = 1.0f;

    [Header("Timing")]
    public float windup = 0.05f, recover = 0.08f, cooldown = 0.10f;
    public float Cooldown => cooldown;
}

[CreateAssetMenu(menuName = "Mechanics/Melee Instant")]
public class MeleeInstantMechanic : SkillMechanicBase<MeleeParams>
{
    public override IEnumerator Cast(Transform owner, Camera cam, MeleeParams p)
    {
        if (p.windup > 0f) yield return new WaitForSeconds(p.windup);

        Vector2 origin = owner.position;
        Vector2 fwd = GetMouseDir(cam, origin);
        float half = p.angleDeg * 0.5f;

        var hits = Physics2D.OverlapCircleAll(origin, p.radius, p.enemyMask);
        foreach (var c in hits)
        {
            // 각도 필터(360이면 스킵)
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
                v.TakeDamage(p.attack * p.attackPercent, p.apRatio);

            if (c.attachedRigidbody)
            {
                var dir = ((Vector2)c.bounds.center - origin).normalized;
                c.attachedRigidbody.AddForce(dir * p.knockback, ForceMode2D.Impulse);
            }
        }

        if (p.recover > 0f) yield return new WaitForSeconds(p.recover);
    }

    static Vector2 GetMouseDir(Camera cam, Vector2 from)
    {
        if (!cam) return Vector2.right;
        var m = cam.ScreenToWorldPoint(Input.mousePosition);
        m.z = 0f; return ((Vector2)m - from).normalized;
    }
}