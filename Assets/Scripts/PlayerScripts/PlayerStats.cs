using StatsInterfaces;
using System.Collections.Generic;
using UnityEngine;
public sealed class PlayerStats : MonoBehaviour // �÷��̾� ���� ����, �ٸ� �������� ������
{
    [SerializeField] PlayerEffects effects;
    public float BaseHealth { get; private set; }
    public float MaxHealth { get; private set; }
    public float Health { get; private set; }
    public float Shield { get; private set; }
    public float SpecialShield { get; private set; }
    public float BaseArmor { get; private set; }
    public float Armor { get; private set; }
    public float BaseHealthRegen { get; private set; }
    public float HealthRegen { get; private set; }
    public List<float> DamageReduction { get; private set; }
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
    public void ReduceStat(ReduceType stat, float damage, float armorRatio = 1f, bool isfixed = false)
    {
        if (stat == ReduceType.Mana) // 1(Mana)�̸� ����, 0(Health)�̸� ü��
        {
            Mana = Mathf.Max(0f, Mana - damage);
        }
        else
        {
            Damaged(damage, armorRatio, isfixed);
        }
        if (Health <= 0f) IsDead = true;
    }
    float DamageReductionCalc(float armor, float armorRatio = 1f, float damageRatio = 1f) //Player�� ���ظ� �޴� ���
    {
        return (80 / (80 + armor * armorRatio)) * damageRatio;
    }
    void Damaged(float damage, float armorRatio = 1f, bool isfixed = false) //��� ���ظ� ���⼭ ����ؾ� �ǳ� << �׷� �� ���� ����
    {
        if (IsDead || damage <= 0f) return;
        if (!isfixed) damage *= DamageReductionCalc(Armor, armorRatio, TotalDamageReduction());
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
    public float TotalArmorPenetration() //�ʵ带 �ǵ�� �޼��尡 �ƴϹǷ� public���� �ξ ���� ������
    {
        float totalAP = 1f;
        foreach (var ap in ArmorPenetration)
        {
            totalAP *= (1 - ap / 100); // ���� AP ������ 100%�� ������ ������ ���̳ʽ��� ���� ������ �ʹ� Ŀ���Ƿ� �׷� ���� ����� ��
        }
        return totalAP;
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
//PlayerStats�� �� ������µ� ���� �з��� �����?
//Health, Mana�� ���� ���� array�� List�� ��� enum StatRef(Base, Max, Current)�� �����ؾ� �ǳ�?
//�ƴϸ� ������� ���� �ʿ����̹Ƿ� �׳� ������ �д�? Stat���� �ʹ� ���Ƽ� ��¿ �� ����?
//PlayerStatsController�� �����Ѵٰ� �ص� �޼��带 ���ǹ��ϰ� ������ �� ���� ������?
//���⿡ event listening���� �ϸ� �ڵ尡 �� ����� ���ε�
//�׷��ٰ� PlayerStatsController�� PlayerStats�� ���� �����ϴ� �� �ȴ�, ���� ���͸� ���̰� ����
//�ƴϸ� ������ Controller�� �ֱ⿡ ���� ���Ͱ� �پ���?
//���� ����: �ٸ� ��ü�� �ʿ��� �ñ⿡ ���� PlayerStats�� ����
//��Ʈ�ѷ�: �ٸ� ��ü�� Controller�� ����, Controller�� PlayerStats�� ����
//�̰� �� ���� �� ����� ������, �츮�� �ñ������� event�� ���� SerializeField ���� ���Ÿ� ����
//event�� SerializeField ��ü ������ ���������� �� ���� ������ �� ���� ���͸� ���� �� ���� ������ ����
//StatsEventListener���� �� �����? ������ ���� ���͸� �ø��� ��� ���ٰ� ����