using StatsInterfaces;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;

public interface  IPlayerStats : IDefensiveStats, IOffensiveStats, ICasterStats, IMovingStats, IEffectStats, IStatProvider
{}
public sealed class PlayerStats : MonoBehaviour, IPlayerStats // 플레이어 스탯 관리, Empty에 붙이기?
{
    public float BaseHealth { get; private set; }
    public float MaxHealth { get; private set; }
    public float Health { get; private set; }
    public float BaseArmor { get; private set; }
    public float Armor { get; private set; }
    public float BaseHealthRegen { get; private set; }
    public float HealthRegen { get; private set; }
    public float BaseDamageReduction { get; private set; }
    public float DamageReduction { get; private set; }
    public float BaseAttackDamage { get; private set; }
    public float AttackDamage { get; private set; }
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
    public bool IsImmune { get; private set; }

    public float EffectResistance { get; private set; } = 0f; // 상태 이상 저항 (0% 기본)
    public Dictionary<Effects, float> EffectList { get; private set; } = new();

    public float GetBool(StatBool sb)
    {
        return sb switch
        {
            StatBool.OnGround => OnGround ? 1f : 0f,
            StatBool.IsDead => IsDead ? 1f : 0f,
            StatBool.IsImmune => IsImmune ? 1f : 0f,
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
    public void ReduceStat(bool stat, float amount, float apratio = 0, bool isfixed = false)
    {
        if(!stat && !isfixed) // true면 마나, false면 체력
        {
            Mana = Mathf.Max(0f, Mana - amount);
            Health = Mathf.Max(0f, Health - amount);
            if (Health <= 0f) IsDead = true;
        }
        else if(isfixed)
        {
            Health = Mathf.Max(0f, Health - amount);
        }
        else
        {
            Health = Mathf.Max(0f, Health - amount * (80 / (80 + Armor * (1 - apratio))) * (DamageReduction + 1));
        }
        if (Health <= 0f) IsDead = true;
    }
    public bool HasEffect(Effects e)
    {
        return EffectList.ContainsKey(e);
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
            case StatBool.IsImmune:
                IsImmune = !IsImmune;
                break;
            default:
                break;
        }
    }
    public void AddEffect(Effects buffType, float duration, float Amplifier = 0)
    {
        if (EffectList.ContainsKey(buffType))
        {
            EffectList[buffType] = Mathf.Max(EffectList[buffType], duration);
        }
        else
        {
            EffectList.Add(buffType, duration);
            StartCoroutine(CoEffectDuration(buffType, duration));
        }
    }
    public IEnumerator CoEffectDuration(Effects e, float duration)
    {
        while (duration > 0f)
        {
            duration -= Time.deltaTime * (1 - EffectResistance);
            EffectList[e] = duration;
            yield return null;
        }
        EffectList.Remove(e);
    }
    public void RemoveEffect(ActInterfaces.Effects buffType)
    {
        Effects e = (Effects)buffType;
        if (EffectList.ContainsKey(e))
        {
            EffectList.Remove(e);
        }
    }
}