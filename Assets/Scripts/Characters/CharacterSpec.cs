using SOInterfaces;
using UnityEngine;
[System.Serializable]
public struct SkillBinding
{
    public SkillSlot slot;
    public ScriptableObject mechanic;
    [SerializeReference] public ISkillParam param;
    public FollowUpBinding onCastStart;  // 시전 시작 시
    public FollowUpBinding onHit;        // 적중 시
    public FollowUpBinding onExpire;     // 소멸/종료 시
}
// 1) 콤보(후속 호출) 바인딩: 캐릭터별로 다른 파라미터를 SerializeReference로 저장
/*[System.Serializable]
public struct FollowUpBinding
{
    public ScriptableObject mechanic;             // ISkillMechanic 여야 함
    [SerializeReference] public ISkillParam param;
    [Min(0)] public float delay;
    public bool passSameTarget;                   // 이전 타격 대상(Transform)을 전달할지
    public bool respectBusyCooldown;              // true면 runner busy/cd 존중, false면 강제

    public readonly bool IsValid(ISkillMechanic next) =>
        next != null && param != null && next.ParamType.IsInstanceOfType(param);
}*/
[CreateAssetMenu(menuName = "Game/Characters/Spec")]
/// 캐릭터 애셋을 만들기 위한 SO
/// 기술 스테이터스와 같은 것은 Inspector를 통해 직접 할당
/// 기본 스테이터스(base)만 가지고, 실제 스테이터스(current)는 PlayerStats가 계산해서 사용
public class CharacterSpec : ScriptableObject
{
    public string displayName;
    public float baseHP, baseHPGen, baseMana, baseManaGen, baseAttack, baseDefense, baseSpeed;
    public SkillBinding attack, skill1, skill2, ultimate;
}