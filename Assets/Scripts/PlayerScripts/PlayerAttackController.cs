// PlayerAttackController.cs  (교체)
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using SOInterfaces;

public class PlayerAttackController : MonoBehaviour
{
    [Header("Character")]
    public CharacterSpec spec; // 캐릭터 SO (슬롯→메커니즘+파라미터)
    //public ICharacter Spec { get { return spec; } set { spec = value; } }

    [Header("Input")]
    public InputActionReference attackKey;   // LMB 등
    public InputActionReference skill1Key;   // RMB
    public InputActionReference skill2Key;   // SHIFT
    public InputActionReference ultimateKey; // SPACE

    private readonly Dictionary<SkillSlot, ISkillRunner> _runners = new();

    readonly Dictionary<SkillSlot, ISkillRunner> runners = new();

    void Awake()
    {
        Bind(spec.attack);
        Bind(spec.skill1);
        Bind(spec.skill2);
    }

    void Bind(SkillBinding b)
    {
        if (b.mechanic is not ISkillMechanic mech || b.param == null) return;
        if (!mech.ParamType.IsInstanceOfType(b.param))
        {
            Debug.LogError($"Param mismatch: need {mech.ParamType.Name}, got {b.param.GetType().Name}"); return;
        }
        var r = gameObject.AddComponent<SkillRunner>();
        r.Init(mech, b.param);
        runners[b.slot] = r;
    }

    void OnEnable()
    {
        if (attackKey) { attackKey.action.Enable(); attackKey.action.performed += _ => TryCast(SkillSlot.Attack); }
        if (skill1Key) { skill1Key.action.Enable(); skill1Key.action.performed += _ => TryCast(SkillSlot.Skill1); }
        if (skill2Key) { skill2Key.action.Enable(); skill2Key.action.performed += _ => TryCast(SkillSlot.Skill2); }
    }
    void OnDisable()
    {
        if (attackKey) attackKey.action.performed -= _ => TryCast(SkillSlot.Attack);
        if (skill1Key) skill1Key.action.performed -= _ => TryCast(SkillSlot.Skill1);
        if (skill2Key) skill2Key.action.performed -= _ => TryCast(SkillSlot.Skill2);
        attackKey?.action.Disable(); skill1Key?.action.Disable();
    }

    void TryCast(SkillSlot slot) { if (runners.TryGetValue(slot, out var r)) r.TryCast(); }
}
