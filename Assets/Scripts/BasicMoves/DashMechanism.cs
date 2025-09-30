using SkillInterfaces;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static EventBus;
using static GameEventMetaFactory;

[CreateAssetMenu(menuName = "Mechanics/Dash")]
public class DashMechanism : SkillMechanismBase<DashParams>, ITargetedMechanic
{
    public override IEnumerator Cast(Transform owner, Camera cam, DashParams p)
    { Debug.LogError("Dash requires a target Transform"); yield break; }

    public IEnumerator Cast(Transform owner, Camera cam, ISkillParam param, Transform target)
        => Cast(owner, cam, (DashParams)param, target);

    IEnumerator Cast(Transform owner, Camera cam, DashParams p, Transform target)
    {
        if (!target) yield break;

        var motor = owner.GetComponent<KinematicMotor2D>();
        if (!motor) yield break;

        // (선택) 무적
        if (p.grantIFrame && p.iFrameDuration > 0f)
        {
            // Publish(new EffectApplyReq(Create(owner,"combat"), owner, new IFrameEffect(), p.iFrameDuration));
        }

        // === 거리 예산 고정 ===
        Vector2 start = owner.position;
        Vector2 toTarget0 = (Vector2)target.position - start;
        Vector2 dir0 = toTarget0.sqrMagnitude > 1e-4f ? toTarget0.normalized : (Vector2)owner.right;
        float directDist = toTarget0.magnitude;
        float desiredDist = p.FallbackRange > 0 ? Mathf.Min(directDist, p.FallbackRange) : directDist;
        float remaining = desiredDist;

        // === 정책 스코프 ===
        var basePolicy = motor.CurrentPolicy;
        var dashPolicy = basePolicy;
        dashPolicy.wallsMask = p.WallsMask;
        dashPolicy.enemyMask = p.enemyMask;
        dashPolicy.enemyAsBlocker = !p.CanPenetrate; // 비관통만 적 차단
        dashPolicy.radius = p.radius;
        dashPolicy.skin = Mathf.Max(0.01f, p.skin);
        dashPolicy.allowWallSlide = true; // 센서 상태로 프레임별로 갱신 예정

        using (motor.With(dashPolicy))
        {
            var hitIds = new HashSet<int>();

            float elapsed = 0f;
            float total = Mathf.Max(0.01f, p.duration);

            while (remaining > 0f)
            {
                // 스코프 내에서 갱신 반영
                using (motor.With(dashPolicy))
                {
                    //if (sensor && s.intruding && s.mtvDir != Vector2.zero) motor.RemoveComponent();
                }

                // --- 예산 분배: 감속 금지, step만큼 시도 ---
                float tNorm = Mathf.Clamp01(elapsed / total);
                float nominalSpeed = (desiredDist / total) * p.speedCurve.Evaluate(tNorm);
                float stepDist = Mathf.Min(remaining, nominalSpeed * Time.deltaTime);

                // --- 목표 방향 ---
                Vector2 pos = owner.position;
                Vector2 aim = ((Vector2)target.position - pos);
                Vector2 dir = aim.sqrMagnitude > 1e-4f ? aim.normalized : dir0;

				// --- 단일 스윕(모터가 같은 프레임에 슬라이드 반복 처리) ---
				motor.Depenetration();
				var res = motor.SweepMove(dir * stepDist);
				motor.Depenetration();
				remaining -= res.actualDelta.magnitude;

                // --- 히트(관통 여부 무관) ---
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
                        }
                    }
                }

                // --- 종료 조건 ---
                if (res.hitWall) break; // 슬라이드 후에도 못 가면 종료

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

            // 관통 대시였다면, 종료 프레임에 겹침 청소 한 번 더(적 포함 상황 대비)
			motor.Depenetration();
        }

        owner.GetComponent<SkillRunner>()?.NotifyHook(AbilityHook.OnCastEnd, p);
    }
}
