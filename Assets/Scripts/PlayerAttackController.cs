using UnityEngine;
using System.Collections.Generic;
using CharacterSOInterfaces;
// �Է��� ���� ������Ʈ �帧�� ���� ���ε�(���⼱ �޼��� ȣ�� ���÷� �ܼ�ȭ)

public class PlayerAttackController : MonoBehaviour
{
    [Header("Character")]
    public Character1 character; // ĳ���� SO (�ν����Ϳ��� �Ҵ�)  :contentReference[oaicite:13]{index=13}

    // ���Ժ� ����ü ����
    private readonly Dictionary<SkillSlot, ISkillRunner> runners = new();

    void Awake()
    {
        // ĳ���� SO���� ��ų ���� �о� ����ü ����
        foreach (var sb in character.skills)
        {
            if (sb.specAsset is not ISkillSpec spec)
            {
                Debug.LogError($"Skill spec�� ISkillSpec�� �ƴ�: {sb.specAsset}");
                continue;
            }
            var runner = spec.Bind(gameObject); // ����ü�� ���� �÷��̾ ����
            if (runner != null) runners[sb.slot] = runner;
        }
    }

    // ���� �Է� �޼���(���� Input System���� ȣ���ص� ��)
    public void OnPrimary() { if (runners.TryGetValue(SkillSlot.Primary, out var r)) r.TryCast(); }
    public void OnSkill1() { if (runners.TryGetValue(SkillSlot.Skill1, out var r)) r.TryCast(); }
    public void OnSkill2() { if (runners.TryGetValue(SkillSlot.Skill2, out var r)) r.TryCast(); }
}