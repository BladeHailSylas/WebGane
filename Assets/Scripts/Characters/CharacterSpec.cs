using SOInterfaces;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Characters/Spec")]
public class CharacterSpec : ScriptableObject
{
    public string displayName;
    public float baseHP, baseHPGen, baseDR, baseAttack, baseDefense;

    [System.Serializable]
    public struct SkillBinding
    {
        public SkillSlot slot;
        public ScriptableObject mechanic;
        [SerializeReference] public ISkillParam param;
    }
    public SkillBinding attack, skill1, skill2, ultimate;
}