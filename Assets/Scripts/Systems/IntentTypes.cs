using SkillInterfaces;

/// <summary>
///     Enumerates every supported intent type that can travel through the deterministic
///     command pipeline. Additional types must be explicitly routed by the <see cref="IntentRouter"/>.
/// </summary>
public enum IntentType
{
    None,
    Move,
    Cast,
}

/// <summary>
///     Common metadata shared by every intent dispatched through the core systems.
/// </summary>
public interface IIntent
{
    ushort OwnerID { get; }
    int IntentID { get; }
    IntentType Type { get; }
    ushort GeneratedTick { get; }
}

/// <summary>
///     Small immutable container that bundles the mechanism and parameter pair used by
///     the casting pipeline.
/// </summary>
public readonly struct SkillInfo
{
    public ISkillMechanism Mechanism { get; }
    public ISkillParam Param { get; }

    public SkillInfo(ISkillMechanism mechanism, ISkillParam param)
    {
        Mechanism = mechanism;
        Param = param;
    }
}

/// <summary>
///     Declarative intent describing how an actor wishes to move during the next tick.
///     The payload is designed to be compact and deterministic.
/// </summary>
public struct MoveIntent : IIntent
{
    public ushort OwnerID { get; }
    public int IntentID { get; }
    public IntentType Type { get; }
    public ushort GeneratedTick { get; }
    public FixedVector2 Movement { get; private set; }
    public ushort MoverID { get; }

    public MoveIntent(ushort ownerID, int intentID, ushort generatedTick, FixedVector2 movement, ushort moverID = 0)
    {
        OwnerID = ownerID;
        IntentID = intentID;
        Type = IntentType.Move;
        GeneratedTick = generatedTick;
        Movement = movement;
        MoverID = moverID;
        if(moverID == 0) moverID = OwnerID;
    }
}

/// <summary>
///     Intent that requests a skill cast. In addition to the base metadata, it transports
///     the target information and a <see cref="SkillInfo"/> payload describing which
///     mechanism should be executed.
/// </summary>
public sealed class CastIntent : IIntent
{
    public ushort OwnerID { get; }
    public int IntentID { get; }
    public IntentType Type => IntentType.Cast;
    public ushort GeneratedTick { get; }
    public ushort TargetID { get; }
    public FixedVector2 TargetPosition { get; }
    public SkillInfo Skill { get; }

    public CastIntent(
        ushort ownerID,
        int intentID,
        ushort generatedTick,
        ushort targetID,
        FixedVector2 targetPosition,
        SkillInfo skill)
    {
        OwnerID = ownerID;
        IntentID = intentID;
        GeneratedTick = generatedTick;
        TargetID = targetID;
        TargetPosition = targetPosition;
        Skill = skill;
    }

    /** Optional debug hook: expose additional diagnostic strings when integrating a telemetry layer. */
}
