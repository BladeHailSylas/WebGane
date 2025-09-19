using UnityEngine;

/// <summary>
/// TargetAnchor(��Ÿ�� ��Ŀ) �ð�ȭ �����.
/// - ����: ���� ��, �̸� ��, (����) �ֱ� �̵� ����/���� ��
/// - ���Ӻ�: LineRenderer�� ���� ���� ǥ��(�ɼ�)
/// </summary>
[DisallowMultipleComponent]
public sealed class TargetAnchorDebug : MonoBehaviour
{
    [Header("Appearance")]
    [SerializeField] float radius = 0.15f;            // ȭ�鿡�� ���� ���� �ݰ�(�ð�ȭ ����)
    [SerializeField] bool enableRuntimeVisual = true;
    [SerializeField] int segments = 24;

    LineRenderer lr; Vector3[] ring;

    void Awake()
    {
        if (enableRuntimeVisual)
        {
            lr = gameObject.GetComponent<LineRenderer>();
            if (!lr) lr = gameObject.AddComponent<LineRenderer>();
            lr.positionCount = segments + 1;
            lr.loop = true;
            lr.useWorldSpace = true;
            lr.widthMultiplier = 0.03f;      // ȭ�鿡�� ������ ���̴� �β�
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.textureMode = LineTextureMode.Stretch;

            ring = new Vector3[segments + 1];
            BuildRing();
        }
    }

    void Update()
    {
        if (!enableRuntimeVisual || lr == null) return;

        // ��¦ �޽� ȿ��(������ �ð�ȭ)
        float r = radius * (1f + 0.05f * Mathf.Sin(Time.time * 6f));
        for (int i = 0; i <= segments; i++)
        {
            float a = (Mathf.PI * 2f) * i / segments;
            ring[i] = transform.position + new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, 0f);
        }
        lr.SetPositions(ring);
    }

    void BuildRing()
    {
        for (int i = 0; i <= segments; i++)
        {
            float a = (Mathf.PI * 2f) * i / segments;
            ring[i] = transform.position + new Vector3(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius, 0f);
        }
        lr.SetPositions(ring);
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        // ���信�� ���� ��
        Gizmos.color = new Color(0.2f, 0.9f, 1f, 0.9f);
        DrawWireDisc(transform.position, Vector3.forward, radius, 32);

        // ��
        UnityEditor.Handles.color = Color.cyan;
        UnityEditor.Handles.Label(transform.position + Vector3.up * (radius + 0.05f), $"ANCHOR\n{gameObject.name}");
    }

    static void DrawWireDisc(Vector3 center, Vector3 normal, float r, int seg)
    {
        Vector3 prev = center + new Vector3(r, 0, 0);
        for (int i = 1; i <= seg; i++)
        {
            float a = (Mathf.PI * 2f) * i / seg;
            Vector3 cur = center + new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, 0f);
            Gizmos.DrawLine(prev, cur);
            prev = cur;
        }
    }
#endif
}