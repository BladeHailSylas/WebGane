using ActInterfaces;
using Generals;
using StatsInterfaces;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class PlayerEffects : MonoBehaviour, IAffectable, IEffectStats
{
    public bool IsImmune { get; private set; } = false;
    public bool IsMovable { get; private set; } = true;
    public float EffectResistance { get; private set; } = 0f; // ȿ�� ���׷� (0% �⺻)
    public Dictionary<Effects, EffectState> EffectList { get; private set; } = new();
    public HashSet<Effects> PositiveEffects { get; private set; } = new() { Effects.Haste, Effects.DamageBoost, Effects.ReduceDamage, Effects.GainHealth, Effects.GainMana, Effects.Invisibility };
    public HashSet<Effects> NegativeEffects { get; private set; } = new() { Effects.Slow, Effects.Stun, Effects.Silence, Effects.Root, Effects.Tumbled, Effects.Damage };
    public HashSet<Effects> DisturbEffects { get; private set; } = new() { Effects.Slow, Effects.Stun, Effects.Silence, Effects.Root, Effects.Tumbled }; //���� ȿ��, Damage�� ������ ���� ȿ���̹Ƿ� EffectResistance�� ������ ���� ����, CC �� ����
    public HashSet<Effects> CCEffects { get; private set; } = new() { Effects.Stun, Effects.Silence, Effects.Root, Effects.Tumbled };
    public bool HasEffect(Effects e) => EffectList.ContainsKey(e); //CC Ȯ�ο� �ʿ���

    public void ApplyEffect(Effects buffType, float duration = -1, int amp = 0) //Effect�� Stats�� ������ �ֹǷ� ���⼭ �����ϰ� �ͱ�� �ѵ�, Ȯ�强�� �����ϸ� Effects�� ���� �δ� �� ��������
    {
        if (EffectList.ContainsKey(buffType))
        {
            EffectList[buffType].duration = Mathf.Max(EffectList[buffType].duration, duration);
            EffectList[buffType].amplifier += amp;
        }
        else
        {
            EffectList.Add(buffType, new EffectState(duration, amp));
            StartCoroutine(CoEffectDuration(buffType, duration));
        }
        Affection(buffType, duration, amp); // FixedUpdate()�� ȣ���� �ߺ��Ǿ� 2�� ����Ǵ� �Ϳ� ����, ���� ����Ʈ ������ �� �ڵ鸵 �ʿ�
                                            // FixedUpdate()�� Verify �� reload �뵵�� ����ϰ� �����Ƿ� ������ �� ����ؾ� ��
    }
    public void Affection(Effects buffType, float duration, float Amplifier = 0) // Amplifier�� ȿ���� ����, ���� ��� Haste/Slow�� �̵��ӵ� ������, Damage�� �ʴ� ���ط� -> �����, Damage�� Amplifier�� ��밡 ���ϴ� ���� ���ط��� �����Ƿ� ���⼭ DamageResistance�� ����ؾ� ��
    {
        Debug.Log($"Effect {buffType.GetType()} has affected for {duration} with amp {Amplifier}"); //Effect ������ ��, DisturbEffects�� EffectResistance ������ ��
        if(CCEffects.Contains(buffType))
        {
            IsMovable = false;
        }
    }
    public IEnumerator CoEffectDuration(Effects e, float duration) //Effect ���ӽð� ����
    {
        while (duration > 0f)
        {
            duration -= Time.deltaTime;
            EffectList[e].duration = duration;
            yield return null;
        }
        EffectList.Remove(e);
    }
    public void Cleanse(Effects buffType) // Cleanse�� ȣ���ϴ� �޼���, ������ ȿ���� ������ �� �ִ����� ������ ���� �� ��
                                                // ���� Cleanse�� ��������� ������ ȿ���� �������� �ʴ� ������ �νĵǹǷ� �������� �ʴ� �� ���� ��
    {
        if (EffectList.ContainsKey(buffType))
        {
            EffectList.Remove(buffType);
        }
    }
    public void ClearNegative() // ���� ȿ�� ��ü ����: ���� ���� ȿ���� �������� �ʴ� ���谡 ����(������ ��), Cleanse�� ȣ���ϴ� �޼���
                                // ���� ȿ���� ���Ǹ� "����/������ ������ ����� �ൿ�� �� ���� ����� ȿ��" �� �����ϸ� ��, ���� ���ش� �޵� ��¼�� �ൿ�� �Ǵϱ�
                                // "���� ���ط� ������ ����� �ൿ�� �� ���ݾƿ�!" << ������ ����/������ �Ŀ��� ����� �ൿ�� �� ���� �Ŵϱ� ��� ����
    {
        foreach (var effect in EffectList.Keys)
        {
            if (DisturbEffects.Contains(effect))
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