// EventPayloads.cs
// ����: Ÿ�� ��� EventBus<T>�� ���� "������ �� Ÿ�� ���̷ε�" ���� ����
// ��� ��:
//   EventBus.Subscribe<DamageDealt>(OnDamage); // ����
//   EventBus.Publish(new DamageDealt(meta, attacker, target, raw, final, EDamageType.Normal, isCrit, hitPt, hitNrm)); // ����
//
// ���� ��Ģ:
// 1) �� Ÿ��(readonly struct)���� ������ ������ �ּ�ȭ.
// 2) "��û(Request)"�� "��� �˸�(Event)"�� ��Ȯ�� ����.
// 3) Unity ���� ���: Transform/Object ������ ����/���� ������ �����ϰ�, �ʿ� �� Id/Meta�� ���� ����.
// 4) Ÿ���� �� ä��: EventBus<T> Ű�� T Ÿ���̹Ƿ�, ����/������ ������ Ÿ�� �ñ״�ó�� ����.
// �� �� ���� ��:
// �÷��̾� ĳ���� ���� �ÿ��� event�� ����ؾ� �ϴ��� �𸣰���
// �������ͽ� 
#nullable enable
using SOInterfaces;
using UnityEngine;

#region ===== ���� ��Ÿ������ =====

/// <summary>
/// ��� ���� �̺�Ʈ�� �������� ���Ե� �� �ִ� ��Ÿ ����.
/// - EventId: ���� ���� ������(�α�/Ʈ���̽� ����)
/// - Time: �̺�Ʈ �߻� �ð�(Time.time ������)
/// - Source: �߽���(�ַ� Transform ��Ʈ, null ����)
/// - Channel: �� ä��("combat", "buff", "ui" ��) - ����/����� ��
/// - CorrelationId: ����~�߰�~���� ���� �帧�� ���� ��� �ĺ���
/// </summary>
public readonly struct GameEventMeta
{
    public readonly int EventId;
    public readonly float Time;
    public readonly Transform Source;
    public readonly string? Channel;
    public readonly int CorrelationId;

    public GameEventMeta(int eventId, float time, Transform source, string? channel, int correlationId)
    {
        EventId = eventId;
        Time = time;
        Source = source;
        Channel = channel;
        CorrelationId = correlationId;
    }

    public override string ToString()
        => $"#{EventId} t={Time:F3} src={(Source ? Source.name : null)} ch={Channel ?? null} corr={CorrelationId}";
}

/// <summary>
/// GameEventMeta ������ ǥ��ȭ�ϴ� ���� ���丮.
/// - �ϰ��� ���� ������/�ð� �������� �ο�.
/// - <see cref="Create"/> �ϳ��� ���� ��Ÿ�� ���������ϴ�.
/// </summary>
public static class GameEventMetaFactory
{
    private static int _seq;

    /// <summary>ǥ�� ��Ÿ ����. channel/correlationId�� �ʿ� �ÿ��� ����.</summary>
    public static GameEventMeta Create(Transform source, string? channel = null, int correlationId = 0)
        => new(++_seq, Time.time, source, channel, correlationId);
}

#endregion

#region ===== ����: ��ų ���� =====

/// <summary>
/// ��ų ������ ���� �淮 ����.
/// - Asset: ScriptableObject(��ī��/��ų ������) ����. ���� �� ����.
/// - Id: ������ �ĺ���(��Ʈ��ŷ/���̺�/ǥ��ȭ ��).
/// �� �� �ϳ��� �ᵵ �ǰ�, �� �� �� ���� �ֽ��ϴ�.
/// </summary>
public readonly struct SkillRef
{
    public readonly Object? Asset; // ScriptableObject(�Ǵ� ��Ÿ UnityEngine.Object)
    public readonly int Id;                    // �ܺ� �ý��۰��� ȣȯ�� ���� ������ Ű(0�̸� �̻������ ����)

    public bool HasAsset => Asset != null;
    public bool HasId => Id != 0;

    public SkillRef(Object? asset, int id = 0)
    { Asset = asset; Id = id; }

    public override string ToString()
        => HasAsset ? $"{Asset!.name}(#{Id})" : $"SkillId#{Id}";
}

#endregion

#region ===== ����(ĳ����) �帧: ��� �˸�(Event) =====

