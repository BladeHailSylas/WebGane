using UnityEngine;
using System.Collections.Generic;

public static class TargetAnchorPool
{
    static readonly Stack<Transform> pool = new();
    static Transform root;
	public static bool IsAnchor(Transform target)
	{
		return pool.Contains(target);
	}
    public static Transform Acquire(Vector3 pos)
    {
        if (!root)
        {
            var go = new GameObject("_TargetAnchors");
            Object.DontDestroyOnLoad(go);
            root = go.transform;
        }

        var t = pool.Count > 0 ? pool.Pop() : new GameObject("Anchor").transform;
        t.SetParent(root, false);
        t.position = pos;
        t.gameObject.SetActive(true);
        //Display(for debug)
        if (!t.GetComponent<TargetAnchorDebug>())
            t.gameObject.AddComponent<TargetAnchorDebug>();
        return t;
    }

    public static void Release(Transform t)
    {
        if (!t || pool.Contains(t)) return;
        t.gameObject.SetActive(false);
        t.SetParent(root, false);
        pool.Push(t);
    }
}