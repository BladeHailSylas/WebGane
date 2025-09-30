using SkillInterfaces;
using UnityEngine;
using System.Collections;

[CreateAssetMenu(menuName = "Mechanics/Projectile (Homing, Targeted)")]
public class ProjectileMechanics : SkillMechanismBase<MissileParams>, ITargetedMechanic
{
    protected override IEnumerator Execute(MechanismContext ctx, MissileParams p)
    {
        var owner = ctx.Owner;
        if (!owner)
        {
            Debug.LogWarning("[ProjectileMechanics] Owner transform가 존재하지 않습니다.");
            yield break;
        }

        if (!ctx.HasTarget)
        {
            Debug.LogWarning("[ProjectileMechanics] Target이 없어 기본 발사를 수행하지 않습니다.");
            yield break;
        }

        var go = new GameObject("HomingProjectile");
        go.transform.position = owner.position;
        var mover = go.AddComponent<ProjectileMovement>();
        mover.Init(p, ctx);

        // 1프레임 동안 생성된 투사체의 초기화가 완료되도록 대기합니다.
        yield return null;
    }

    public IEnumerator Cast(MechanismContext ctx, ISkillParam param, Transform target)
    {
        if (param is not MissileParams missile)
            throw new System.InvalidOperationException($"Param type mismatch. Need {nameof(MissileParams)}");
        return Execute(ctx.WithTarget(target), missile);
    }
}
