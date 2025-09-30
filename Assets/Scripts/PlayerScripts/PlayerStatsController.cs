using StatsInterfaces;
using UnityEngine;

public class PlayerStatsController : MonoBehaviour, IStatProvider
{
    [SerializeField] PlayerStatsContainer stats;
    public float GetBool(StatBool sb)
    {
        return sb switch
        {
            StatBool.OnGround => stats.OnGround ? 1f : 0f,
            StatBool.IsDead => stats.IsDead ? 1f : 0f,
            _ => 0f,
        };
    }
    public float GetStat(StatType t, StatRef re = StatRef.Current)
    {
        return 1f;
    }
    /*public float GetStat(StatType t, StatRef re = StatRef.Current) //너무 길어진다?
    {
        return re switch
        {
            StatRef.Base => t switch
            {
                StatType.Health => BaseHealth,
                StatType.HealthRegen => BaseHealthRegen,
                StatType.Armor => BaseArmor,
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
                StatType.AttackDamage => AttackDamage,
                StatType.Mana => Mana,
                StatType.ManaRegen => ManaRegen,
                StatType.Velocity => Velocity,
                _ => 0f,
            },
            _ => 0f,
        };
    }*/
    public float GetArmorRatio() //플레이어가 피해를 가하는 경우에만 쓰임
    {
        return stats.TotalArmorPenetration();
    }
}
/*public sealed class ArmorPenPercentMod : IStatModifier
{
    public readonly float Percent;  // 0~100
    public ArmorPenPercentMod(float percent) { Percent = Mathf.Clamp(percent, 0, 100); }
    public void Apply(PlayerStats s) => s.AddArmorPen(Percent);
    public void Remove(PlayerStats s) => s.RemoveArmorPen(Percent);
}*/