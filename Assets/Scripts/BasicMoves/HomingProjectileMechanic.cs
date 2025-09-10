using SOInterfaces;
using ActInterfaces;
using UnityEngine;

[CreateAssetMenu(menuName = "Mechanics/Projectile (Homing)")]
public class HomingProjectileMechanic : SkillMechanicBase<HomingProjectileParams>
{
    public override System.Collections.IEnumerator Cast(Transform owner, Camera cam, HomingProjectileParams p)
    {
        // Ÿ�� Ȯ��
        Transform target = null;
        var provider = owner.GetComponent<ITargetable>();
        provider?.TryGetTarget(out target);
        if (target == null) yield break; // Ÿ���� ������ �߻� �� ��(��å�� ���� �⺻ ����ü�� ��ü ����)

        // ����ü ����
        var go = new GameObject("HomingProjectile");
        go.transform.position = owner.position;
        var mover = go.AddComponent<HomingProjectileMover>();
        mover.Init(p, owner, target);
        yield return null;
    }
}