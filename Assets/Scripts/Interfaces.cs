using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Generals;
using ActInterfaces;
using StatsInterfaces;

namespace Generals
{
    public enum Effects
    {
        None = 0, Haste, DamageBoost, ReduceDamage, GainHealth, GainMana, Slow, Stun, Silence, Root, Tumbled, Damage //Damage는 지속 피해, duration을 0으로 하면 즉시 피해도 가능함
    }
    public enum ReduceType
    {
        Health = 0, Mana
    }
    public interface IPlayerCharacter : IDefensiveStats, IResistiveStats, IOffensiveStats, ICasterStats, IMoverStats, 
        ITogglable, IAttackable
    {
        string DisplayName { get; }
        void Attack();
        void ShillShift();
        void SkillQ();
        void SkillE();
        void Ultimate();

    }
}

namespace ActInterfaces
{
    public interface IVulnerable //피해를 받아 죽을 수 있음
    {
        void TakeDamage(float damage, float apRatio = 0, bool isFixed = false);
        void Die();
    }

    public interface IExpirable // 살아있는 시간이 제한된 엔터티, Expire는 Die가 아님
    {
        float Lifespan { get; }
        void Expire();
    }

    public interface IActivatable // 발동 가능한 행동(공격, 기술)
    {
        float BaseCooldown { get; }
        float MaxCooldown { get; }
        float Cooldown { get; }
    }

    public interface IAttackable : IActivatable // 일반 공격
    {
        void Attack(float attackDamage);
    }

    public interface ICastable : IActivatable // 기술 캐스트
    {
        void Cast(CastKey key, float mana, Rigidbody2D target, bool absoluteTarget = false);
        void Cast(CastKey key, float mana, Vector2 targetArea, bool penetration = false);
    }
    public enum CastKey
    {
        Skill1, Skill2, Skill3, Ultimate, General // Default Shift, Q, E, R, F
    }

    public interface ITogglable // 토글 기술
    {
        bool IsOn { get; }
        void Toggle();
    }

    public interface IKnockbackable
    {
        void ApplyKnockback(UnityEngine.Vector2 direction, float force, float time, bool isFixed);
    }

    public interface IMovable
    {
        void Move(UnityEngine.Vector2 direction, Rigidbody2D rb);
        void Jump(float time, float wait = 1f);
    }

    public interface IAffectable
    {
        void ApplyEffect(Effects buffType, float duration, float Amplifier = 0);
        void Cleanse(Effects buffType);
    }

}

namespace StatsInterfaces
{
    public enum StatType
    {
        Health, HealthRegen,
        Armor, DamageReduction,
        AttackDamage,
        Mana, ManaRegen,
        Velocity, JumpTime
    }
    public enum StatRef
    {
        Base, Max, Current
    }
    public enum StatBool
    {
        OnGround, IsDead, IsImmune
    }
    public interface IStatProvider
    {
        float GetStat(StatType stat, StatRef re = StatRef.Current);
    }
    public interface IDefensiveStats
    {
        float BaseHealth { get; }
        float MaxHealth { get; }
        float Health { get; }
        float BaseHealthRegen { get; }
        float HealthRegen { get; }

        float BaseArmor { get; }
        float Armor { get; }

    }
    
    public interface IResistiveStats
    {
        float Shield {  get; } // 일반 보호막
        float SpecialShield { get; } //특수 보호막은 일반 보호막과 다름
        float BaseDamageReduction { get; }
        float DamageReduction { get; }
    }

    public interface IOffensiveStats
    {
        float BaseAttackDamage { get; }
        float AttackDamage { get; }
    }
    public interface ICasterStats
    {
        float BaseMana { get; }
        float MaxMana { get; }
        float Mana { get; }
        float BaseManaRegen { get; }
        float ManaRegen { get; }
    }

    public interface IMoverStats
    {
        bool OnGround { get; }
        float BaseVelocity { get; }
        float Velocity { get; }
        float JumpTime { get; }
    }
    public interface IEffectStats
    {
        Dictionary<Effects, float> EffectList { get; }
        float EffectResistance { get; }
        bool HasEffect(Effects e);
        HashSet<Effects> PositiveEffects { get; }
        HashSet<Effects> NegativeEffects { get; }
    }
}
