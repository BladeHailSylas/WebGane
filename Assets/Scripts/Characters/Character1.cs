using UnityEngine;
using Generals;

[CreateAssetMenu(fileName = "Character1", menuName = "Scriptable Objects/Character1")]
public class Character1 : ScriptableObject//, IPlayerCharacter -> ĳ���� ���赵, �ٸ� ��ü���� �׳� ������ �� �� �ְ� ������ ��(�ٸ� ��ü�� ���� X)
{
    public string displayName;
    public GameObject prefab;          // �� ĳ���� ������
    public float baseMaxHP = 100f;
    public float baseAttack = 12f;
    public float baseDefense = 3f;
    public void SkillQ()
    {
        Debug.Log("CaptainQ");
    }
}