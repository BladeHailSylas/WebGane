using SkillInterfaces;
using UnityEngine;
using System.Collections;

[CreateAssetMenu(menuName = "Mechanics/Projectile (Homing, Targeted)")]
public class ProjectileMechanics : SkillMechanismBase<MissileParams>, ITargetedMechanic
{
    // Ÿ���� ������
    public IEnumerator Cast(Transform owner, Camera cam, ISkillParam p, Transform target)
    {
        return Cast(owner, cam, (MissileParams)p, target);
    }

    // �Ϲ� �������� ��� �� ��(�ʿ��ϸ� Ŀ�� ���� �⺻ �߻�� ��ü ����)
    public override IEnumerator Cast(Transform owner, Camera cam, MissileParams p)
    {
        //Debug.Log("Homing Missile Casted Without any target"); //��ǻ� �� log�� �� ���;� ��. �̰� ������ SkillRunner�� �̻��ϴٴ� ����
        yield break; 
    }

    // ���� ����
    IEnumerator Cast(Transform owner, Camera cam, MissileParams p, Transform target) //Camera cam�� ��� ���� ��? ī�޶� ��ũ�� �ʿ�?
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