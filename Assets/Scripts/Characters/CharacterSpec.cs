using SOInterfaces;
using UnityEngine;
[System.Serializable]
public struct SkillBinding
{
    public SkillSlot slot;
    public ScriptableObject mechanic;
    [SerializeReference] public ISkillParam param;
    public FollowUpBinding onCastStart;  // ���� ���� ��
    public FollowUpBinding onHit;        // ���� ��
    public FollowUpBinding onExpire;     // �Ҹ�/���� ��
}
// 1) �޺�(�ļ� ȣ��) ���ε�: ĳ���ͺ��� �ٸ� �Ķ���͸� SerializeReference�� ����
/*[System.Serializable]
public struct FollowUpBinding
{
    public ScriptableObject mechanic;             // ISkillMechanic ���� ��
    [SerializeReference] public ISkillParam param;
    [Min(0)] public float delay;
    public bool passSameTarget;                   // ���� Ÿ�� ���(Transform)�� ��������
    public bool respectBusyCooldown;              // true�� runner busy/cd ����, false�� ����

    public readonly bool IsValid(ISkillMechanic next) =>
        next != null && param != null && next.ParamType.IsInstanceOfType(param);
}*/
[CreateAssetMenu(menuName = "Game/Characters/Spec")]
/// ĳ���� �ּ��� ����� ���� SO
/// ��� �������ͽ��� ���� ���� Inspector�� ���� ���� �Ҵ�
/// �⺻ �������ͽ�(base)�� ������, ���� �������ͽ�(current)�� PlayerStats�� ����ؼ� ���
public class CharacterSpec : ScriptableObject
{
    public string displayName;
    public float baseHP, baseHPGen, baseMana, baseManaGen, baseAttack, baseDefense, baseSpeed;
    public SkillBinding attack, skill1, skill2, ultimate;
}