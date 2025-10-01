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

	ISkillRunner runner;
	readonly Dictionary<SkillSlot, (ISkillMechanic mech, ISkillParam param)> slotBindings = new();

	void Awake()
	{
		if (spec == null)
		{
			Debug.LogError("CharacterSpec이 할당되지 않았습니다. PlayerAttackController가 작동하지 않습니다.");
			return;
		}
		runner = GetComponentInChildren<ISkillRunner>();
		Bind(spec.attack);
		Bind(spec.skill1);
		Bind(spec.skill2);
		Bind(spec.ultimate);
	}

	void Bind(SkillBinding binding)
	{
		if (binding.mechanism is not ISkillMechanic mech || binding.param == null)
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
		slotBindings[binding.slot] = (mech, binding.param);
	}

	void Attack() => TryCast(SkillSlot.Attack);
	void Skill1() => TryCast(SkillSlot.Skill1);
	void Skill2() => TryCast(SkillSlot.Skill2);
	void Ultimate() => TryCast(SkillSlot.Ultimate);

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
		if (runner == null) return;
		if (!slotBindings.TryGetValue(slot, out var binding)) return;

		var request = new TargetRequest
		{
			Policy = TargetPolicy.SameAsCast,
			ExplicitActor = transform,
			Radius = 0f,
			TeamMask = ~0,
		};

		// 추후 목표 포인트(조준선/커서 등)를 반영할 경우 TargetRequest를 갱신해야 합니다.
		runner.EnqueueRootIntent(binding.mech, binding.param, request, priorityLevel: 0);
	}
}