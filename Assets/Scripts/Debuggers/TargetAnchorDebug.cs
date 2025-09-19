using UnityEngine;

/// <summary>
/// TargetAnchor(런타임 앵커) 시각화 디버거.
/// - 씬뷰: 굵은 원, 이름 라벨, (선택) 최근 이동 벡터/법선 등
/// - 게임뷰: LineRenderer로 얇은 도넛 표시(옵션)
/// </summary>
[DisallowMultipleComponent]
public sealed class TargetAnchorDebug : MonoBehaviour
{
    [Header("Appearance")]
    [SerializeField] float radius = 0.15f;            // 화면에서 보일 임의 반경(시각화 전용)
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
            lr.widthMultiplier = 0.03f;      // 화면에서 적당히 보이는 두께
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.textureMode = LineTextureMode.Stretch;

            ring = new Vector3[segments + 1];
            BuildRing();
        }
    }

    void Update()
    {
        if (!enableRuntimeVisual || lr == null) return;

        // 살짝 펄스 효과(가벼운 시각화)
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
        // 씬뷰에서 굵은 원
        Gizmos.color = new Color(0.2f, 0.9f, 1f, 0.9f);
        DrawWireDisc(transform.position, Vector3.forward, radius, 32);

        // 라벨
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