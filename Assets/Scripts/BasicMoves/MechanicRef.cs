using SOInterfaces;
using System;
using UnityEngine;

[Serializable]
public struct MechanicRef
{
    public ScriptableObject mechanic;             // ISkillMechanic
    [SerializeReference] public ISkillParam param; // �� �ܰ躰 �Ķ����

    public readonly bool TryGet(out ISkillMechanic next)
    {
        next = mechanic as ISkillMechanic;
        return next != null && param != null && next.ParamType.IsInstanceOfType(param);
    }
}