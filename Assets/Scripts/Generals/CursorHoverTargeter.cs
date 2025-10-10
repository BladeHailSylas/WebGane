using UnityEngine;
using ActInterfaces;

public class CursorHoverTargetProvider : MonoBehaviour, ITargetable
{
    public LayerMask enemyMask;
    public float maxRange = 12f;
    public bool requireLineOfSight = true;
    public LayerMask blockerMask;

    Camera cam;
    Transform owner;

    void Awake()
    {
        cam = Camera.main;
        owner = GetComponentInParent<SkillRunner>()?.transform ?? transform.root;
    }

    public bool TryGetTarget(out Transform target)
    {
        target = null;
        //if (!cam) { Debug.LogWarning("[TargetProvider] MainCamera not found"); return false; }

        var m = cam.ScreenToWorldPoint(Input.mousePosition);
        Vector2 p = m;
        Vector2 origin = owner ? (Vector2)owner.position : Vector2.zero;

        // 1) ����Ʈ ��Ʈ
        var hits = Physics2D.OverlapPointAll(p, enemyMask);
        if (hits.Length == 0)
        {
            // 1�� ) ���� Ȯ��
            hits = Physics2D.OverlapCircleAll(p, 0.0625f, enemyMask);
            if (hits.Length == 0)
            {
                // �α� �� ����
                //Debug.Log("[TargetProvider] No enemy under cursor");
                return false;
            }
        }

        Transform best = null;
        float bestDist = float.PositiveInfinity;

        foreach (var h in hits)
        {
            var t = h.transform;
            float d = Vector2.Distance(origin, h.bounds.ClosestPoint(origin));
            bool inRange = d <= maxRange;

            bool blocked = false;
            if (requireLineOfSight)
            {
                var to = (Vector2)h.bounds.ClosestPoint(origin);
                var hit = Physics2D.Linecast(origin, to, blockerMask);
                blocked = hit.collider != null; // �� line�� ������ �� ����, hit ������ �߸��Ǿ���?
            }

            // Ÿ�� �α�
            //Debug.Log($"[TargetProvider] hit={h.name} layer={LayerMask.LayerToName(h.gameObject.layer)} d={d:F2} inRange={inRange} blocked={blocked}");
            if (!inRange || blocked) continue;
            if (d < bestDist) { bestDist = d; best = t; }
        }

        if (!best)
        {
            //Debug.Log("[TargetProvider] Hits found but filtered out by range/LOS");
            return false;
        }

        target = best;
        return true;
    }
}