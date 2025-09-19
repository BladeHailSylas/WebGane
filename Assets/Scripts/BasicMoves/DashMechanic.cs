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
        // (선택) 대시 시작 시 I-Frame 요청 — 효과/스탯 시스템이 처리
        if (p.grantIFrame && p.iFrameDuration > 0f)
        {
            //Publish(new EffectApplyReq(Create(owner, "combat"), owner, new IFrameEffect(), p.iFrameDuration));
        }
        var rb = owner.GetComponent<Rigidbody2D>();
        Vector2 startPos = owner.position;

        // 대시 총거리: Runner가 TargetMode.FixedForward인 경우 보통 fallbackRange를 목적지까지로 쓰지만,
        // 메커닉은 '타깃 Transform'만 받고 움직입니다. 목적지까지 직선거리로 대략 보정.
        //target = ResolveTarget(owner); // Runner가 넘긴 target(실제 적 또는 앵커) -> 필요가 없음
        if (target == null) yield break;

        float totalTime = Mathf.Max(0.01f, p.duration);
        float elapsed = 0f;
        float traveled = 0f;

        // “타깃형: 타깃 소실 시 종료” 플래그
        bool targetIsEnemy = target.GetComponent<Collider2D>() != null;

        var hitIds = new HashSet<int>(); // 관통 중복 타격 방지

        while (elapsed < totalTime)
        {
            float dt = Time.deltaTime;
            elapsed += dt;

            // 현재 위치/목표 계산
            Vector2 pos = owner.position;
            Vector2 toTarget = (Vector2)target.position - pos;

            // 타깃 소실(타깃형) → 종료
            if (targetIsEnemy && target == null)
                break;

            // 속도(거리/시간) * 커브
            float speed = (p.FallbackRange > 0f ? p.FallbackRange : toTarget.magnitude) / totalTime;
            float step = speed * p.speedCurve.Evaluate(Mathf.Clamp01(elapsed / totalTime)) * dt;

            float remaining = step;
            while (remaining > 0f)
            {
                pos = owner.position;
                Vector2 dir = toTarget.sqrMagnitude > 0.0001f ? toTarget.normalized : (Vector2)owner.right;

                // 1) 벽 검사
                var wallHit = Physics2D.CircleCast(pos, p.radius, dir, remaining, p.WallsMask);
                if (wallHit.collider)
                {
                    // 벽까지 이동
                    Move(owner, rb, dir, wallHit.distance);
                    traveled += wallHit.distance;

                    // 벽이면 종료(옵션에 따라 슬라이드 등 확장 가능)
                    if (p.stopOnWall) goto DashEnd;
                    // stopOnWall=false면, 접선 방향으로 재계산 등 확장 가능(여기서는 간단 종료)
                    goto DashEnd;
                }

                // 2) 적 검사
                var enemyHit = Physics2D.CircleCast(pos, p.radius, dir, remaining, p.enemyMask);
                bool enemyProcessed = false;

                if (enemyHit.collider)
                {
                    var c = enemyHit.collider;
                    int id = c.GetInstanceID();
                    // 같은 콜라이더 중복 타격 방지
                    if (!hitIds.Contains(id))
                    {
                        // 충돌점까지 이동
                        Move(owner, rb, dir, enemyHit.distance);
                        traveled += enemyHit.distance;

                        // 피해/넉백 적용
                        if (p.dealDamage)
                        {
                            if (c.TryGetComponent(out ActInterfaces.IVulnerable v))
                                v.TakeDamage(p.damage, p.apRatio);
                            if (c.attachedRigidbody)
                                c.attachedRigidbody.AddForce(dir * p.knockback, ForceMode2D.Impulse);

                            // (선택) DamageDealt 이벤트 발행
                            Publish(new DamageDealt(Create(owner, "combat"),
                                owner, c.transform, p.damage, p.damage, 0f, EDamageType.Normal,
                                (Vector2)owner.position, dir));
                        }

                        hitIds.Add(id);
                        enemyProcessed = true;

                        // Targeted: 대상과 충돌 시 종료(관통 옵션 무시)
                        if (targetIsEnemy && c.transform == target)
                            goto DashEnd;

                        // 관통 불가면 종료
                        if (!p.canPenetrate)
                            goto DashEnd;

                        // 관통이면 SKIN만큼 더 나아가 재충돌 방지
                        Move(owner, rb, dir, SKIN);
                        traveled += SKIN;
                        remaining -= enemyHit.distance + SKIN;
                        continue; // 다음 충돌/이동 처리
                    }
                }

                // 3) 충돌이 없거나 이미 친 적이면 남은 거리 이동
                Move(owner, rb, (toTarget.sqrMagnitude > 0.0001f ? toTarget.normalized : (Vector2)owner.right), remaining);
                traveled += remaining;
                remaining = 0f;
            }

            // Non-targeted: 앵커(Transform) 도달 판단(충분히 가까우면 종료)
            if (!targetIsEnemy)
            {
                if (((Vector2)target.position - (Vector2)owner.position).sqrMagnitude <= (p.radius + p.skin) * (p.radius + p.skin))
                    break;
            }

            // Targeted: 타깃과 거의 겹치면 종료(거리 임계)
            else
            {
                if (((Vector2)target.position - (Vector2)owner.position).sqrMagnitude <= (p.radius + p.skin) * (p.radius + p.skin))
                    break;
            }

            // 최대 대시 거리(고정 거리) 소진 시 종료
            if (p.FallbackRange > 0f && (owner.position - (Vector3)startPos).sqrMagnitude >= p.FallbackRange * p.FallbackRange)
                break;

            yield return null;
        }

    DashEnd:
        // 종료 훅: Runner가 OnExpire 훅을 받아 FollowUp을 스케줄합니다.
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