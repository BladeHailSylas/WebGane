// SkillRunner.cs — Intent 전용 Runner. 액터당 1개만 존재해야 합니다.
// - Runner는 IntentOrchestrator로 Root Intent만 전달합니다.
// - FollowUp은 CastScope.AddIntent를 통해 Orchestrator가 직접 수집합니다.
// - Busy/Cooldown 상태는 Orchestrator에서 관리합니다.
// 잠재적 문제: 현재 Owner Transform을 Awake 시점에 고정하며, 런타임에 변경되면 Intent가 잘못된 위치를 참조할 수 있습니다.

using Intents;
using SkillInterfaces;
using Unity.VisualScripting;
using UnityEngine;

public sealed class SkillRunner : MonoBehaviour, ISkillRunner
{
	[Header("Actor")]
	[SerializeField] int actorId = 1;
	[SerializeField] Camera boundCamera;

	[Header("Queueing")]
	[SerializeField] bool respectBusyCooldown = true;
	[SerializeField] int defaultPriority;

	[Header("Debug")]
	[SerializeField] bool verbose;

	int _rootSequence;
	IntentOrchestrator _orchestrator;

	void Awake()
	{
		_orchestrator = IntentOrchestrator.Instance;
		if (_orchestrator == null)
		{
			Debug.LogError("IntentOrchestrator 인스턴스를 찾을 수 없습니다. Runner가 작동하지 않습니다.");
		}
		boundCamera ??= Camera.main;
	}

	void OnEnable()
	{
		_orchestrator ??= IntentOrchestrator.Instance;
		if (_orchestrator == null)
		{
			Debug.LogWarning("Orchestrator 부재: Intent를 큐잉할 수 없습니다.");
		}
	}

	public bool IsBusy => _orchestrator != null && _orchestrator.IsActorBusy(actorId);

	public bool IsOnCooldown => _orchestrator != null && _orchestrator.IsActorOnCooldown(actorId);

	public void EnqueueRootIntent(ISkillMechanism mech, ISkillParam param, TargetRequest request, int priorityLevel = 0)
	{
		if (mech == null || param == null)
		{
			Debug.LogWarning("EnqueueRootIntent: 메커니즘 또는 파라미터가 null입니다.");
			return;
		}
		_orchestrator ??= IntentOrchestrator.Instance;
		if (_orchestrator == null)
		{
			Debug.LogError("Orchestrator가 없어 Intent를 큐잉할 수 없습니다.");
			return;
		}

		if (!mech.ParamType.IsInstanceOfType(param))
		{
			Debug.LogError($"ParamType mismatch: {mech.ParamType.Name} 필요, {param.GetType().Name} 제공");
			return;
		}

		var intent = CastIntent.Root(
			actorId,
			++_rootSequence,
			mech,
			param,
			request,
			respectBusyCooldown,
			priorityLevel == 0 ? defaultPriority : priorityLevel,
			transform,
			boundCamera);

		// Guard/Dedup 기본값 설정. 실제 프로젝트에서는 Skill 고유 키로 치환 필요.
		intent.DedupKey ??= $"root:{actorId}:{intent.RootCastId}";
		intent.GuardKey ??= $"guard:{actorId}:{intent.RootCastId}";

		_orchestrator.Enqueue(intent);

		if (verbose)
		{
			Debug.Log($"[Runner] Root Intent enqueue: {intent}");
		}
	}
	public int SkillPriority(ISkillMechanism mech, ICooldownParam param, SkillSlot slot) 
	{
		if(mech == null || param == null)
		{
			Debug.LogWarning("SkillPriority: 메커니즘 또는 파라미터가 null입니다. Priority level이 임시로 0이 됩니다.");
			return 0;
		}
		if(!mech.ParamType.IsInstanceOfType(param))
		{
			Debug.LogError($"ParamType mismatch: {mech.ParamType.Name} 필요, {param.GetType().Name} 제공. Priority level이 임시로 -1이 됩니다.");
			return -1;
		}
		int weight = 0;
		switch(mech.ParamType.Name)
		{
			case "MeleeParams":
			case "MissileParams":
			case "HitscanParams":
			case "AreaParams":
				weight += 3;
				break;
			case "DashParams":
			case "TeleportParams":
				weight += 2;
				break;
			default:
				weight += 1;
				break;
		}
		switch(slot)
		{
			case SkillSlot.Attack:
				weight += 10;
				break;
			case SkillSlot.AttackSkill:
			case SkillSlot.Skill1:
			case SkillSlot.Skill2:
				weight += 20;
				break;
			case SkillSlot.Ultimate:
				weight += 30;
				break;
			default:
				weight = 0;
				break;
		}
		Debug.Log($"[Runner] SkillPriority: {slot} 슬롯의 {mech.ParamType.Name} 타입은 {weight * 1000} priority입니다");
		return weight * 1000;
	}
}
