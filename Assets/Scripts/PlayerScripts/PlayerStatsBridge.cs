using StatsInterfaces;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

public class PlayerStatsBridge : MonoBehaviour, IStatProvider
{
	//Basically no logic here, just a bridge to PlayerStats
	//Should we change the name to PlayerStatsBridge?
	[SerializeField] PlayerStats stats;
	[SerializeField] PlayerEffects effects;
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
		switch(re)
		{
			case StatRef.Base:
				return t switch
				{
					StatType.Health => stats.BaseHealth,
					StatType.HealthRegen => stats.BaseHealthRegen,
					StatType.Armor => stats.BaseArmor,
					StatType.AttackDamage => stats.BaseAttackDamage,
					StatType.Mana => stats.BaseMana,
					StatType.ManaRegen => stats.BaseManaRegen,
					StatType.Speed => stats.BaseSpeed,
					_ => 0f,
				};
			case StatRef.Max:
				return t switch
				{
					StatType.Health => stats.MaxHealth,
					StatType.Mana => stats.MaxMana,
					_ => 0f,
				};
			case StatRef.Current:
				return t switch
				{
					StatType.Health => stats.Health,
					StatType.HealthRegen => stats.HealthRegen,
					StatType.Armor => stats.Armor,
					StatType.Shield => stats.Shield,
					StatType.SpecialShield => stats.SpecialShield,
					StatType.AttackDamage => stats.AttackDamage,
					StatType.Mana => stats.Mana,
					StatType.ManaRegen => stats.ManaRegen,
					StatType.Speed => stats.Speed,
					_ => 0f,
				};
			default:
				return -1f;
		}
	}
	public float GetArmorRatio() //플레이어가 피해를 가하는 경우에만 쓰임
	{
		return stats.TotalArmorPenetration();
	}
	public void ReduceStat(ReduceType stat, float damage, float armorRatio = 1f, DamageType type = DamageType.Normal) //The only way to access stats
	{
		stats.ReduceStat(stat, damage, armorRatio, type);
	}
}
/*public sealed class ArmorPenPercentMod : IStatModifier
{
	public readonly float Percent;  // 0~100
	public ArmorPenPercentMod(float percent) { Percent = Mathf.Clamp(percent, 0, 100); }
	public void Apply(PlayerStats s) => s.AddArmorPen(Percent);
	public void Remove(PlayerStats s) => s.RemoveArmorPen(Percent);
}*/