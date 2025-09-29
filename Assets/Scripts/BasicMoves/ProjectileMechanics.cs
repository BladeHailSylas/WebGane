using SkillInterfaces;
using UnityEngine;
using System.Collections;

[CreateAssetMenu(menuName = "Mechanics/Projectile (Homing, Targeted)")]
public class ProjectileMechanics : SkillMechanismBase<MissileParams>, ITargetedMechanic
{
    // 타깃형 진입점
    public IEnumerator Cast(Transform owner, Camera cam, ISkillParam p, Transform target)
    {
        return Cast(owner, cam, (MissileParams)p, target);
    }

    // 일반 진입점은 사용 안 함(필요하면 커서 방향 기본 발사로 대체 가능)
    public override IEnumerator Cast(Transform owner, Camera cam, MissileParams p)
    {
        //Debug.Log("Homing Missile Casted Without any target"); //사실상 이 log는 안 나와야 함. 이게 나오면 SkillRunner가 이상하다는 증거
        yield break; 
    }

    // 실제 로직
    IEnumerator Cast(Transform owner, Camera cam, MissileParams p, Transform target) //Camera cam은 어디에 쓰는 것? 카메라 워크에 필요?
    {
        //Debug.Log($"Homing Missile Casted with target {target.name}");
        if (target == null) yield break;

        var go = new GameObject("HomingProjectile");
        go.transform.position = owner.position;
        var mover = go.AddComponent<ProjectileMovement>();
        mover.Init(p, owner, target);
        yield return null;
    }
}