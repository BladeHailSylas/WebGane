using ActInterfaces;
using EffectInterfaces;
using StatsInterfaces;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
public class PlayerEffects : MonoBehaviour, IAffectable, IEffectStats
{
    public bool IsImmune { get; private set; } = false;
    public bool IsMovable { get; private set; } = true;
    public float EffectResistance { get; private set; } = 0f; // ȿ�� ���׷� (0% �⺻)
    public Dictionary<Effects, EffectState> EffectList { get; private set; } = new();
	public List<EffectState> StackList { get; private set; } = new();
    public HashSet<Effects> PositiveEffects { get; private set; } = new() { Effects.Haste, Effects.DamageBoost, Effects.ArmorBoost, Effects.APBoost, Effects.DRBoost, Effects.Invisibility, Effects.Invincible};
    public HashSet<Effects> NegativeEffects { get; private set; } = new() { Effects.Slow, Effects.Stun, Effects.Suppressed, Effects.Root, Effects.Tumbled, Effects.Damage };
    public HashSet<Effects> DisturbEffects { get; private set; } = new() { Effects.Slow, Effects.Stun, Effects.Suppressed, Effects.Root, Effects.Tumbled }; //���� ȿ��, Damage�� ������ ���� ȿ���̹Ƿ� EffectResistance�� ������ ���� ����, CC �� ����
    public HashSet<Effects> CCEffects { get; private set; } = new() { Effects.Stun, Effects.Suppressed, Effects.Root, Effects.Tumbled };
    public bool HasEffect(Effects e) => EffectList.ContainsKey(e); //CC Ȯ�ο� �ʿ���
	public void ApplyStack(string name, int amp = 1, GameObject go = null) //���� �� ������ �������� ApplyStack�� ���� ȣ��, ���� ���� ������ �״� ��� ApplyEffect�� �����ؾ� �Ѵ�
	{
		if (amp == 0) amp = 1;
		if (go == null) go = gameObject;
		foreach (var stack in StackList)
		{
			if (stack.effectName == name)
			{
				stack.amplifier += amp;
				return;
			}
		}
		StackList.Add(new EffectState(name, amp, go));
	}
	public void ApplyEffect(Effects buffType, GameObject effecter, float duration = float.PositiveInfinity, int amp = 0, string name = null)
	{
		if (buffType == Effects.Stack)
		{
			ApplyStack(name, amp, effecter);
		}
		else if (EffectList.ContainsKey(buffType))
		{
			EffectList[buffType].duration = Mathf.Max(EffectList[buffType].duration, duration);
			EffectList[buffType].amplifier += amp;
		}
		else
		{
			EffectList.Add(buffType, new EffectState(duration, amp, effecter));
			StartCoroutine(CoEffectDuration(buffType, duration));
		}
		Affection(buffType, duration, amp); // FixedUpdate()�� ȣ���� �ߺ��Ǿ� 2�� ����Ǵ� �Ϳ� ����, ���� ����Ʈ ������ �� �ڵ鸵 �ʿ�
											// FixedUpdate()�� Verify �� reload �뵵�� ����ϰ� �����Ƿ� ������ �� ����ؾ� ��
	}
	public void Affection(Effects buffType, float duration, float Amplifier = 0) // Amplifier�� ȿ���� ����, ���� ��� Haste/Slow�� �̵��ӵ� ������, Damage�� �ʴ� ���ط� -> �����, Damage�� Amplifier�� ��밡 ���ϴ� ���� ���ط��� �����Ƿ� ���⼭ DamageResistance�� ����ؾ� ��
    {
        Debug.Log($"Effect {buffType.GetType()} has affected for {duration} with amp {Amplifier}"); //Effect ������ �� Tumbled �̿��� DisturbEffects�� EffectResistance ������ ��
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
    public void Purify(Effects buffType) // ��ȭ, Cleanse�� ��� ������ ������ �� �ִ��� ������ �ʿ䰡 ����
                                                // ���� Cleanse�� ��������� ������ ȿ���� �������� �ʴ� ������ �νĵǹǷ� �������� �ʴ� �� ���� ��
                                                // ���� �Ѿ���(Tumbled)�� ���� ������ CC ȿ���� �����߱⿡ ��ȭ�� �������� �ʾƾ� �ϴ��� ��� �ʿ�
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
            if (DisturbEffects.Contains(effect) && EffectList[effect].duration != float.PositiveInfinity)
            {
                EffectList.Remove(effect);
            }
        }
    }
    /*void OnEnable()
    {
        EventBus.Subscribe<BuffApplyReq>(OnBuffApply);
        EventBus.Subscribe<BuffRemoveReq>(OnBuffRemove);
    }
    void OnDisable()
    {
        EventBus.Unsubscribe<BuffApplyReq>(OnBuffApply);
        EventBus.Unsubscribe<BuffRemoveReq>(OnBuffRemove);
    }

    void OnBuffApply(BuffApplyReq e)
    {
        if (e.Target != transform) return;
        e.Mod.Apply(this);
        if (e.Duration > 0) StartCoroutine(CoExpire(e.Mod, e.Duration));
    }
    void OnBuffRemove(BuffRemoveReq e)
    {
        if (e.Target != transform) return;
        e.Mod.Remove(this);
    }
    IEnumerator CoExpire(IEffectModifier mod, float dur)
    {
        yield return new WaitForSeconds(dur);
        mod.Remove(this);
    }*/
}