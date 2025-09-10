using SOInterfaces;
using UnityEngine;

[System.Serializable]
public class MeleeParams : ISkillParam, IHasCooldown
{
    [Header("Area")]
    public float radius = 1.6f;
    [Range(0, 360)] public float angleDeg = 120f;  // 360�̸� ����
    public LayerMask enemyMask;

    [Header("Damage")]
    public float attack = 10f, apRatio = 0f, knockback = 6f, attackPercent = 1.0f;

    [Header("Timing")]
    public float windup = 0.05f, recover = 0.08f, cooldown = 0.10f;
    public float Cooldown => cooldown;
}

[System.Serializable]
public class HomingProjectileParams : ISkillParam, IHasCooldown
{
    [Header("Projectile")]
    public float speed = 10f;              // �ʱ� �ӵ�
    public float acceleration = 0f;        // ���� ����(����)
    public float maxTurnDegPerSec = 360f;  // ȸ�� �Ѱ�(��/s) �� ������� ������ ����
    public float radius = 0.2f;
    public float maxRange = 12f;
    public float maxLife = 3f;             // ���� ��ġ(��)

    [Header("Collision")]
    public LayerMask enemyMask;
    public LayerMask blockerMask;

    [Header("Damage")]
    public float damage = 8f, apRatio = 0f, knockback = 5f;

    [Header("Timing")]
    public float cooldown = 0.35f;
    public float Cooldown => cooldown;

    [Header("Behavior")]
    public bool retargetOnLost = true;     // Ÿ�� ������ ��Ž�� �õ�
    public float retargetRadius = 3f;      // �ֺ� ��Ž�� �ݰ�
}