/// <summary>
/// [Event] ���� ���� �˸�.
/// - EventBus.Subscribe&lt;CastStarted&gt;(...) �� ����.
/// - �����ڴ� "���"�� �˸���, UI/VFX/����� �̸� ������ �����մϴ�.
/// </summary>
public readonly struct CastStarted
{
    public readonly GameEventMeta Meta;
    public readonly SkillRef Skill;       // ��Ÿ�� ����(�Ǵ� Id)
    public readonly ISkillParam Param;    // ���� ������ �Ķ���� ������(�б� ���� ���)
    public readonly Transform Caster;     // ���ǻ� ���� ����
    public readonly Transform? TargetOpt; // ����� ��ų�� ���� ����(������ null)

    public CastStarted(GameEventMeta meta, SkillRef skill, ISkillParam param, Transform caster, Transform? targetOpt)
    { Meta = meta; Skill = skill; Param = param; Caster = caster; TargetOpt = targetOpt; }

    public override string ToString() => $"CastStarted[{Skill}] by {Caster.name}";
}

/// <summary>
/// [Event] ���� ���� �˸�.
/// - Interrupted: ���/�ǰ�/CC � ���� �ߴ� ����.
/// </summary>
public readonly struct CastEnded
{
    public readonly GameEventMeta Meta;
    public readonly SkillRef Skill;
    public readonly Transform Caster;
    public readonly bool Interrupted;

    public CastEnded(GameEventMeta meta, SkillRef skill, Transform caster, bool interrupted)
    { Meta = meta; Skill = skill; Caster = caster; Interrupted = interrupted; }

    public override string ToString() => $"CastEnded[{Skill}] by {Caster.name}, interrupted={Interrupted}";
}

/// <summary>
/// [Event] Ÿ�� Ȯ�� �˸�, 
/// </summary>
public readonly struct TargetAcquired
{
    public readonly GameEventMeta Meta;
    public readonly SkillRef Skill;
    public readonly Transform Caster;
    public readonly Transform Target;
    public TargetAcquired(GameEventMeta meta, SkillRef skill, Transform caster, Transform target)
    {
        Meta = meta; Skill = skill; Caster = caster; Target = target;
    }
    public override string ToString() => $"TargetAcquired[{Target.name}] by {Caster.name} to activate {Skill}";
}
public readonly struct TargetNotFound
{
    public readonly GameEventMeta Meta;
    public readonly SkillRef Skill;
    public readonly Transform Caster;
    public TargetNotFound(GameEventMeta meta, SkillRef skill, Transform caster)
    {
        Meta = meta; Skill = skill; Caster = caster;
    }
    public override string ToString() => $"TargetNotFound by {Caster.name} to activate {Skill}";
}

#endregion

#region ===== ����: ��� �˸�(Event) =====

/// <summary>
/// [Event] ���ذ� "�����" ��� �˸�(�α�/����/UI��).
/// - RawDamage: �⺻ ���ط�
/// - FinalDamage: ���� ���ϴ� ���ط�(���ط� ������ ȿ���� �޴�)
/// - ArmorPenetration: �� ��� ���� ���(������, <see cref="PlayerStats.TotalArmorPenetration()"/> ����)
/// - DamageType(<see cref="EDamageType"/>): �Ϲ�(Normal)/���(Percentaged)/����(Fixed)
/// - HitPoint/HitNormal: ����/�˹� ���� ��� �ٰ�
/// </summary>
public readonly struct DamageDealt
{
    public readonly GameEventMeta Meta;
    public readonly Transform Attacker;
    public readonly Transform Target;
    public readonly float RawDamage;
    public readonly float FinalDamage;
    public readonly float ArmorPenetration;
    public readonly EDamageType DamageType;
    public readonly Vector2 HitPoint;
    public readonly Vector2 HitNormal;

    public DamageDealt(
        GameEventMeta meta, Transform attacker, Transform target,
        float rawDamage, float finalDamage, float armorPenetration, EDamageType damageType,
        Vector2 hitPoint, Vector2 hitNormal)
    {
        Meta = meta;
        Attacker = attacker;
        Target = target;
        RawDamage = rawDamage;
        FinalDamage = finalDamage;
        ArmorPenetration = armorPenetration;
        DamageType = damageType;
        HitPoint = hitPoint;
        HitNormal = hitNormal;
    }

    public override string ToString()
        => $"DamageDealt {FinalDamage} ({DamageType}) {Attacker.name}��{Target.name}";
}

/// <summary>���� ����: �Ϲ� ���� Normal, ��� ���� Percentaged(�ִ� ü���� 1% ��), ���� ���� Fixed(���ط� ������ ����, �װ��� �����̴ϱ� ��)</summary>
public enum EDamageType { Normal, Percentaged, Fixed }

