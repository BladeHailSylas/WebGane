using ActInterfaces;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;

namespace ActInterfaces
{
    public enum Effects
    {
        None = 0, Haste, DamageBoost, ReduceDamage, HealthRegen, ManaRegen, Slow, Stun, Silence, Root, Tumbled
    }
    public interface IVulnerable
    {
        void TakeDamage(float damage, float apRatio = 0, bool isFixed = false);
        void Die();

        bool IsDead { get; }
    }

    public interface IAttackable
    {
        float BasicCooldown { get; }
        float MaxCooldown { get; }
        float Cooldown { get; }
        void Attack();
        IEnumerator CoAttack();
    }

    public interface ICastable
    {
        void Cast(int spellIndex, float mana);
        IEnumerator CoCast(int spellIndex, float mana);
    }

    public interface IKnockbackable
    {
        void ApplyKnockback(UnityEngine.Vector2 direction, float force, float time, bool isFixed);
    }

    public interface IMovable
    {
        void Move(UnityEngine.Vector2 direction, float velocity);
        void Jump(float time);
    }

    public interface IAffectable
    {
        void ApplyEffect(Effects buffType, float duration);
        void Cleanse(Effects buffType);
    }

}

namespace StatsInterface
{
    public interface IDefensiveStats
    {
        float BasicHealth { get; }
        float MaxHealth { get; }
        float Health { get; }
        float BasicHealthRegen { get; }
        float HealthRegen { get; }

        float BasicArmor { get; }
        float Armor { get; }

    }

    public interface IOffensiveStats
    {
        float BasicAttackDamage { get; }
        float AttackDamage { get; }
        float BasicAttackSpeed { get; }
        float AttackSpeed { get; }
    }
    public interface ICasterStats
    {
        float BasicMana { get; }
        float MaxMana { get; }
        float Mana { get; }
        float BasicManaRegen { get; }
        float ManaRegen { get; }
    }

    public interface IMovingStats
    {
        float BasicVelocity { get; }
        float Velocity { get; }

        float JumpTime { get; }
    }
    public interface IEffectStats
    {
        Dictionary<Effects, float> EffectList { get; }
        float EffectResistance { get; }
    }
}
