// PlayerAttackController.cs  (��ü)
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using SkillInterfaces;

public class PlayerAttackController : MonoBehaviour
{
    [Header("Character")]
    public CharacterSpec spec; // ĳ���� SO (���ԡ��Ŀ����+�Ķ����)
    //public ICharacter Spec { get { return spec; } set { spec = value; } }

    [Header("Input")]
    public InputActionReference attackKey;   // LMB(+Passive)
    public InputActionReference skill1Key;   // RMB
    public InputActionReference skill2Key;   // SHIFT
    public InputActionReference ultimateKey; // SPACE
                                            //���� LMB, Skill1 Shift, Skill2 Space, Ultimate RMB?
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
        if (ultimateKey) { ultimateKey.action.Enable(); ultimateKey.action.performed += _ => TryCast(SkillSlot.Ultimate); }
    }
    void OnDisable()
    {
        if (attackKey) attackKey.action.performed -= _ => TryCast(SkillSlot.Attack);
        if (skill1Key) skill1Key.action.performed -= _ => TryCast(SkillSlot.Skill1);
        if (skill2Key) skill2Key.action.performed -= _ => TryCast(SkillSlot.Skill2);
        if (ultimateKey) ultimateKey.action.performed -= _ => TryCast(SkillSlot.Ultimate);
        attackKey?.action.Disable(); skill1Key?.action.Disable(); skill2Key?.action.Disable(); ultimateKey?.action.Disable(); 
    }

    void TryCast(SkillSlot slot) { if (runners.TryGetValue(slot, out var r)) r.TryCast(); }
}
