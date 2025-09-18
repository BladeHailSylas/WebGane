using System;
using UnityEngine;
using ActInterfaces;

[Serializable]
public struct CollisionPolicy
{
    public LayerMask wallsMask;      // �׻� ����
    public LayerMask enemyMask;      // �� ���̾�
    public bool enemyAsBlocker;      // true: ���� '��ó��' ����, false: ���� ���� ��� �ƴ�
    public float radius;             // ��ü �ݰ�
    public float skin;               // �� ������ ���� ����
}

public struct MoveResult
{
    public Vector2 actualDelta;      // ���� �̵���(�浹�� �پ�� �� ����)
    public bool hitWall;
    public bool hitEnemy;
    public Transform hitTransform;
    public Vector2 hitNormal;
}

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
public class KinematicMotor2D : MonoBehaviour, ISweepable
{
    [Header("Defaults")]
    public CollisionPolicy defaultPolicy = new()
    {
        wallsMask = 0,
        enemyMask = 0,
        enemyAsBlocker = true,
        radius = 0.5f,
        skin = 0.05f
    };

    Rigidbody2D rb;
    CollisionPolicy current;
    public Vector2 LastMoveVector { get; private set; } = Vector2.zero;
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        // [RULE: Kinematic] �׻� Kinematic�� ������ ���
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        current = defaultPolicy;
    }

    // [RULE: PolicyScope] �Ͻ� ��å �������̵� ������
    public IDisposable With(in CollisionPolicy overridePolicy)
    {
        var prev = current;
        current = overridePolicy;
        return new Scope(() => current = prev);
    }
    sealed class Scope : IDisposable { readonly Action onDispose; public Scope(Action a) { onDispose = a; } public void Dispose() { onDispose?.Invoke(); } }

    // [RULE: Depenetrate] ���� ��ħ Ż��(�ǵ� �������� radius+skin��ŭ)
    public void BeginFrameDepenetrate(Vector2 preferredDir)
    {
        // �� ��ħ�� ���(�� ��ħ���� �о�� ���δ� ��å�� ���� ���� ����)
        if (Physics2D.OverlapCircle(transform.position, current.radius, current.wallsMask))
        {
            var dir = preferredDir.sqrMagnitude > 0.0001f ? preferredDir.normalized : (Vector2)transform.right;
            MoveDiscrete(dir * (current.radius + Mathf.Max(0.01f, current.skin)));
        }
    }

    // [RULE: SweepClamp] �浹-���� �̵�(���� ����)
    public MoveResult SweepMove(Vector2 desiredDelta)
    {
        var result = new MoveResult { actualDelta = Vector2.zero };
        if (desiredDelta.sqrMagnitude <= 0f) return result;

        Vector2 origin = transform.position;
        Vector2 dir = desiredDelta.normalized;
        float dist = desiredDelta.magnitude;

        // 1) �� �켱 ĳ��Ʈ
        var wallHit = Physics2D.CircleCast(origin, current.radius, dir, dist, current.wallsMask);
        if (wallHit.collider)
        {
            float toHit = wallHit.distance;
            if (toHit > 0f) MoveDiscrete(dir * toHit);
            // [RULE: SKIN] �� ������ ������ �ҷ� ����(����)
            // MoveDiscrete(dir * Mathf.Min(current.skin, 0.01f));
            result.actualDelta = (Vector2)transform.position - origin;
            result.hitWall = true;
            result.hitTransform = wallHit.transform;
            result.hitNormal = wallHit.normal;
            return result;
        }

        // 2) �� ���� ��å
        if (current.enemyAsBlocker && current.enemyMask.value != 0)
        {
            var enemyHit = Physics2D.CircleCast(origin, current.radius, dir, dist, current.enemyMask);
            if (enemyHit.collider)
            {
                float toHit = enemyHit.distance;
                if (toHit > 0f) MoveDiscrete(dir * toHit);
                result.actualDelta = (Vector2)transform.position - origin;
                result.hitEnemy = true;
                result.hitTransform = enemyHit.transform;
                result.hitNormal = enemyHit.normal;
                return result;
            }
        }

        // 3) �浹 ���� �� ���� �̵�
        MoveDiscrete(desiredDelta);
        result.actualDelta = desiredDelta;
        LastMoveVector = result.actualDelta;
        return result;
    }

    // ���� �̵� ����: Rigidbody2D.Kinematic ��� MovePosition
    void MoveDiscrete(Vector2 delta)
    {
        if (delta.sqrMagnitude <= 0f) return;
        if (rb) rb.MovePosition((Vector2)transform.position + delta);
        else transform.position += (Vector3)delta;
    }

    // ���� ��å�� ������ �� �ֵ��� Getter ����(����)
    public CollisionPolicy CurrentPolicy => current;
}
