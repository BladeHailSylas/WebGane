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
    public bool allowWallSlide;      // �� ���� �� ���� �����ӿ� ���� �����̵� ���
}

public struct MoveResult
{
    public Vector2 actualDelta;
    public bool hitWall, hitEnemy;
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
        enemyAsBlocker = false,   // �Ϲ� �̵��� ���� '��'���� ���� ����(���� ����)
        radius = 0.5f,
        skin = 0.05f,
        allowWallSlide = true
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
    /// ������ ���� ��ħ Ż��(������ ������ MTV ������ ���).
    /// - ���� +X/�ǵ� ���� '�б�'�� ����, ���� �� �ݴ������� ���������� ������ ����.
    /// - �Ÿ�(ħ����)�� ColliderDistance ���÷� ���� �� '��ħ�� + skin' ��ŭ�� ��Ż.
    /// </summary>
    public void BeginFrameDepenetrate(Vector2 mtvDir)
    {
        if (mtvDir.sqrMagnitude <= 1e-6f) return;

        // ��ħ �ĺ� ����
        var cols = Physics2D.OverlapCircleAll(transform.position, current.radius, current.wallsMask);
        float worstPenetration = 0f; // �����ϼ��� ���� ħ��(���밪 �ִ�)

        foreach (var c in cols)
        {
            var dist = Physics2D.Distance(c, GetComponent<Collider2D>()); // c(��) vs ��(��ü) ���� ����
            if (dist.distance < worstPenetration) worstPenetration = dist.distance; // ���� ����(���� ���� ħ��)
        }

        if (worstPenetration < 0f)
        {
            float moveOut = (-worstPenetration) + Mathf.Max(0.01f, current.skin);
            MoveDiscrete(mtvDir.normalized * moveOut);
        }
    }

    /// <summary>
    /// �浹-���� �̵�(���� ����).
    /// - ���� ������ '����'�� �ƴ϶� '���� or ���� ������ �����̵�'�� ����.
    /// - �����̵�� ���� �ݺ�(2~3ȸ)���� ���� ������ ���� �����ӿ� �ִ��� ����.
    /// </summary>
    public MoveResult SweepMove(Vector2 desiredDelta)
    {
        MoveResult result = default;
        if (desiredDelta.sqrMagnitude <= 0f) return result;

        Vector2 startPos = transform.position;
        float remaining = desiredDelta.magnitude;
        Vector2 wishDir = desiredDelta.normalized;

        int iterations = 0;
        const int kMaxSlideIters = 3;

        while (remaining > 1e-5f && iterations < kMaxSlideIters)
        {
            iterations++;

            // 1) �� �켱
            var wallHit = Physics2D.CircleCast((Vector2)transform.position, current.radius, wishDir, remaining, current.wallsMask);
            if (wallHit.collider)
            {
                float toHit = wallHit.distance;
                if (toHit > 0f) MoveDiscrete(wishDir * toHit);

                result.hitWall = true;
                result.hitTransform = wallHit.transform;
                result.hitNormal = wallHit.normal;

                remaining -= Mathf.Max(0f, toHit);

                if (current.allowWallSlide)
                {
                    // ���� ���� ����: t = v - n*(v��n)
                    Vector2 n = wallHit.normal;
                    Vector2 v = wishDir * remaining;
                    Vector2 tangential = v - Vector2.Dot(v, n) * n;

                    if (tangential.sqrMagnitude > 1e-6f)
                    {
                        wishDir = tangential.normalized;
                        remaining = tangential.magnitude;
                        continue; // ���� �����ӿ� �������� ��õ�
                    }
                }

                // �����̵� ���ϸ� �� ������ ����
                remaining = 0f;
                break;
            }

            // 2) �� ����(��å)
            if (current.enemyAsBlocker && current.enemyMask.value != 0)
            {
                var enemyHit = Physics2D.CircleCast((Vector2)transform.position, current.radius, wishDir, remaining, current.enemyMask);
                if (enemyHit.collider)
                {
                    float toHit = enemyHit.distance;
                    if (toHit > 0f) MoveDiscrete(wishDir * toHit);
                    result.hitEnemy = true;
                    result.hitTransform = enemyHit.transform;
                    result.hitNormal = enemyHit.normal;
                    remaining -= Mathf.Max(0f, toHit);
                    remaining = 0f; // ���� �����̵� ��� �ƴ�(��å�� ���� �ʿ� �� Ȯ��)
                    break;
                }
            }

            // 3) �浹 ���� �� ���� �Ÿ� ���� �̵�
            MoveDiscrete(wishDir * remaining);
            remaining = 0f;
        }

        result.actualDelta = (Vector2)transform.position - startPos;
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
