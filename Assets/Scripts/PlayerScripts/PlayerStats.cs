using StatsInterfaces;
using System.Collections.Generic;
using UnityEngine;
public sealed class PlayerStats : MonoBehaviour // �÷��̾� ���� ����, �ٸ� �������� ������
{
	[SerializeField] readonly CharacterSpec spec;
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
		if (stat == ReduceType.Mana) // 1(Mana)�̸� ����, 0(Health)�̸� ü��
		{
			Mana = Mathf.Max(0f, Mana - damage);
		}
		else
		{
			GetDamage(damage, apRatio, type);
		}
		if (Health <= 0f) IsDead = true;
	}
	void GetDamage(float damage, float apRatio = 0f, DamageType type = DamageType.Normal) //��� ���ظ� ���⼭ ����ؾ� �ǳ� << �׷� �� ���� ����
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
	float DamageReductionCalc(float armor, float apRatio = 0f, float damageRatio = 1f) //Player�� ���ظ� �޴� ���
	{
		return (80 / (80 + armor * (1 - apRatio))) * damageRatio;
	}
	public float TotalArmorPenetration() //AP�� ��ȯ�ϴ� �Ÿ� 1 - totalAP�� �´µ� �׷� ����� ��������, ��Ī�� �ٲٴ� ���� ���� �ʳ�
	{
		float totalAP = 1f;
		foreach (var ap in ArmorPenetration)
		{
			totalAP *= (1 - ap / 100); // ���� AP ������ 100%�� ������ ������ ���̳ʽ��� ���� ������ �ʹ� Ŀ���Ƿ� �׷� ���� ����� ��
		}
		return 1 - totalAP;
	}
	public float TotalDamageReduction()
	{
		float totalDR = 1f;
		foreach (var dr in DamageReduction)
		{
			totalDR *= (1 - dr / 100); //���� DR ������ 100%�� ������ �´µ� ȸ����, AP�� 100%�� ������ �� ������ DR�� ������ ���� ������ 100%�� �Ѿ�� �� ��(������)
		}
		return Mathf.Max(0.15f, totalDR); //������ �켱(�ϰ� �ε� ����� �־��) -> ������� ���� �ε� �ٸ� ���� ����(ü�� ȸ��, ��ȣ�� ��)���� ������ �� ���� �϶�
	}
}