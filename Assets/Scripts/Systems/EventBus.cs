using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Ÿ�� T�� ������ ������ Ű(object)�� �������� ����/�����ϴ� ������ EventBus.
/// - SafeInvoke: �����ں� try/catch�� �ݸ�
/// - Weak context: MonoBehaviour �� ���ؽ�Ʈ�� �ı��Ǹ� �ڵ� ����
/// - Snapshot invoke: ��ȸ �� ���� ��ȣ
/// - Scope ����: ���� T�� ä��/���ʺ��� �и� ����
/// - Thread-safe: ���� lock
/// </summary>
public static class EventBus
{
    // ���� ������ ǥ��
    private interface ISub
    {
        bool IsAlive { get; }               // ���ؽ�Ʈ ���� ����(������ true)
        bool Matches(Delegate d, object ctx);
        void Invoke(object payload);        // ���� ȣ��
        UnityEngine.Object Context { get; } // ������
        Delegate Handler { get; }           // ������
    }

    private class Sub<T> : ISub
    {
        private readonly WeakReference<UnityEngine.Object> _ctx; // �ı� ����
        public Delegate Handler { get; }
        public UnityEngine.Object Context
        {
            get { _ctx.TryGetTarget(out var o); return o; }
        }

        private readonly Action<T> _action;

        public Sub(Action<T> action, UnityEngine.Object context)
        {
            Handler = action;
            _action = action;
            _ctx = context != null ? new WeakReference<UnityEngine.Object>(context) : null;
        }

        public bool IsAlive
        {
            get
            {
                if (_ctx == null) return true; // ���ؽ�Ʈ ���� = �׻� alive
                if (!_ctx.TryGetTarget(out var o)) return false; // ���ŵ�
                return o != null; // UnityEngine.Object �ı� ����
            }
        }

        public bool Matches(Delegate d, object ctx)
        {
            return Equals(Handler, d) && Equals(Context, ctx as UnityEngine.Object);
        }

        public void Invoke(object payload)
        {
            // �����ں� ���� �ݸ�(SafeInvoke)
            try
            {
                if (!IsAlive) return;
                _action((T)payload);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }

    // (Type, Scope) �� ������ ����Ʈ
    private static readonly Dictionary<(Type, object), List<ISub>> Map = new();
    private static readonly object Gate = new();

    /// <summary>
    /// ����: ���ؽ�Ʈ(�ɼ�)�� ���� �ѱ�� �ı� �� �ڵ� ������.
    /// </summary>
    public static void Subscribe<T>(Action<T> handler, UnityEngine.Object context = null, object scope = null)
    {
        if (handler == null) return;
        var key = (typeof(T), scope);
        lock (Gate)
        {
            if (!Map.TryGetValue(key, out var list))
            {
                list = new List<ISub>(4);
                Map[key] = list;
            }
            list.Add(new Sub<T>(handler, context));
        }
    }

    /// <summary>
    /// ���� ����: ���� handler/context/scope ������ ����.
    /// </summary>
    public static void Unsubscribe<T>(Action<T> handler, UnityEngine.Object context = null, object scope = null)
    {
        var key = (typeof(T), scope);
        lock (Gate)
        {
            if (!Map.TryGetValue(key, out var list)) return;
            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (list[i].Matches(handler, context))
                    list.RemoveAt(i);
            }
            if (list.Count == 0) Map.Remove(key);
        }
    }

    /// <summary>
    /// ����: ���� (T, scope) �����ڿ��Ը� ����.
    /// ���ؽ�Ʈ�� ���� �����ڴ� ȣ�� ��/�ķ� û��.
    /// </summary>
    public static void Publish<T>(T evt, object scope = null)
    {
        var key = (typeof(T), scope);
        List<ISub> snapshot = null;

        lock (Gate)
        {
            if (Map.TryGetValue(key, out var list) && list.Count > 0)
            {
                // ���� ������ 1�� û��
                for (int i = list.Count - 1; i >= 0; i--)
                    if (!list[i].IsAlive) list.RemoveAt(i);

                // ������ ����(��ȸ �� ���� ��ȣ)
                if (list.Count > 0) snapshot = new List<ISub>(list);
                if (list.Count == 0) Map.Remove(key);
            }
        }

        if (snapshot == null) return;

        // ���� ȣ��
        foreach (var sub in snapshot)
            sub.Invoke(evt);

        // ȣ�� �� 2�� û��(����)
        lock (Gate)
        {
            if (Map.TryGetValue(key, out var list))
            {
                for (int i = list.Count - 1; i >= 0; i--)
                    if (!list[i].IsAlive) list.RemoveAt(i);
                if (list.Count == 0) Map.Remove(key);
            }
        }
    }

    // ---- �����/����(����) ----
    public static int Count(Type t, object scope = null)
    {
        lock (Gate)
            return Map.TryGetValue((t, scope), out var list) ? list.Count : 0;
    }
}