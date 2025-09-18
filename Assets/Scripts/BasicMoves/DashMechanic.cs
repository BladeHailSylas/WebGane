using SkillInterfaces;
//using ActInterfaces;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static EventBus;
using static GameEventMetaFactory;

[CreateAssetMenu(menuName = "Mechanics/Dash")]
public class DashMechanic : SkillMechanicBase<DashParams>, ITargetedMechanic
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

        // [RULE: IFrame] 필요 시 무적 상태 요청(효과 시스템에 위임)
        if (p.grantIFrame && p.iFrameDuration > 0f)
        {
            // Publish(new EffectApplyReq(Create(owner,"combat"), owner, new IFrameEffect(), p.iFrameDuration));
        }

        // [RULE: DistanceBudget] 종착점/거리 예산 고정(결정성)
        Vector2 start = owner.position;
        Vector2 toTarget0 = (Vector2)target.position - start;
        Vector2 dir0 = toTarget0.sqrMagnitude > 1e-4f ? toTarget0.normalized : (Vector2)owner.right;
        float directDist = toTarget0.magnitude;
        float desiredDist = p.FallbackRange > 0 ? Mathf.Min(directDist, p.FallbackRange) : directDist;
        float remaining = desiredDist;

        // [RULE: PolicyScope] 관통 정책: 적을 '벽'으로 볼지 여부
        var basePolicy = motor.CurrentPolicy;
        var dashPolicy = basePolicy;
        dashPolicy.wallsMask = p.WallsMask;
        dashPolicy.enemyMask = p.enemyMask;
        dashPolicy.enemyAsBlocker = !p.CanPenetrate; // 관통이면 적을 차단하지 않음
        dashPolicy.radius = p.radius;
        dashPolicy.skin = Mathf.Max(0.01f, p.skin);

        using (motor.With(dashPolicy))
        {
            var hitIds = new HashSet<int>(); // [RULE: HitSet] 중복 히트 방지

            // [RULE: Depenetrate] 시작 겹침 탈출
            motor.BeginFrameDepenetrate(dir0);

            float elapsed = 0f;
            float total = Mathf.Max(0.01f, p.duration);

            while (remaining > 0f)
            {
                float tNorm = Mathf.Clamp01(elapsed / total);
                float speed = (desiredDist / total) * p.speedCurve.Evaluate(tNorm);
                float stepDist = Mathf.Min(remaining, speed * Time.deltaTime);

                // 현재 방향(타깃형은 종착점 고정, 비타깃 앵커도 고정)
                Vector2 pos = owner.position;
                Vector2 dir = ((Vector2)target.position - pos).sqrMagnitude > 1e-4f ? ((Vector2)target.position - pos).normalized : dir0;

                // [RULE: SweepClamp] Motor로 단일 이동
                var res = motor.SweepMove(dir * stepDist);
                remaining -= res.actualDelta.magnitude;

                // [RULE: DealDamage] 경로 중 히트(관통 여부와 무관하게 '히트는' 발생)
                if (p.dealDamage && p.enemyMask.value != 0)
                {
                    // 작은 원으로 주변 타격(원한다면 Box/Capsule로 확장 가능)
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
                            // 필요 시 OnHit 훅/이벤트
                            Publish(new DamageDealt(Create(owner, "combat"), owner, c.transform,
                                    p.damage, p.damage, 0f, EDamageType.Normal, owner.position, dir));
                        }
                    }
                }

                // [RULE: StopOnWallOrTarget] 벽에 막히면 종료 / 타깃과 충분히 가까우면 종료
                if (res.hitWall) break;

                // 타깃형: 타깃 그 자체에 닿았다고 판단되면 종료
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
        }

        // [RULE: FollowUpHook] 종료 훅 → Runner가 FollowUp을 '대기 후 실행' 정책으로 스케줄
        owner.GetComponent<SkillRunner>()?.NotifyHookOnExpire((Vector2)owner.position);
    }
}