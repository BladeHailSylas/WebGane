using StatsInterfaces;
using UnityEngine;
using EffectInterfaces;
using System.Collections.Generic;

public interface  IPlayerStats : IDefensiveStats, IResistiveStats, IOffensiveStats, ICasterStats, IMoverStats, IStatProvider // PlayerStats���� ����� �ϳ���, �ʹ� ������ ���� �����
{}
public sealed class PlayerStats : MonoBehaviour, IPlayerStats // �÷��̾� ���� ����, �ٸ� �������� ������
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
    public float BaseDamageReduction { get; private set; }
    public float DamageReduction { get; private set; }
    public float BaseAttackDamage { get; private set; } = 10f;
    public float AttackDamage { get; private set; } = 12f;
    public List<float> ArmorPenetration { get; private set; } = new();
    public float BaseMana { get; private set; }
    public float MaxMana { get; private set; }
    public float Mana { get; private set; }
    public float BaseManaRegen { get; private set; }
    public float ManaRegen { get; private set; }
    public float BaseVelocity { get; private set; } = 8f;
    public float Velocity { get; private set; } = 8f;
    public float JumpTime { get; private set; }
    public bool OnGround { get; private set; }
    public bool IsDead { get; private set; }


    public float GetBool(StatBool sb)
    {
        return sb switch
        {
            StatBool.OnGround => OnGround ? 1f : 0f,
            StatBool.IsDead => IsDead ? 1f : 0f,
            _ => 0f,
        };
    }
    public float GetStat(StatType t, StatRef re = StatRef.Current)
    {
        return re switch
        {
            StatRef.Base => t switch
            {
                StatType.Health => BaseHealth,
                StatType.HealthRegen => BaseHealthRegen,
                StatType.Armor => BaseArmor,
                StatType.DamageReduction => BaseDamageReduction,
                StatType.AttackDamage => BaseAttackDamage,
                StatType.Mana => BaseMana,
                StatType.ManaRegen => BaseManaRegen,
                StatType.Velocity => BaseVelocity,
                StatType.JumpTime => JumpTime,
                _ => 0f,
            },
            StatRef.Max => t switch
            {
                StatType.Health => MaxHealth,
                StatType.Mana => MaxMana,
                _ => 0f,
            },
            StatRef.Current => t switch
            {
                StatType.Health => Health,
                StatType.HealthRegen => HealthRegen,
                StatType.Armor => Armor,
                StatType.DamageReduction => DamageReduction,
                StatType.AttackDamage => AttackDamage,
                StatType.Mana => Mana,
                StatType.ManaRegen => ManaRegen,
                StatType.Velocity => Velocity,
                _ => 0f,
            },
            _ => 0f,
        };
    }
    public float GetArmorRatio() //�÷��̾ ���ظ� ���ϴ� ��쿡�� ����
    {
        float TotalAR = 1f;
        foreach (float ap in ArmorPenetration)
        {
            TotalAR *= 1 - ap / 100;
        }
        return TotalAR;
    }
    public void ReduceStat(ReduceType stat, float damage, float armorRatio = 1f, bool isfixed = false)
    {
        if(stat == ReduceType.Mana) // 1(Mana)�̸� ����, 0(Health)�̸� ü��
        {
            Mana = Mathf.Max(0f, Mana - damage);
        }
        else
        {
            Damaged(damage, armorRatio, isfixed);
        }
        if (Health <= 0f) IsDead = true;
    }
    float DamageReductionCalc(float armor, float armorRatio = 1f, float damageReduction = 0f) //Player�� ���ظ� �޴� ���
    {
        return (80 / (80 + armor * armorRatio)) * (1 + damageReduction);
    }
    void Damaged(float damage, float armorRatio = 1f, bool isfixed = false)
    {
        if(IsDead || damage <= 0f) return;
        if(!isfixed) damage *= DamageReductionCalc(Armor, armorRatio, DamageReduction);
        if(SpecialShield > damage)
        {
            SpecialShield -= damage;
        }
        else if(SpecialShield + Shield > damage)
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
    public void StatSwitch(StatBool sb)
    {
        switch (sb)
        {
            case StatBool.OnGround:
                OnGround = !OnGround;
                break;
            case StatBool.IsDead:
                IsDead = !IsDead;
                break;
            default:
                break;
        }
    }
}