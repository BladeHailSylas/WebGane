using UnityEngine;
using CharacterSOInterfaces;

[CreateAssetMenu(menuName = "Scriptable Objects/Character1")]
public class Character1 : ScriptableObject
{
    [Header("Info")]
    [SerializeField] private string displayName = "Character1";
    [SerializeField] private float baseMaxHP = 100f;
    [SerializeField] private float baseAttackDamage = 12f;
    [SerializeField] private float baseDefense = 3f;

    public string DisplayName => displayName;
    public float BaseMaxHP => baseMaxHP;
    public float BaseAttackDamage => baseAttackDamage;
    public float BaseDefense => baseDefense;

    [System.Serializable]
    public struct SkillBinding
    {
        public SkillSlot slot;
        public ScriptableObject mechanic;              // ISkillMechanic
        [SerializeReference] public ISkillParam param; // ���� �Ķ���� (MeleeParams ��)
    }

    [Header("Skills")]
    public SkillBinding[] skills;
}