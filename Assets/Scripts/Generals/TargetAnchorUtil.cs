using UnityEngine;

public static class TargetAnchorUtil2D
{
    /// <summary>
    /// ����(origin)���� ��ǥ��(desired)�� ���� ��ο� ���� ������,
    /// ù �浹�� ����(skin)���� �������� ����, ������ desired�� ��ȯ�մϴ�.
    /// radius>0�̸� CircleCast��, 0�̸� Raycast�� �˻��մϴ�.
    /// </summary>
    public static Vector3 ResolveReachablePoint(
        Vector3 origin,
        Vector3 desired,
        LayerMask wallsMask,
        float radius = 0.0f,
        float skin = 0.05f)
    {
        Vector2 from = origin;
        Vector2 to = desired;
        Vector2 dir = (to - from);
        float dist = dir.magnitude;
        if (dist <= Mathf.Epsilon) return origin;

        dir /= dist;
        skin = Mathf.Max(0f, skin);

        if (radius > 0f)
        {
            // ĳ����/����ü �ݰ��� ����� ��ΰ� ������ ���� ����
            var hit = Physics2D.CircleCast(from, radius, dir, dist, wallsMask);
            if (hit.collider != null)
            {
                // �浹�� �������� ���
                float back = Mathf.Min(skin, hit.distance);
                return hit.point - dir * back;
            }
        }
        else
        {
            var hit = Physics2D.Raycast(from, dir, dist, wallsMask);
            if (hit.collider != null)
            {
                float back = Mathf.Min(skin, hit.distance);
                return hit.point - dir * back;
            }
        }

        // ������ �ʾ����� ���� ��ǥ�� ���
        return desired;
    }

    /// <summary>
    /// �������� �� ����/���� �������� ���� ����. ���� ��ġ�� false.
    /// </summary>
    public static bool IsPointFree(Vector3 pos, float radius, LayerMask wallsMask)
    {
        if (radius > 0f)
            return Physics2D.OverlapCircle(pos, radius, wallsMask) == null;
        // �� ������ �밳 ����������, �ʿ� �� ���� �ݰ����ε� üũ ����
        return true;
    }
    ///<summary> ��ƿ: ���콺 ���� ��ǥ (2D ����, Z=owner�� Z�� ����) </summary>
    public static Vector3 GetCursorWorld2D(Camera cam, Transform owner)
    {
        var sp = Input.mousePosition;
        // ī�޶󿡼� owner������ ���̸� ���
        float depth = Mathf.Abs(cam.transform.position.z - owner.position.z);
        var w = cam.ScreenToWorldPoint(new Vector3(sp.x, sp.y, depth));
        w.z = owner.position.z;
        return w;
    }
    public static Vector3 ResolveReachablePoint2D(
    Vector3 origin, Vector3 desired, LayerMask wallsMask, float radius, float skin)
    {
        Vector2 from = origin; Vector2 to = desired;
        Vector2 dir = to - from; float dist = dir.magnitude;
        if (dist <= Mathf.Epsilon) return origin;
        dir /= dist; skin = Mathf.Max(0f, skin);

        if (radius > 0f)
        {
            var hit = Physics2D.CircleCast(from, radius, dir, dist, wallsMask);
            if (hit.collider != null)
            {
                float back = Mathf.Min(skin, hit.distance);
                // ���� �������� ���� ����: hit.point - hit.normal * skin
                return hit.point - dir * back;
            }
        }
        else
        {
            var hit = Physics2D.Raycast(from, dir, dist, wallsMask);
            if (hit.collider != null)
            {
                float back = Mathf.Min(skin, hit.distance);
                return hit.point - dir * back;
            }
        }
        return desired;
    }
}
