using UnityEngine;

[CreateAssetMenu(menuName = "Game/PlayerCharacter Spec (No Prefab)", fileName = "PlayerCharacterSpec_Dummy")]
public class PlayerCharacterSpec : ScriptableObject
{
    [Header("Basic Info")]
    public string displayName = "Dummy";

    [Header("Base Stats (Dummy)")]
    public float baseMaxHP = 100f;
    public float baseAttack = 10f;
    public float baseDefense = 2f;

    // 나중에 필요하면 스킬 데이터, 아이콘 등 확장 가능
}