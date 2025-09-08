using UnityEngine;
using CharacterSOInterfaces;

public enum SkillSlot { Attack, Skill1, Skill2, Skill3, Ultimate }

[CreateAssetMenu(menuName = "Game/PlayerCharacter Spec")]
public class PlayerCharacterSpec : ScriptableObject
{
    [System.Serializable]
    public struct SkillBinding
    {
        public SkillSlot slot;
        public ScriptableObject mechanic;           // ISkillMechanic ���� SO
        [SerializeReference] public ISkillParam param; // ���� �Ķ����(MeleeParams ��)
    }

    public string displayName = "Dummy";
    public float baseMaxHP = 100f, baseAttack = 10f, baseDefense = 2f;

    public SkillBinding attack;
    public SkillBinding skill1;
    // �ʿ� �� skill2/3/ult �߰�
}