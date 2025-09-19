using System;
using UnityEngine;
using ActInterfaces;

[Serializable]
public struct CollisionPolicy
{
    public LayerMask wallsMask;      // �׻� ����
    public LayerMask enemyMask;      // �� ���̾�
    public bool enemyAsBlocker;      // true: ���� '��ó��' ����
    public float radius;             // ��ü �ݰ�
    public float skin;               // �� ������ ���� ����

    // ���߰�: ������ ���� �� '����' ��� ���� �����̵带 �������(��á��̵� ���� ��å���� ����)
    public bool allowWallSlide;
}

public struct MoveResult
{
    public Vector2 actualDelta;      // ���� �̵���
    public bool hitWall;
    public bool hitEnemy;
    public Transform hitTransform;
    public Vector2 hitNormal;        // ù �浹�� ����(�����̵� �� ����)
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
        enemyAsBlocker = false,     // �ڱ⺻ �̵��� ���� '�������� ����'���� ��ȯ(���� ���� ����)
        radius = 0.5f,
        skin = 0.05f,
        allowWallSlide = true       // �⺻�� �����̵� ���(���/�̵����� ���� �������̵�)
    };

    Rigidbody2D rb;
    CollisionPolicy current;
    public Vector2 LastMoveVector { get; private set; } = Vector2.zero;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        current = defaultPolicy;
    }

    public IDisposable With(in CollisionPolicy overridePolicy)
    {
        var prev = current;
        current = overridePolicy;
        return new Scope(() => current = prev);
    }
    sealed class Scope : IDisposable { readonly Action onDispose; public Scope(Action a) { onDispose = a; } public void Dispose() { onDispose?.Invoke(); } }

    /// <summary>
    /// ������ ���� ��ħ �ؼ�. 
    /// - ���� �ڵ��� "preferredDir ������ transform.right(+X)�� �δ�"�� ����: ��ġ �ʴ� +X �и� ����.
    /// - ȣ���ڰ� ��Ȯ�� ��ȣ ����(�ֱ� �̵�/��� ����)�� ������ ���� ����ϼ���.
    /// </summary>
    public void BeginFrameDepenetrate(Vector2 preferredDir)
    {
        if (preferredDir.sqrMagnitude <= 1e-6f) return; // ������ +X �и� ����
        if (Physics2D.OverlapCircle(transform.position, current.radius, current.wallsMask))
        {
            var dir = preferredDir.normalized;
            MoveDiscrete(dir * (current.radius + Mathf.Max(0.01f, current.skin)));
        }
    }

    /// <summary>
    /// �浹-���� �̵�(���� ����).
    /// - ������ ù �浹���� �̵��� '����'���� �ʰ�, ��å�� ����ϸ� ���� ������ ������ '���� �����̵�'�� �õ�.
    /// - �� ������ ��å���θ� ����(�⺻ �̵��� false, ��� ������� true�� �������̵�).
    /// </summary>
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

            // �ڽ����̵�: ���� ��� ù ������ ���� ���� ������ '���� ������'�� �Һ� (���� ���ο����� 2�� ĳ��Ʈ ���)
            result.hitWall = true;
            result.hitTransform = wallHit.transform;
            result.hitNormal = wallHit.normal;

            Vector2 moved = (Vector2)transform.position - origin;
            float remaining = Mathf.Max(0f, dist - moved.magnitude);

            if (current.allowWallSlide && remaining > 1e-5f)
            {
                // ���� ���� ���: t = desiredDelta - n*(desiredDelta��n)
                Vector2 n = wallHit.normal.normalized;
                Vector2 tangential = desiredDelta - Vector2.Dot(desiredDelta, n) * n;

                if (tangential.sqrMagnitude > 1e-6f)
                {
                    Vector2 tdir = tangential.normalized;
                    float tdist = Mathf.Min(remaining, tangential.magnitude);

                    // �������� �� �� �� ĳ��Ʈ(�� �� �� ��)
                    var w2 = Physics2D.CircleCast((Vector2)transform.position, current.radius, tdir, tdist, current.wallsMask);
                    if (w2.collider)
                    {
                        float tToHit = w2.distance;
                        if (tToHit > 0f) MoveDiscrete(tdir * tToHit);
                        // ���� �ٽ� ������ ���⼭ �����̵� ����(���� ����, '����' ��Ģ)
                    }
                    else
                    {
                        if (current.enemyAsBlocker && current.enemyMask.value != 0)
                        {
                            var e2 = Physics2D.CircleCast((Vector2)transform.position, current.radius, tdir, tdist, current.enemyMask);
                            if (e2.collider)
                            {
                                float tToHit = e2.distance;
                                if (tToHit > 0f) MoveDiscrete(tdir * tToHit);
                                result.hitEnemy = true;
                                result.hitTransform = e2.transform;
                                result.hitNormal = e2.normal;
                            }
                            else
                            {
                                MoveDiscrete(tdir * tdist);
                            }
                        }
                        else
                        {
                            MoveDiscrete(tdir * tdist);
                        }
                    }
                }
            }

            result.actualDelta = (Vector2)transform.position - origin;
            LastMoveVector = result.actualDelta;
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
                LastMoveVector = result.actualDelta;
                return result;
            }
        }

        // 3) �浹 ���� �� ���� �̵�
        MoveDiscrete(desiredDelta);
        result.actualDelta = desiredDelta;
        LastMoveVector = result.actualDelta;
        return result;
    }

    void MoveDiscrete(Vector2 delta)
    {
        if (delta.sqrMagnitude <= 0f) return;
        if (rb) rb.MovePosition((Vector2)transform.position + delta);
        else transform.position += (Vector3)delta;
    }

    public CollisionPolicy CurrentPolicy => current;
}