using System;
using UnityEngine;
using SOInterfaces; // ISkillMechanic, ISkillParam
using ActInterfaces; // ITargetable (Runner�� ���)

    /// <summary>
    /// "������ ��� �̾ ��������"�� �����ͷ� ǥ��
    /// </summary>
    [Serializable]
    public struct FollowUpBinding
    {
        public ScriptableObject mechanic;             // ISkillMechanic ���� ��
        [SerializeReference] public ISkillParam param;

        [Min(0)] public float delay;                  // ���� �� ����
        public bool passSameTarget;                   // ���� Ÿ�� ����?
        public bool respectBusyCooldown;              // Runner busy/cd ����?
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
