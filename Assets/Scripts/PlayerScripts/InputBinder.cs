// PlayerAttackController.cs — Intent 기반 입력 파이프라인.
// 잠재적 문제: Runner가 존재하지 않으면 입력이 조용히 무시되므로, 에디터 툴에서 검증 루틴을 추가해야 합니다.
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using SkillInterfaces;
using Intents;

public class InputBinder : MonoBehaviour
{
	[Header("Character")]
	public CharacterSpec spec;
	[Header("Input")]
	public InputActionReference attackKey;
	public InputActionReference skill1Key;
	public InputActionReference skill2Key;
	public InputActionReference ultimateKey;
	private InputSystem_Actions _controls;
	private PlayerActController _acter;
	private PlayerAttackController _attacker;
	readonly Dictionary<SkillSlot, (ISkillMechanism mech, ISkillParam param)> _slotBindings = new();
	private byte _mySid;
	void Awake()
	{
		_acter = new();
		_attacker = new();
		if (spec is null)
		{
			Debug.LogError("CharacterSpec이 할당되지 않았습니다. 게임을 정상 진행할 수 없습니다.");
		}
		else
		{
			Bind(spec.attack);
			Bind(spec.skill1);
			Bind(spec.skill2);
			Bind(spec.ultimate);
		}
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
		/*_controls.Player.Move.performed += ctx => _acter.MakeMove(new FixedVector2(ctx.ReadValue<Vector2>()));
		_controls.Player.Move.canceled += _ => _acter.MakeMove(Vector2.zero);
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
		}*/
	}
	private void TickHandler(ushort tick)
	{
		
	}
}