using UnityEngine;
using ActInterfaces;

public static class TargetAnchorUtil2D
{
    /// <summary>
    /// 원점(origin)에서 목표점(desired)로 가는 경로에 벽이 있으면,
    /// 첫 충돌점 직전(skin)으로 목적지를 당기고, 없으면 desired를 반환합니다.
    /// radius>0이면 CircleCast로, 0이면 Raycast로 검사합니다.
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
            // 캐릭터/투사체 반경을 고려해 통로가 막히면 조기 차단
            var hit = Physics2D.CircleCast(from, radius, dir, dist, wallsMask);
            if (hit.collider != null)
            {
                // 충돌점 직전으로 당김
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

        // 막히지 않았으면 원래 목표점 허용
        return desired;
    }

    /// <summary>
    /// 목적지가 벽 내부/교차 상태인지 최종 검증. 벽과 겹치면 false.
    /// </summary>
    public static bool IsPointFree(Vector3 pos, float radius, LayerMask wallsMask)
    {
        if (radius > 0f)
            return Physics2D.OverlapCircle(pos, radius, wallsMask) == null;
        // 점 샘플은 대개 안전하지만, 필요 시 작은 반경으로도 체크 가능
        return true;
    }
    ///<summary> 유틸: 마우스 월드 좌표 (2D 기준, Z=owner의 Z로 고정) </summary>
    public static Vector3 CursorWorld2D(Camera cam, Transform owner, float depthFallback)
    {
        var sp = Input.mousePosition;
        float depth = Mathf.Abs(cam.transform.position.z - owner.position.z);
        if (depth < 0.01f) depth = depthFallback;
        var w = cam.ScreenToWorldPoint(new Vector3(sp.x, sp.y, depth));
        w.z = owner.position.z;
        return w;
    }
    public static Vector2 GetMoveDirOrFacing(Transform t)
    {
        // 플레이어 입력 소스가 있다면 거기서 “최근 이동 벡터”를 받아오세요.
        // 없으면 정면을 기본으로.
        var input = t.GetComponent<IMovable>() ?? t.GetComponentInChildren<IMovable>(); // 프로젝트별 인터페이스
        if (input == null) Debug.Log("Have ☆ Children?");
        Vector2 mv = input != null ? input.LastMoveVector : Vector2.zero;
        if (mv.sqrMagnitude < 0.01f) mv = (Vector2)t.right;
        return mv.normalized;
    }

    public static Vector3 ResolveReachablePoint2D(Vector3 origin, Vector3 desired, LayerMask walls, float radius, float skin)
    {
        Vector2 from = origin, to = desired, dir = to - from; float dist = dir.magnitude;
        if (dist <= Mathf.Epsilon) return origin;
        dir /= dist; skin = Mathf.Max(0f, skin);

        if (radius > 0f)
        {
            var hit = Physics2D.CircleCast(from, radius, dir, dist, walls);
            if (hit.collider) { float back = Mathf.Min(skin, hit.distance); return hit.point - dir * back; }
        }
        else
        {
            var hit = Physics2D.Raycast(from, dir, dist, walls);
            if (hit.collider) { float back = Mathf.Min(skin, hit.distance); return hit.point - dir * back; }
        }
        return desired;
    }
}