#endregion

#region ===== ����/����Ʈ: ��û(Request) & ����(Remove) =====
// EventBus���� "��û" ���̷ε�� �ý���(���� �Ŵ��� ��)���� ������ �Ƿ��մϴ�.
// - ApplyReq: ���� ��û(����/���� ���δ� ���� ���� �̺�Ʈ�� �����ص� ��)
// - RemoveReq: ���� ��û(���Ŵ� PlayerEffects���� ���� �����ص� ���� �𸣰���)
// �� ��û�� "��� �˸�"�� ���� ���� �и��ϸ� �帧 ������ �����ϴ�.

/// <summary>
/// [Request] ����(����) ���� ��û.
/// - Target: �������� ����?
/// - Mod: � ���� ����?
/// - Duration: float.PositiveInfinity�� ��쿡�� ����(������ ������ ����� ����)
/// - Tag: �ߺ� ��å�� ���� �±�(�ɼ�; ���� �±״� ����/��ü �� ��å�� Ȱ��)
/// </summary>
public readonly struct BuffApplyReq
{
    public readonly GameEventMeta Meta;
    public readonly Transform Target;
    public readonly IStatModifier Mod;
    public readonly float Duration;
    public readonly string? Tag;

    public BuffApplyReq(GameEventMeta meta, Transform target, IStatModifier mod, float duration, string? tag = null)
    { Meta = meta; Target = target; Mod = mod; Duration = duration; Tag = tag; }

    public override string ToString()
        => $"BuffApplyReq {Mod?.GetType().Name} to {Target.name}, dur={Duration}, tag={Tag}";
}

/// <summary>
/// [Request] ����Ʈ(���־�/�����/����) ���� ��û.
/// ������ ������ ��å���� ����������, �ð�/�����/���� ���� �ʿ� ġ��ģ ������̾�.
/// </summary>
public readonly struct EffectApplyReq
{
    public readonly GameEventMeta Meta;
    public readonly Transform Target;
    public readonly IEffectModifier Mod;
    public readonly float Duration;
    public readonly string? Tag;

    public EffectApplyReq(GameEventMeta meta, Transform target, IEffectModifier mod, float duration, string? tag = null)
    { Meta = meta; Target = target; Mod = mod; Duration = duration; Tag = tag; }

    public override string ToString()
        => $"EffectApplyReq {Mod?.GetType().Name} to {Target.name}, dur={Duration}, tag={Tag}";
}

/// <summary>
/// [Request] ���� ���� ��û.
/// Ư�� Mod �ν��Ͻ��� �����ϴ� �������� ����.
/// ��å�� ���� Tag ��� ����� �����ϴ� ���� ��û�� �߰��� ���� �ֽ��ϴ�.
/// </summary>
public readonly struct BuffRemoveReq
{
    public readonly GameEventMeta Meta;
    public readonly Transform Target;
    public readonly IStatModifier Mod;

    public BuffRemoveReq(GameEventMeta meta, Transform target, IStatModifier mod)
    { Meta = meta; Target = target; Mod = mod; }

    public override string ToString()
        => $"BuffRemoveReq {Mod?.GetType().Name} from {Target.name}";
}

/// <summary>
/// [Request] ����Ʈ ���� ��û.
/// </summary>
public readonly struct EffectRemoveReq
{
    public readonly GameEventMeta Meta;
    public readonly Transform Target;
    public readonly IEffectModifier Mod;

    public EffectRemoveReq(GameEventMeta meta, Transform target, IEffectModifier mod)
    { Meta = meta; Target = target; Mod = mod; }

    public override string ToString()
        => $"EffectRemoveReq {Mod?.GetType().Name} from {Target.name}";
}

#endregion

#region ===== �ܺ� ������ ���(�������̽�) =====

// �ܺ� ������ ���(����/����Ʈ ���� ����).
// - �̺�Ʈ ���̷ε�� �� ��࿡�� �����ϰ�, ��ü ������ �ý��� ���ο� ����ϴ�.
// - �׽�Ʈ ����/��ŷ�� �������ϴ�.
public interface IStatModifier
{
    void Apply(PlayerStats s);
    void Remove(PlayerStats s);
}

public interface IEffectModifier
{
    void Apply(PlayerEffects fx);
    void Remove(PlayerEffects fx);
}

#endregion
