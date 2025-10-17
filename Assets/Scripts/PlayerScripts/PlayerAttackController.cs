// PlayerAttackController.cs — Intent 기반 입력 파이프라인.
// 잠재적 문제: Runner가 존재하지 않으면 입력이 조용히 무시되므로, 에디터 툴에서 검증 루틴을 추가해야 합니다.
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using SkillInterfaces;
using Intents;

public class PlayerAttackController : MonoBehaviour
{
	[Header("Character")]
	public CharacterSpec spec;

	[Header("Input")]
	public InputActionReference attackKey;
	public InputActionReference skill1Key;
	public InputActionReference skill2Key;
	public InputActionReference ultimateKey;

	ISkillRunner _runner;
	readonly Dictionary<SkillSlot, (ISkillMechanism mech, ISkillParam param)> _slotBindings = new();
	private byte _mySID;
	void Awake()
	{
		if (spec == null)
		{
			Debug.LogError("CharacterSpec이 할당되지 않았습니다. PlayerAttackController가 작동하지 않습니다.");
			return;
		}
		_runner = GetComponentInChildren<ISkillRunner>();
		Bind(spec.attack);
		Bind(spec.skill1);
		Bind(spec.skill2);
		Bind(spec.ultimate);
	}

	void Bind(SkillBinding binding)
	{
		if (binding.mechanism is not ISkillMechanism mech || binding.param == null)
			return;

		if (!mech.ParamType.IsInstanceOfType(binding.param))
		{
			Debug.LogError($"Param mismatch: need {mech.ParamType.Name}, got {binding.param.GetType().Name}");
			return;
		}

		var runner = GetComponentInChildren<ISkillRunner>();
		if (runner == null)
		{
			Debug.LogError("ISkillRunner 구현체를 찾지 못했습니다. 액터 루트에 Runner 1개가 필요합니다.");
			return;
		}
		_slotBindings[binding.slot] = (mech, binding.param);
	}

	void OnEnable()
	{
		if (attackKey)
		{
			attackKey.action.Enable();
			attackKey.action.performed += _ => TryCast(SkillSlot.Attack);
		}
		if (skill1Key)
		{
			skill1Key.action.Enable();
			skill1Key.action.performed += _ => TryCast(SkillSlot.Skill1);
		}
		if (skill2Key)
		{
			skill2Key.action.Enable();
			skill2Key.action.performed += _ => TryCast(SkillSlot.Skill2);
		}
		if (ultimateKey)
		{
			ultimateKey.action.Enable();
			ultimateKey.action.performed += _ => TryCast(SkillSlot.Ultimate);
		}
	}
	void TryCast(SkillSlot slot)
	{
		if (_runner == null) return;
		if (!_slotBindings.TryGetValue(slot, out var binding)) return;
	}
	public int SkillPriority(ISkillMechanism mech, ISkillParam param, SkillSlot slot)
	{
		return SkillPriority(mech, param as ICooldownParam, slot);
	}
	public int SkillPriority(ISkillMechanism mech, ICooldownParam param, SkillSlot slot)
	{
		if (mech == null || param == null)
		{
			Debug.LogWarning("SkillPriority: 메커니즘 또는 파라미터가 null입니다. Priority level이 임시로 0이 됩니다.");
			return 0;
		}
		if (!mech.ParamType.IsInstanceOfType(param))
		{
			Debug.LogError($"ParamType mismatch: {mech.ParamType.Name} 필요, {param.GetType().Name} 제공. Priority level이 임시로 -1이 됩니다.");
			return -1;
		}
		int weight = 0;
		weight += mech.ParamType.Name switch
		{
			"MeleeParams" or "MissileParams" or "HitscanParams" or "AreaParams" => 3,
			"DashParams" or "TeleportParams" => 2,
			_ => 1,
		};
		weight += slot switch
		{
			SkillSlot.Attack => 1,
			SkillSlot.AttackSkill or SkillSlot.Skill1 or SkillSlot.Skill2 => 2,
			SkillSlot.Ultimate => 3,
			_ => 0,
		};
		//Debug.Log($"[Runner] SkillPriority: {slot} 슬롯의 {mech.ParamType.Name} 타입은 {weight * 1000} priority입니다");
		return weight * 1000 + (int)param.Cooldown;
	}
}