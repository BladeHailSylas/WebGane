using UnityEngine;
using Generals;

[CreateAssetMenu(fileName = "Character1", menuName = "Scriptable Objects/Character1")]
public class Character1 : ScriptableObject//, IPlayerCharacter -> 캐릭터 설계도, 다른 객체에서 그냥 가져다 쓸 수 있게 만들어야 됨(다른 객체에 의존 X)
{
    public string displayName;
    public GameObject prefab;          // 그 캐릭터 프리팹
    public float baseMaxHP = 100f;
    public float baseAttack = 12f;
    public float baseDefense = 3f;
    public void SkillQ()
    {
        Debug.Log("CaptainQ");
    }
}