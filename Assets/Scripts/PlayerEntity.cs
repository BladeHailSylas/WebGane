using UnityEngine;
using ActInterfaces;
using StatsInterface;
using NUnit.Framework;
using System.Collections.Generic;

public class PlayerEntity : MonoBehaviour
{
    
    public void TakeDamage(float damage, float apratio, bool istrue)
    {
        if(istrue)
        {
            Health -= damage;
        }
        else
        {
            Health -= damage * (Armor * (1 - apratio) / (80 + Armor * (1 - apratio)));
        }
    }
    public void Die()
    {
        IsDead = true;
        Destroy(gameObject);
    }
    public void ApplyEffect(Effects buffType, float duration)
    {
        if (BuffList.ContainsKey(buffType))
        {
            BuffList[buffType] = duration;
        }
        else
        {
            BuffList.Add(buffType, duration);
        }
    }
    public void Cleanse(Effects buffType)
    {
        if (buffType == Effects.None)
        {
            BuffList.Clear();
        }
        else if (BuffList.ContainsKey(buffType))
        {
            BuffList.Remove(buffType);
        }
    }
    public void ApplyKnockback(Vector2 direction, float force, float time, bool isFixed)
    {
        throw new System.NotImplementedException();
    }
    void Awake()
    {
    }
}
public class PlayerStats : IDefensiveStats, IOffensiveStats, ICasterStats, IMovingStats, 
{
    public float BasicHealth { get; private set; }
    public float MaxHealth { get; private set; }
    public float Health { get; private set; }
    public float BasicArmor { get; private set; }
    public float Armor { get; private set; }
    public float BasicVelocity { get; private set; }
    public float Velocity { get; private set; }
    public bool IsDead { get; private set; }
    public bool IsImmune { get; private set; }
    public Dictionary<Effects, float> EffectList { get; private set; }
}