using SOInterfaces;
using UnityEngine;

[System.Serializable]
public class MeleeParams : ISkillParam, IHasCooldown
{
    [Header("Area")]
    public float radius = 1.6f;
    [Range(0, 360)] public float angleDeg = 120f;  // 360이면 원형
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
    public float speed = 10f;              // 초기 속도
    public float acceleration = 0f;        // 매초 가속(선택)
    public float maxTurnDegPerSec = 360f;  // 회전 한계(°/s) → 낮출수록 느리게 굽힘
    public float radius = 0.2f;
    public float maxRange = 12f;
    public float maxLife = 3f;             // 안전 장치(초)

    [Header("Collision")]
    public LayerMask enemyMask;
    public LayerMask blockerMask;

    [Header("Damage")]
    public float damage = 8f, apRatio = 0f, knockback = 5f;

    [Header("Timing")]
    public float cooldown = 0.35f;
    public float Cooldown => cooldown;

    [Header("Behavior")]
    public bool retargetOnLost = true;     // 타깃 잃으면 재탐색 시도
    public float retargetRadius = 3f;      // 주변 재탐색 반경
}