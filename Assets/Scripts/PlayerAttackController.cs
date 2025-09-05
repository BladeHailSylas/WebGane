using UnityEngine;
using System.Collections.Generic;
using CharacterSOInterfaces;
// 입력은 기존 프로젝트 흐름에 맞춰 바인딩(여기선 메서드 호출 예시로 단순화)

public class PlayerAttackController : MonoBehaviour
{
    [Header("Character")]
    public Character1 character; // 캐릭터 SO (인스펙터에서 할당)  :contentReference[oaicite:13]{index=13}

    // 슬롯별 실행체 보관
    private readonly Dictionary<SkillSlot, ISkillRunner> runners = new();

    void Awake()
    {
        // 캐릭터 SO에서 스킬 매핑 읽어 실행체 생성
        foreach (var sb in character.skills)
        {
            if (sb.specAsset is not ISkillSpec spec)
            {
                Debug.LogError($"Skill spec이 ISkillSpec가 아님: {sb.specAsset}");
                continue;
            }
            var runner = spec.Bind(gameObject); // 실행체를 현재 플레이어에 부착
            if (runner != null) runners[sb.slot] = runner;
        }
    }

    // 예시 입력 메서드(기존 Input System에서 호출해도 됨)
    public void OnPrimary() { if (runners.TryGetValue(SkillSlot.Primary, out var r)) r.TryCast(); }
    public void OnSkill1() { if (runners.TryGetValue(SkillSlot.Skill1, out var r)) r.TryCast(); }
    public void OnSkill2() { if (runners.TryGetValue(SkillSlot.Skill2, out var r)) r.TryCast(); }
}