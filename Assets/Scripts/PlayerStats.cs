using StatsInterfaces;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using Generals;

public interface  IPlayerStats : IDefensiveStats, IOffensiveStats, ICasterStats, IMoverStats, IEffectStats, IStatProvider // PlayerStats에는 상속을 하나만, 너무 많으면 관리 어려움
{}
public sealed class PlayerStats : MonoBehaviour, IPlayerStats // 플레이어 스탯 관리, 다른 곳에서는 참조만
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
    public float BaseAttackDamage { get; private set; } = 10f;
    public float AttackDamage { get; private set; } = 12f;
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
    public void ReduceStat(ReduceType stat, float amount, float apratio = 0, bool isfixed = false)
    {
        if(stat == ReduceType.Mana) // 1(Mana)이면 마나, 0(Health)이면 체력
        {
            Mana = Mathf.Max(0f, Mana - amount);
            Health = Mathf.Max(0f, Health - amount);
            if (Health <= 0f) IsDead = true;
        }
        else if(isfixed) //고정 피해
        {
            Health = Mathf.Max(0f, Health - amount);
        }
        else //일반 피해
        {
            Health = Mathf.Max(0f, Health - amount * (80 / (80 + Armor * (1 - apratio))) * (DamageReduction + 1));
        }
        if (Health <= 0f) IsDead = true;
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
    //Effect 항목
    public float EffectResistance { get; private set; } = 0f; // 상태 이상 저항 (0% 기본)
    public Dictionary<Effects, float> EffectList { get; private set; } = new();
    public HashSet<Effects> PositiveEffects { get; private set; } = new() { Effects.Haste, Effects.DamageBoost, Effects.ReduceDamage, Effects.GainHealth, Effects.GainMana };
    public HashSet<Effects> NegativeEffects { get; private set; } = new() { Effects.Slow, Effects.Stun, Effects.Silence, Effects.Root, Effects.Tumbled, Effects.Damage };
    public HashSet<Effects> DisturbEffects { get; private set; } = new() { Effects.Slow, Effects.Stun, Effects.Silence, Effects.Root, Effects.Tumbled }; //방해 효과, Damage는 엄연한 공격 효과이므로 EffectResistance의 영향을 받지 않음
    public bool HasEffect(Effects e) // CC 확인에 필요
    {
        return EffectList.ContainsKey(e);
    }

    public void AddEffect(Effects buffType, float duration, float Amplifier = 0) //Effect는 Stats에 영향을 주므로 여기서 관리하고 싶기는 한데, 확장성을 생각하면 Effects를 따로 두는 게 나을지도
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
        Affection(buffType, duration, Amplifier); //FixedUpdate()와 호출이 중복되어 2번 적용되는 것에 유의, 추후 이펙트 구현할 때 핸들링 필요
    }
    public void Affection(Effects buffType, float duration, float Amplifier = 0) // Amplifier는 효과의 강도, 예를 들어 Haste/Slow면 이동속도 변동성, Damage면 초당 피해량 -> 참고로, Damage의 Amplifier는 상대가 가하는 원래 피해량을 받으므로 여기서 DamageResistance를 계산해야 함
    {
        Debug.Log($"Effect {buffType.GetType()} has affected for {duration} with amp {Amplifier}"); //Effect 적용할 때, DisturbEffects는 EffectResistance 적용할 것
    }
    public IEnumerator CoEffectDuration(Effects e, float duration) //Effect 지속시간 관리
    {
        while (duration > 0f)
        {
            duration -= Time.deltaTime;
            EffectList[e] = duration;
            yield return null;
        }
        EffectList.Remove(e);
    }
    public void RemoveEffect(Effects buffType) //Cleanse가 호출하는 메서드, 긍정적 효과를 제거할 수 있는지는 생각해 봐야 할 듯
    {
        if (EffectList.ContainsKey(buffType))
        {
            EffectList.Remove(buffType);
        }
    }
    public void ClearNegative() // 방해 효과 전체 제거: 지속 피해 효과는 제거하지 않는 설계가 좋음(지속 피해 캐릭터가 죽어요), Cleanse가 호출하는 메서드
                                // 방해 효과의 정의를 "종료/해제될 때까지 제대로 행동할 수 없게 만드는 효과" 로 정의하면 됨, 지속 피해는 받든 어쩌든 행동이 되니까
    {
        foreach (var effect in EffectList.Keys)
        {
            if (EffectList.ContainsKey(effect) && DisturbEffects.Contains(effect))
            {
                EffectList.Remove(effect);
            }
        }
    }
    /*void FixedUpdate()
    {
        Affection(Effects.Haste, 0f, 10f);
    }*/
}