using SkillInterfaces;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SOInterfaces;
using static EventBus;
using static GameEventMetaFactory;

[CreateAssetMenu(menuName = "Mechanics/Dash")]
public class DashMechanic : SkillMechanicBase<DashParams>, ITargetedMechanic
{
    const float SKIN = 0.01f; // 충돌면을 살짝 넘기는 여유
    public override IEnumerator Cast(Transform owner, Camera cam, DashParams param)
    {
        Debug.Log("NO\nDash has been casted without target");
        throw new System.NotImplementedException();
    }
    public IEnumerator Cast(Transform owner, Camera cam, ISkillParam param, Transform target)
    {
        return Cast(owner, cam, (DashParams)param, target);
    }
    IEnumerator Cast(Transform owner, Camera cam, DashParams p, Transform target)
    {
        if (!target) yield break;
        var motor = owner.GetComponent<KinematicMotor2D>();
        if (!motor) yield break;

        // (선택) 무적
        if (p.grantIFrame && p.iFrameDuration > 0f)
        {
            //Publish(new EffectApplyReq(Create(owner, "combat"), owner, new IFrameEffect(), p.iFrameDuration));
        }
        var rb = owner.GetComponent<Rigidbody2D>();
        Vector2 startPos = owner.position;

        // === 거리 예산 고정 ===
        Vector2 start = owner.position;
        Vector2 toTarget0 = (Vector2)target.position - start;
        Vector2 dir0 = toTarget0.sqrMagnitude > 1e-4f ? toTarget0.normalized : (Vector2)owner.right;
        float directDist = toTarget0.magnitude;
        float desiredDist = p.FallbackRange > 0 ? Mathf.Min(directDist, p.FallbackRange) : directDist;
        float remaining = desiredDist;

        // === 정책 스코프: 벽은 항상 차단, 적 차단 여부는 CanPenetrate에 따름, 슬라이드 허용 ===
        var basePolicy = motor.CurrentPolicy;
        var dashPolicy = basePolicy;
        dashPolicy.wallsMask = p.WallsMask;
        dashPolicy.enemyMask = p.enemyMask;
        dashPolicy.enemyAsBlocker = !p.CanPenetrate;     // 비관통 대시만 적을 차단
        dashPolicy.radius = p.radius;
        dashPolicy.skin = Mathf.Max(0.01f, p.skin);
        dashPolicy.allowWallSlide = false;//p.allowSlideOnWalls; // ★DashParams에 bool allowSlideOnWalls 추가 권장(없다면 true로 가정)

        while (elapsed < totalTime)
        {
            // 시작 겹침 해소(임의 +X 금지 → 명확한 대시 방향 사용)
            motor.BeginFrameDepenetrate(dir0);

            var hitIds = new HashSet<int>();
            float elapsed = 0f;
            float total = Mathf.Max(0.01f, p.duration);

            // 속도(거리/시간) * 커브
            float speed = (p.FallbackRange > 0f ? p.FallbackRange : toTarget.magnitude) / totalTime;
            float step = speed * p.speedCurve.Evaluate(Mathf.Clamp01(elapsed / totalTime)) * dt;

            float remaining = step;
            while (remaining > 0f)
            {
                // === 속도 곡선은 '예산 분할'에만 쓰고, 벽 근접 감속은 없음 ===
                float tNorm = Mathf.Clamp01(elapsed / total);
                float nominalSpeed = (desiredDist / total) * p.speedCurve.Evaluate(tNorm);
                float stepDist = Mathf.Min(remaining, nominalSpeed * Time.deltaTime);

                // 타깃형: 종착점은 고정, 중간에 벽으로 절단/슬라이드 가능
                Vector2 pos = owner.position;
                Vector2 toTarget = ((Vector2)target.position - pos);
                Vector2 dir = toTarget.sqrMagnitude > 1e-4f ? toTarget.normalized : dir0;

                // === 단일 스윕(모터 내부에서 필요 시 슬라이드 처리) ===
                var res = motor.SweepMove(dir * stepDist);
                remaining -= res.actualDelta.magnitude;

                // === 히트 처리(관통 여부와 무관) ===
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

                // === 종료 조건 ===
                if (res.hitWall) break;                         // 벽으로 절단(슬라이드 후에도 더 못가면 종료)
                if (elapsed >= total) break;                         // 시간 만료
                if (target.TryGetComponent<Collider2D>(out var _))
                {
                    float arrive = p.radius + p.skin;
                    if (((Vector2)target.position - (Vector2)owner.position).sqrMagnitude <= arrive * arrive)
                        break;                                           // 타깃에 충분히 근접
                }

                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        owner.GetComponent<SkillRunner>()?.NotifyHookOnExpire((Vector2)owner.position);
        yield break;
    }

    private static void Move(Transform owner, Rigidbody2D rb, Vector2 dir, float d)
    {
        if (d <= 0f) return;
        Vector3 delta = (Vector3)(dir * d);
        if (rb) rb.MovePosition((Vector2)owner.position + (Vector2)delta);
        else owner.position += delta;
    }
}
