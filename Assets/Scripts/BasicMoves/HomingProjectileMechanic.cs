using SOInterfaces;
using UnityEngine;
using System.Collections;

[CreateAssetMenu(menuName = "Mechanics/Projectile (Homing, Targeted)")]
public class HomingProjectileMechanic : SkillMechanicBase<HomingParams>, ITargetedMechanic
{
    // 타깃형 진입점
    public IEnumerator Cast(Transform owner, Camera cam, ISkillParam p, Transform target)
    {
        Debug.Log("Hello again");
        return Cast(owner, cam, (HomingParams)p, target);
    }

    // 일반 진입점은 사용 안 함(필요하면 커서 방향 기본 발사로 대체 가능)
    public override IEnumerator Cast(Transform owner, Camera cam, HomingParams p)
    {
        Debug.Log("Homing Missile Casted Without any target");
        yield break; 
    }

    // 실제 로직
    IEnumerator Cast(Transform owner, Camera cam, HomingParams p, Transform target)
    {
        Debug.Log("Homing Missile Casted with target");
        if (target == null) yield break;

        var go = new GameObject("HomingProjectile");
        go.transform.position = owner.position;
        var mover = go.AddComponent<HomingProjectileMovement>();
        mover.Init(p, owner, target);
        yield return null;
    }
}