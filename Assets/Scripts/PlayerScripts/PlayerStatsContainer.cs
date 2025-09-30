using StatsInterfaces;
using System.Collections.Generic;
using UnityEngine;
public sealed class PlayerStatsContainer : MonoBehaviour // 플레이어 스탯 관리, 다른 곳에서는 참조만
{
    [SerializeField] PlayerEffects effects;
    public float BaseHealth { get; private set; }
    public float MaxHealth { get; private set; }
    public float Health { get; private set; }
    public float Shield { get; private set; }
    public float SpecialShield { get; private set; }
    public float BaseArmor { get; private set; }
    public float Armor { get; private set; }
    public float BaseHealthRegen { get; private set; }
    public float HealthRegen { get; private set; }
    public List<float> DamageReduction { get; private set; }
    public float BaseAttackDamage { get; private set; } = 10f;
    public float AttackDamage { get; private set; } = 12f;
    public List<float> ArmorPenetration { get; private set; } = new();
    public float BaseMana { get; private set; }
    public float MaxMana { get; private set; }
    public float Mana { get; private set; }
    public float BaseManaRegen { get; private set; }
    public float ManaRegen { get; private set; }
    public float BaseSpeed { get; private set; } = 8f;
    public float Speed { get; private set; } = 8f;
    public float JumpTime { get; private set; }
    public bool OnGround { get; private set; }
    public bool IsDead { get; private set; }
    public void ReduceStat(ReduceType stat, float damage, float armorRatio = 1f, bool isfixed = false)
    {
        if (stat == ReduceType.Mana) // 1(Mana)이면 마나, 0(Health)이면 체력
        {
            Mana = Mathf.Max(0f, Mana - damage);
        }
        else
        {
            Damaged(damage, armorRatio, isfixed);
        }
        if (Health <= 0f) IsDead = true;
    }
    float DamageReductionCalc(float armor, float armorRatio = 1f, float damageRatio = 1f) //Player가 피해를 받는 경우
    {
        return (80 / (80 + armor * armorRatio)) * damageRatio;
    }
    void Damaged(float damage, float armorRatio = 1f, bool isfixed = false) //비례 피해를 여기서 계산해야 되나 << 그럴 거 같지 않음
    {
        if (IsDead || damage <= 0f) return;
        if (!isfixed) damage *= DamageReductionCalc(Armor, armorRatio, TotalDamageReduction());
        if (SpecialShield > damage)
        {
            SpecialShield -= damage;
        }
        else if (SpecialShield + Shield > damage)
        {
            Shield -= SpecialShield - damage;
        }
        else
        {
            Health -= Shield + SpecialShield - damage;
        }
        if (Health <= 0f)
        {
            IsDead = true;
        }
    }
    public float TotalArmorPenetration() //필드를 건드는 메서드가 아니므로 public으로 두어도 되지 않을까
    {
        float totalAP = 1f;
        foreach (var ap in ArmorPenetration)
        {
            totalAP *= (1 - ap / 100); // 단일 AP 비율이 100%를 넘으면 방어력이 마이너스라 피해 배율이 너무 커지므로 그런 일이 없어야 함
        }
        return totalAP;
    }
    public float TotalDamageReduction()
    {
        float totalDR = 1f;
        foreach (var dr in DamageReduction)
        {
            totalDR *= (1 - dr / 100); //단일 DR 비율이 100%를 넘으면 맞는데 회복함, AP도 100%를 넘으면 안 되지만 DR은 더더욱 단일 비율이 100%을 넘어서는 안 됨(망겜임)
        }
        return Mathf.Max(0.15f, totalDR); //공격자 우선(하게 두되 대안을 주어라) -> 대미지가 들어가게 두되 다른 생존 수단(체력 회복, 보호막 등)으로 원콤이 안 나게 하라
    }
}