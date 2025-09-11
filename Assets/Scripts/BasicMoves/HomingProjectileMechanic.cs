using SOInterfaces;
using UnityEngine;
using System.Collections;

[CreateAssetMenu(menuName = "Mechanics/Projectile (Homing, Targeted)")]
public class HomingProjectileMechanic : SkillMechanicBase<HomingParams>, ITargetedMechanic
{
    // Ÿ���� ������
    public IEnumerator Cast(Transform owner, Camera cam, ISkillParam p, Transform target)
    {
        Debug.Log("Hello again");
        return Cast(owner, cam, (HomingParams)p, target);
    }

    // �Ϲ� �������� ��� �� ��(�ʿ��ϸ� Ŀ�� ���� �⺻ �߻�� ��ü ����)
    public override IEnumerator Cast(Transform owner, Camera cam, HomingParams p)
    {
        Debug.Log("Homing Missile Casted Without any target");
        yield break; 
    }

    // ���� ����
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