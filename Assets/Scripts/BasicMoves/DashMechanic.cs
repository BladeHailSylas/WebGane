using SkillInterfaces;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static EventBus;
using static GameEventMetaFactory;

[CreateAssetMenu(menuName = "Mechanics/Dash")]
public class DashMechanic : SkillMechanismBase<DashParams>, ITargetedMechanic
{
    protected override IEnumerator Execute(MechanismContext ctx, DashParams p)
    {
        var owner = ctx.Owner;
        if (!owner)
        {
            Debug.LogWarning("[DashMechanic] Owner transform가 존재하지 않습니다.");
            yield break;
        }

        var target = ctx.Target;
        if (!target)
        {
            Debug.LogWarning("[DashMechanic] Target transform이 필요합니다.");
            yield break;
        }

        var motor = owner.GetComponent<KinematicMotor2D>();
        if (!motor) yield break;

        var sensor = owner.GetComponentInChildren<PlayerSensor2D>();

        if (p.grantIFrame && p.iFrameDuration > 0f)
        {
            // Publish(new EffectApplyReq(Create(owner, "combat"), owner, new IFrameEffect(), p.iFrameDuration));
        }

        Vector2 start = owner.position;
        Vector2 toTarget0 = (Vector2)target.position - start;
        Vector2 dir0 = toTarget0.sqrMagnitude > 1e-4f ? toTarget0.normalized : (Vector2)owner.right;
        float directDist = toTarget0.magnitude;
        float desiredDist = p.FallbackRange > 0 ? Mathf.Min(directDist, p.FallbackRange) : directDist;
        float remaining = desiredDist;

        var basePolicy = motor.CurrentPolicy;
        var dashPolicy = basePolicy;
        dashPolicy.wallsMask = p.WallsMask;
        dashPolicy.enemyMask = p.enemyMask;
        dashPolicy.enemyAsBlocker = !p.CanPenetrate;
        dashPolicy.radius = p.radius;
        dashPolicy.skin = Mathf.Max(0.01f, p.skin);
        dashPolicy.allowWallSlide = true;

        using (motor.With(dashPolicy))
        {
            var hitIds = new HashSet<int>();
            float elapsed = 0f;
            float total = Mathf.Max(0.01f, p.duration);

            while (remaining > 0f)
            {
                var s = sensor ? sensor.GetState() : default;
                if (sensor) dashPolicy.allowWallSlide = s.nearWall;
                using (motor.With(dashPolicy))
                {
                    // 필요 시 센서 기반 보정 로직을 삽입하십시오.
                }

                float tNorm = Mathf.Clamp01(elapsed / total);
                float nominalSpeed = (desiredDist / total) * p.speedCurve.Evaluate(tNorm);
                float stepDist = Mathf.Min(remaining, nominalSpeed * Time.deltaTime);

                Vector2 pos = owner.position;
                Vector2 aim = ((Vector2)target.position - pos);
                Vector2 dir = aim.sqrMagnitude > 1e-4f ? aim.normalized : dir0;

                motor.Depenetration();
                var res = motor.SweepMove(dir * stepDist);
                motor.Depenetration();
                remaining -= res.actualDelta.magnitude;

                if (p.dealDamage && p.enemyMask.value != 0)
                {
                    var hits = Physics2D.OverlapCircleAll(owner.position, p.radius, p.enemyMask);
                    foreach (var c in hits)
                    {
                        int id = c.GetInstanceID();
                        if (hitIds.Contains(id)) continue;
                        if (c.TryGetComponent(out ActInterfaces.IVulnerable v))
                        {
                            v.TakeDamage(p.damage, p.apRatio);
                            if (c.attachedRigidbody && p.knockback != 0f)
                            {
                                var kdir = (Vector2)(c.transform.position - owner.position).normalized;
                                c.attachedRigidbody.AddForce(kdir * p.knockback, ForceMode2D.Impulse);
                            }
                            hitIds.Add(id);
                            Publish(new DamageDealt(Create(owner, "combat"), owner, c.transform,
                                    p.damage, p.damage, 0f, EDamageType.Normal, owner.position, dir));
                            ctx.EmitHook(AbilityHook.OnHit, c.transform, c.bounds.ClosestPoint(owner.position), nameof(DashMechanic));
                        }
                    }
                }

                if (res.hitWall) break;

                if (target.GetComponent<Collider2D>() != null)
                {
                    float arrive = p.radius + p.skin;
                    if (((Vector2)target.position - (Vector2)owner.position).sqrMagnitude <= arrive * arrive)
                        break;
                }

                elapsed += Time.deltaTime;
                if (elapsed >= total) break;

                yield return null;
            }

            motor.Depenetration();
        }

        ctx.EmitHook(AbilityHook.OnCastEnd, target, owner.position, nameof(DashMechanic));
    }

    public IEnumerator Cast(MechanismContext ctx, ISkillParam param, Transform target)
    {
        if (param is not DashParams dash)
            throw new System.InvalidOperationException($"Param type mismatch. Need {nameof(DashParams)}");
        return Execute(ctx.WithTarget(target), dash);
    }
}
