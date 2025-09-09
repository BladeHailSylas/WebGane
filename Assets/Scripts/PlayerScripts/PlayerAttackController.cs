// PlayerAttackController.cs  (교체)
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using SOInterfaces;

public class PlayerAttackController : MonoBehaviour
{
    [Header("Character")]
    [SerializeField] PlayerCharacterSpec characterSpec; // 캐릭터 SO (슬롯→메커니즘+파라미터)
    public PlayerCharacterSpec Spec { get { return characterSpec; } set { characterSpec = value; } }

    [Header("Input")]
    public InputActionReference attackKey;   // LClick 등
    public InputActionReference skill1Key;   // Q 등
    public InputActionReference skill2Key;   // E 등 (필요시)

    private readonly Dictionary<SkillSlot, ISkillRunner> _runners = new();

    void Awake()
    {
        _runners.Clear();
        BindSlot(characterSpec.attack);
        BindSlot(characterSpec.skill1);
        // 필요 시 skill2/skill3도 동일 패턴
    }

    /*void OnEnable()
    {
        if (attackKey) { attackKey.action.Enable(); attackKey.action.performed += OnAttack; }
        if (skill1Key) { skill1Key.action.Enable(); skill1Key.action.performed += OnSkill1; }
        if (skill2Key) { skill2Key.action.Enable(); skill2Key.action.performed += OnSkill2; }
    }

    void OnDisable()
    {
        if (attackKey) attackKey.action.performed -= OnAttack;
        if (skill1Key) skill1Key.action.performed -= OnSkill1;
        if (skill2Key) skill2Key.action.performed -= OnSkill2;

        attackKey?.action.Disable();
        skill1Key?.action.Disable();
        skill2Key?.action.Disable();
    }*/
    public void OnAttack(InputAction.CallbackContext _) { TryCast(SkillSlot.Attack); }
    public void OnSkill1(InputAction.CallbackContext _) { TryCast(SkillSlot.Skill1); }
    public void OnSkill2(InputAction.CallbackContext _) { TryCast(SkillSlot.Skill2); }

    void TryCast(SkillSlot slot)
    {
        if (_runners.TryGetValue(slot, out var r)) r.TryCast();
    }

    void BindSlot(PlayerCharacterSpec.SkillBinding b)
    {
        if (b.mechanic is not ISkillMechanic mech || b.param == null) return;
        if (!mech.ParamType.IsInstanceOfType(b.param))
        {
            Debug.LogError($"Param 타입 불일치: need {mech.ParamType.Name}, got {b.param.GetType().Name}");
            return;
        }

        var runner = gameObject.AddComponent<SkillRunner>(); // ← 공용 Runner
        runner.Init(mech, b.param);
        _runners[b.slot] = runner;
    }
}
