using StatsInterfaces;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using Generals;

public interface  IPlayerStats : IDefensiveStats, IOffensiveStats, ICasterStats, IMoverStats, IEffectStats, IStatProvider // PlayerStats���� ����� �ϳ���, �ʹ� ������ ���� �����
{}
public sealed class PlayerStats : MonoBehaviour, IPlayerStats // �÷��̾� ���� ����, �ٸ� �������� ������
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
        if(stat == ReduceType.Mana) // 1(Mana)�̸� ����, 0(Health)�̸� ü��
        {
            Mana = Mathf.Max(0f, Mana - amount);
            Health = Mathf.Max(0f, Health - amount);
            if (Health <= 0f) IsDead = true;
        }
        else if(isfixed) //���� ����
        {
            Health = Mathf.Max(0f, Health - amount);
        }
        else //�Ϲ� ����
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
    //Effect �׸�
    public float EffectResistance { get; private set; } = 0f; // ���� �̻� ���� (0% �⺻)
    public Dictionary<Effects, float> EffectList { get; private set; } = new();
    public HashSet<Effects> PositiveEffects { get; private set; } = new() { Effects.Haste, Effects.DamageBoost, Effects.ReduceDamage, Effects.GainHealth, Effects.GainMana };
    public HashSet<Effects> NegativeEffects { get; private set; } = new() { Effects.Slow, Effects.Stun, Effects.Silence, Effects.Root, Effects.Tumbled, Effects.Damage };
    public HashSet<Effects> DisturbEffects { get; private set; } = new() { Effects.Slow, Effects.Stun, Effects.Silence, Effects.Root, Effects.Tumbled }; //���� ȿ��, Damage�� ������ ���� ȿ���̹Ƿ� EffectResistance�� ������ ���� ����
    public bool HasEffect(Effects e) // CC Ȯ�ο� �ʿ�
    {
        return EffectList.ContainsKey(e);
    }

    public void AddEffect(Effects buffType, float duration, float Amplifier = 0) //Effect�� Stats�� ������ �ֹǷ� ���⼭ �����ϰ� �ͱ�� �ѵ�, Ȯ�强�� �����ϸ� Effects�� ���� �δ� �� ��������
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
        Affection(buffType, duration, Amplifier); //FixedUpdate()�� ȣ���� �ߺ��Ǿ� 2�� ����Ǵ� �Ϳ� ����, ���� ����Ʈ ������ �� �ڵ鸵 �ʿ�
    }
    public void Affection(Effects buffType, float duration, float Amplifier = 0) // Amplifier�� ȿ���� ����, ���� ��� Haste/Slow�� �̵��ӵ� ������, Damage�� �ʴ� ���ط� -> �����, Damage�� Amplifier�� ��밡 ���ϴ� ���� ���ط��� �����Ƿ� ���⼭ DamageResistance�� ����ؾ� ��
    {
        Debug.Log($"Effect {buffType.GetType()} has affected for {duration} with amp {Amplifier}"); //Effect ������ ��, DisturbEffects�� EffectResistance ������ ��
    }
    public IEnumerator CoEffectDuration(Effects e, float duration) //Effect ���ӽð� ����
    {
        while (duration > 0f)
        {
            duration -= Time.deltaTime;
            EffectList[e] = duration;
            yield return null;
        }
        EffectList.Remove(e);
    }
    public void RemoveEffect(Effects buffType) //Cleanse�� ȣ���ϴ� �޼���, ������ ȿ���� ������ �� �ִ����� ������ ���� �� ��
    {
        if (EffectList.ContainsKey(buffType))
        {
            EffectList.Remove(buffType);
        }
    }
    public void ClearNegative() // ���� ȿ�� ��ü ����: ���� ���� ȿ���� �������� �ʴ� ���谡 ����(���� ���� ĳ���Ͱ� �׾��), Cleanse�� ȣ���ϴ� �޼���
                                // ���� ȿ���� ���Ǹ� "����/������ ������ ����� �ൿ�� �� ���� ����� ȿ��" �� �����ϸ� ��, ���� ���ش� �޵� ��¼�� �ൿ�� �Ǵϱ�
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