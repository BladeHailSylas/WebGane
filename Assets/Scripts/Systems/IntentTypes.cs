public struct MoveIntent : IIntent
{
    public ushort OwnerID { get; }
    public int IntentID { get; }
    public IntentType Type { get; }
    public ushort GeneratedTick { get; }
    public FixedVector2 Movement { get; private set; }

    public ushort MoverID { get; }

    public MoveIntent(ushort ownerID, int intentID, ushort generatedTick, FixedVector2 movement, ushort moverID)
    {
        OwnerID = ownerID;
        IntentID = intentID;
        Type = IntentType.Move;
        GeneratedTick = generatedTick;
        Movement = movement;
        MoverID = moverID;
    }
}

public struct SkillIntent
{
        
}
//IntentOrchestrator를 IntentRouter로 변환하고 기능을 축소한다, CastIntent도 전면 폐기하고 Intent 파이프라인을 간결하게 만든다
public enum IntentType
{
    None, Move, Cast,
}
public interface IIntent
{
    public ushort OwnerID { get; }
    public int IntentID { get; }
    public IntentType Type { get; }
    public ushort GeneratedTick { get; }
}