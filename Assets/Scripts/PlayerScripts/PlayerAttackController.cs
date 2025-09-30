// PlayerAttackController.cs  (교체)
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using SkillInterfaces;

public class PlayerAttackController : MonoBehaviour
{
	[Header("Character")]
	public CharacterSpec spec; // 캐릭터 SO (슬롯→메커니즘+파라미터)
								//public ICharacter Spec { get { return spec; } set { spec = value; } }

	[Header("Input")]
	public InputActionReference attackKey;   // LMB(+Passive), slot 0
	public InputActionReference skill1Key;   // Shift, slot 1
	public InputActionReference skill2Key;   // Space, slot 2
	public InputActionReference ultimateKey; // RMB, slot 3
											//InputSystem has hold feature, can we use it for delay skills?
	readonly List<InputActionReference> keys = new();
	//readonly Dictionary<SkillSlot, ISkillRunner> runners = new(); -> May noy be used since we have only one runner, haha in your face
	readonly List<SkillSlot> slots = new();
	ISkillRunner r;

    void Awake()
    {
        Bind(spec.attack);
		Bind(spec.skill1);
        Bind(spec.skill2);
		Bind(spec.ultimate);
		keys.Add(attackKey);
		keys.Add(skill1Key);
		keys.Add(skill2Key);
		keys.Add(ultimateKey);
		r = gameObject.GetComponentInChildren<ISkillRunner>();
	}

    void Bind(SkillBinding b)
    {
		if (b.mechanism is not ISkillMechanic mech || b.param == null) return;
		if (!mech.ParamType.IsInstanceOfType(b.param))
		{
			Debug.LogError($"Param mismatch: need {mech.ParamType.Name}, got {b.param.GetType().Name}"); return;
		}
		if (r == null) { Debug.LogError($"No ISkillRunner found in children of {gameObject.name}"); return; }
		slots.Add(b.slot);
	}

	void OnEnable()
	{
		foreach (var key in keys)
		{
			if (key)
			{
				//Is it unsubscribed without any following code? This is awkward, but it seems true
				key.action.performed += _ =>
				{
					Debug.Log(keys.IndexOf(key));
					TryCast(slots[keys.IndexOf(key)]);
				};
				//Well, InputSystem seems to handle the unsubscription well
				//That's good, but that's not how C# events work... Unity, are you doing some magic tricks?
				//Perhaps it is because InputAction is not a standard C# event, but a UnityEvent
				//Well let's leave it as is for now since it does their job
			}
		}

	}
	void TryCast(SkillSlot slot)
	{
		{
			//Here we should Create Intent and pass it to the runner
			//No Direct TryCast allowed, All the skills should be activated through catching Intent
			//There we can use FollowUps as general system; These are almost the same but with auto activation
			//FollowUps for chaining skills with no exception for those(TryFollowUp is the worst idea ever)
			//But how do you catch Intent? Event System? May there be some other way? Let us see
			//But for now we'll make just a log here, not to break anything
			Debug.Log($"Intent to cast {slot} has generated");
		}
	}
}
