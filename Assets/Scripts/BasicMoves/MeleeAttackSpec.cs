using UnityEngine;
using CharacterSOInterfaces;

[CreateAssetMenu(menuName = "Game/Skills/Melee Attack")]
public class MeleeAttackSpec : ScriptableObject, ISkillSpec
{
    [Header("UI")]
    public string displayName = "Melee";

    [Header("Timing")]
    public float windup = 0.06f;
    public float active = 0.08f;
    public float recover = 0.10f;
    public float cooldown = 0.08f;

    [Header("Swing")]
    public float swingAngle = 120f;

    [Header("Hitbox Spec (설계도)")]
    public ScriptableObject hitboxSpecAsset; // IHitboxSpec 구현 SO (예: Character1Hitbox)

    public string DisplayName => displayName;
    public float Cooldown => cooldown;

    public ISkillRunner Bind(GameObject owner)
    {
        var spec = hitboxSpecAsset as IHitboxSpec;
        if (spec == null)
        {
            Debug.LogError($"[MeleeAttackSpec] hitboxSpecAsset가 IHitboxSpec가 아닙니다: {hitboxSpecAsset}");
            return null;
        }
        var r = owner.AddComponent<MeleeAttackRunner>();
        r.Init(owner.transform, this, spec);
        return r;
    }
}