using UnityEngine;
using SOInterfaces;

public enum SkillSlot { Attack, Skill1, Skill2, Skill3, Ultimate }

[CreateAssetMenu(menuName = "Game/PlayerCharacterSpec")]
public class PlayerCharacterSpec : ScriptableObject
{
    [System.Serializable]
    public struct SkillBinding
    {
        public SkillSlot slot;
        public ScriptableObject mechanic;           // ISkillMechanic 구현 SO
        [SerializeReference] public ISkillParam param; // 전용 파라미터(MeleeParams 등)
    }

    public string displayName = "CharacterTester";
    public float baseMaxHP = 100f, baseAttack = 10f, baseDefense = 2f;

    public SkillBinding attack;
    public SkillBinding skill1;
    // 필요 시 skill2/3/ult 추가
}