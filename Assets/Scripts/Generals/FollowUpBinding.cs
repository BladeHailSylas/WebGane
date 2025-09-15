using System;
using UnityEngine;
using SOInterfaces; // ISkillMechanic, ISkillParam
using ActInterfaces; // ITargetable (Runner가 사용)

    /// <summary>
    /// "무엇을 어떻게 이어서 시전할지"를 데이터로 표현
    /// </summary>
    [Serializable]
    public struct FollowUpBinding
    {
        public ScriptableObject mechanic;             // ISkillMechanic 여야 함
        [SerializeReference] public ISkillParam param;

        [Min(0)] public float delay;                  // 시전 전 지연
        public bool passSameTarget;                   // 이전 타겟 전달?
        public bool respectBusyCooldown;              // Runner busy/cd 존중?
    public readonly bool IsValid(out ISkillMechanic next)
        {
            next = mechanic as ISkillMechanic;
            return next != null && param != null && next.ParamType.IsInstanceOfType(param);
        }
    public readonly bool IsValid()
    {
        return mechanic is ISkillMechanic next &&
                param != null &&
                next.ParamType.IsInstanceOfType(param);
    }
}
