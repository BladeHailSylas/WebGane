using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EffectInterfaces;
using StatsInterfaces;

namespace EffectInterfaces
{
    public enum Effects
    {
        None = 0, Haste, DamageBoost, ReduceDamage, GainHealth, GainMana, Invisibility, Slow, Stun, Suppressed, Root, Tumbled, Damage //Damage는 지속 피해, duration을 0으로 하면 즉시 피해도 가능함
    }
    public class EffectState
    {
        public float duration;
        public int amplifier;
        public GameObject effecter;
        public EffectState()
        { 
        }
        public EffectState(float dur, int amp, GameObject eft)
        {
            duration = dur; amplifier = amp; effecter = eft;
        }
    }
    public interface IEffectStats
    {
        Dictionary<Effects, EffectState> EffectList { get; }
        float EffectResistance { get; }
        bool HasEffect(Effects e);
        HashSet<Effects> PositiveEffects { get; }
        HashSet<Effects> NegativeEffects { get; }
    }
    public interface IEffectModifier
    {
        void Apply(PlayerEffects effects);
        void Remove(PlayerEffects effects);
    }
}
namespace ActInterfaces
{
    public interface IVulnerable //피해를 받아 죽을 수 있음
    {
        void TakeDamage(float damage, float apratio = 1f, bool isFixed = false);
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
        //
        //void Drain(float amount); // 피흡, 생명력 흡수 스테이터스가 없을 경우에는?
    }

    public interface ICastable : IActivatable // 기술 캐스트
    {
        void Cast(CastKey key);
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
        void ApplyKnockback(Vector2 direction, float force);
    }

    public interface IMovable
    {
        void Move(Vector2 direction, Rigidbody2D rb, float velocity);
        void Jump(float time, float wait = 1f);
    }

    public interface IAffectable
    {
        void ApplyEffect(Effects buffType, float duration, int Amplifier = 0);
        void Purify(Effects buffType);
    }
    public interface ITargetable
    {
        bool TryGetTarget(out Transform target); // 잠금 대상이 없으면 false
    }
}
namespace StatsInterfaces
{
    public enum StatType
    {
        Health, HealthRegen,
        Armor, DamageReduction,
        Shield, SpecialShield,
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
    public enum ReduceType
    {
        Health = 0, Mana
    }
    public interface IStatProvider
    {
        float GetStat(StatType stat, StatRef re = StatRef.Current);
    }
    /*public interface IDefensiveStats
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
        List<float> ArmorPenetration { get; }
    }
    public interface ICasterStats
    {
        float BaseMana { get; }
        float MaxMana { get; }
        float Mana { get; }
        float BaseManaRegen { get; }
        float ManaRegen { get; }
        //쿨타임?
    }

    public interface IMoverStats
    {
        bool OnGround { get; }
        float BaseVelocity { get; }
        float Velocity { get; }
        float JumpTime { get; }
    }*/
    public interface IStatModifier
    {
        void Apply(PlayerStats stats);
        void Remove(PlayerStats stats);
    }
}
namespace SOInterfaces
{
    public enum SkillSlot { Attack, Skill1, Skill2, Ultimate }
    public interface ISkillParam { }                    // 파라미터 마커
    public interface IHasCooldown : ISkillParam { float Cooldown { get; } }

    public interface ISkillRunner
    {
        bool IsBusy { get; }
        bool IsOnCooldown { get; }
        void TryCast();
    }

    // 메커니즘(공식): "캐스팅 코루틴"을 제공
    public interface ISkillMechanic
    {
        System.Type ParamType { get; }
        IEnumerator Cast(Transform owner, Camera cam, ISkillParam param);
    }
    public interface ITargetedMechanic : ISkillMechanic
    {
        IEnumerator Cast(Transform owner, Camera cam, ISkillParam param, Transform target);
    }

    // 제네릭 베이스: 타입 가드 + 제네릭 오버로드

    public abstract class SkillMechanicBase<TParam> : ScriptableObject, ISkillMechanic
        where TParam : ISkillParam
    {
        public System.Type ParamType => typeof(TParam);

        public IEnumerator Cast(Transform owner, Camera cam, ISkillParam param)
        {
            if (param is not TParam p)
                throw new System.InvalidOperationException(
                    $"Param type mismatch. Need {typeof(TParam).Name}, got {param?.GetType().Name ?? "null"}");
            return Cast(owner, cam, p);
        }
        
        public abstract IEnumerator Cast(Transform owner, Camera cam, TParam param);
    }

}