// PlayerAttackController.cs  (��ü)
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using SOInterfaces;

public class PlayerAttackController : MonoBehaviour
{
    [Header("Character")]
    [SerializeField] PlayerCharacterSpec characterSpec; // ĳ���� SO (���ԡ��Ŀ����+�Ķ����)
    public PlayerCharacterSpec Spec { get { return characterSpec; } set { characterSpec = value; } }

    [Header("Input")]
    public InputActionReference attackKey;   // LClick ��
    public InputActionReference skill1Key;   // Q ��
    public InputActionReference skill2Key;   // E �� (�ʿ��)

    private readonly Dictionary<SkillSlot, ISkillRunner> _runners = new();

    void Awake()
    {
        _runners.Clear();
        BindSlot(characterSpec.attack);
        BindSlot(characterSpec.skill1);
        // �ʿ� �� skill2/skill3�� ���� ����
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
            Debug.LogError($"Param Ÿ�� ����ġ: need {mech.ParamType.Name}, got {b.param.GetType().Name}");
            return;
        }

        var runner = gameObject.AddComponent<SkillRunner>(); // �� ���� Runner
        runner.Init(mech, b.param);
        _runners[b.slot] = runner;
    }
}
