using SOInterfaces;
using ActInterfaces;
using UnityEngine;

[CreateAssetMenu(menuName = "Mechanics/Projectile (Homing)")]
public class HomingProjectileMechanic : SkillMechanicBase<HomingProjectileParams>
{
    public override System.Collections.IEnumerator Cast(Transform owner, Camera cam, HomingProjectileParams p)
    {
        // 타깃 확보
        Transform target = null;
        var provider = owner.GetComponent<ITargetable>();
        provider?.TryGetTarget(out target);
        if (target == null) yield break; // 타깃이 없으면 발사 안 함(정책에 따라 기본 투사체로 대체 가능)

        // 투사체 생성
        var go = new GameObject("HomingProjectile");
        go.transform.position = owner.position;
        var mover = go.AddComponent<HomingProjectileMover>();
        mover.Init(p, owner, target);
        yield return null;
    }
}