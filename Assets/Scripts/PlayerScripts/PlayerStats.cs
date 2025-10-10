using StatsInterfaces;
using System.Collections.Generic;
using UnityEngine;
public sealed class PlayerStats : MonoBehaviour // 플레이어 스탯 관리, 다른 곳에서는 참조만
{
	[SerializeReference] readonly CharacterSpec spec;
	public float BaseHealth { get; private set; }
	public float MaxHealth { get; private set; }
	public float Health { get; private set; }
	public float Shield { get; private set; }
	public float SpecialShield { get; private set; }
	public float BaseArmor { get; private set; }
	public float Armor { get; private set; }
	public float BaseHealthRegen { get; private set; }
	public float HealthRegen { get; private set; }
	public List<float> DamageReduction { get; private set; } = new();
	public float BaseAttackDamage { get; private set; } = 10f;
	public float AttackDamage { get; private set; } = 12f;
	public List<float> ArmorPenetration { get; private set; } = new();
	public float BaseMana { get; private set; }
	public float MaxMana { get; private set; }
	public float Mana { get; private set; }
	public float BaseManaRegen { get; private set; }
	public float ManaRegen { get; private set; }
	public float BaseSpeed { get; private set; } = 8f;
	public float Speed { get; private set; } = 8f;
	public float JumpTime { get; private set; }
	public bool OnGround { get; private set; }
	public bool IsDead { get; private set; }
	private void Awake()
	{
		BaseHealth = spec.baseHP;
		BaseHealthRegen = spec.baseHPGen;
		BaseArmor = spec.baseDefense;
		BaseAttackDamage = spec.baseAttack;
		BaseMana = spec.baseMana;
		BaseManaRegen = spec.baseManaGen;
		BaseSpeed = spec.baseSpeed;
	}
	public void ReduceStat(ReduceType stat, float damage, float apRatio = 0f, DamageType type = DamageType.Normal)
	{
		if (stat == ReduceType.Mana) // 1(Mana)이면 마나, 0(Health)이면 체력
		{
			Mana = Mathf.Max(0f, Mana - damage);
		}
		else
		{
			GetDamage(damage, apRatio, type);
		}
		if (Health <= 0f) IsDead = true;
	}
	void GetDamage(float damage, float apRatio = 0f, DamageType type = DamageType.Normal) //비례 피해를 여기서 계산해야 되나 << 그럴 거 같지 않음
	{
		if (IsDead || damage <= 0f) return;
		switch (type) 
		{ 
			case DamageType.CurrentPercent:
				damage = Health * damage / 100f;
				break;
			case DamageType.LostPercent:
				damage = (MaxHealth - Health) * damage / 100f;
				break;
			case DamageType.MaxPercent:
				damage = MaxHealth * damage / 100f;
				break;
			default:
				break;
		}
		if (type != DamageType.Fixed) damage *= DamageReductionCalc(Armor, apRatio, TotalDamageReduction());
		if (SpecialShield > damage)
		{
			SpecialShield -= damage;
		}
		else if (SpecialShield + Shield > damage)
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
	float DamageReductionCalc(float armor, float apRatio = 0f, float damageRatio = 1f) //Player가 피해를 받는 경우
	{
		return (80 / (80 + armor * (1 - apRatio))) * damageRatio;
	}
	public float TotalArmorPenetration() //AP를 반환하는 거면 1 - totalAP가 맞는데 그럼 계산이 귀찮아짐, 명칭을 바꾸는 것이 맞지 않나
										//어쩌면 괜찮을지도 모르겠다, 어차피 AP 계산식은 1 - (80 / (80 + Armor * (1 - TotalAP)))로 이미 정해져 있으니까
										//오히려 그 공식을 바꾸려 들었다가 수식이 달라 혼란이 올 가능성이 있음, 이름도 ArmorPenetration에서 ArmorRatio로 바꿔야 됨
	{
		float totalAP = 1f;
		foreach (var ap in ArmorPenetration)
		{
			totalAP *= (1 - ap / 100); // 단일 AP 비율이 100%를 넘으면 방어력이 마이너스라 피해 배율이 너무 커지므로 그런 일이 없어야 함
		}
		return 1 - totalAP;
	}
	public float TotalDamageReduction()
	{
		float totalDR = 1f;
		foreach (var dr in DamageReduction)
		{
			totalDR *= (1 - dr / 100);
		}
		return Mathf.Max(0.15f, totalDR); //공격자 우선(하게 두되 대안을 주어라) -> 대미지가 들어가게 두되 다른 생존 수단(체력 회복, 보호막 등)으로 원콤이 안 나게 하라
											//왜 하한을 두나요? 안 그러면 맞는데 피가 닳는 대신 회복하는 망겜이 되어버림
	}
}