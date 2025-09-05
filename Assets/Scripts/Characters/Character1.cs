using UnityEngine;
using CharacterSOInterfaces;
using UnityEditor;

// 입력 슬롯(원하는 대로 확장)
public enum SkillSlot { Primary, Skill1, Skill2 }

[CreateAssetMenu(fileName = "Character1", menuName = "Scriptable Objects/Character1")]
public class Character1 : ScriptableObject
{
    public string displayName;
    [SerializeField] private GameObject prefab;
    [SerializeField] private float baseMaxHP = 100f;
    [SerializeField] private float baseAttackDamage = 12f;
    [SerializeField] private float baseDefense = 3f;

    public GameObject Prefab => prefab;

    [System.Serializable]
    public struct SkillBinding
    {
        public SkillSlot slot;
        public ScriptableObject specAsset; // 반드시 ISkillSpec 구현 SO
    }
    [Header("Skills")]
    public SkillBinding[] skills;
}
public class Character1Hitbox : ScriptableObject, IHitboxSpec
{
    [Header("Shape")]
    [SerializeField] HitboxShape2D shape = HitboxShape2D.Box;
    [SerializeField] Vector2 size = new(1.2f, 0.3f);
    [SerializeField] CapsuleDirection2D capsuleDirection = CapsuleDirection2D.Horizontal;
    [SerializeField] Vector2 localOffset = new(0.8f, 0f);

    [Header("Combat")]
    [SerializeField] float damage = 12f;
    [SerializeField] float apRatio = 0f;
    [SerializeField] float knockback = 7f;
    [SerializeField] LayerMask enemyMask;

    [Header("Lifecycle")]
    [SerializeField] float activeTime = 0.08f;
    [SerializeField] string hitboxLayerName = "Hitbox";

    // 읽기 전용 노출
    public HitboxShape2D Shape => shape;
    public Vector2 Size => size;
    public CapsuleDirection2D CapsuleDirection => capsuleDirection;
    public Vector2 LocalOffset => localOffset;
    public float Damage => damage;
    public float ApRatio => apRatio;
    public float Knockback => knockback;
    public LayerMask EnemyMask => enemyMask;
    public float ActiveTime => activeTime;
    public string HitboxLayerName => hitboxLayerName;
}