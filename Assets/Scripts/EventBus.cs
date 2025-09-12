using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 타입 T와 선택적 스코프 키(object)를 기준으로 발행/구독하는 안전한 EventBus.
/// - SafeInvoke: 구독자별 try/catch로 격리
/// - Weak context: MonoBehaviour 등 컨텍스트가 파괴되면 자동 정리
/// - Snapshot invoke: 순회 중 수정 보호
/// - Scope 지원: 같은 T라도 채널/오너별로 분리 가능
/// - Thread-safe: 얕은 lock
/// </summary>
public static class EventBus
{
    // 내부 구독자 표현
    private interface ISub
    {
        bool IsAlive { get; }               // 컨텍스트 생존 여부(없으면 true)
        bool Matches(Delegate d, object ctx);
        void Invoke(object payload);        // 안전 호출
        UnityEngine.Object Context { get; } // 디버깅용
        Delegate Handler { get; }           // 디버깅용
    }

    private class Sub<T> : ISub
    {
        private readonly WeakReference<UnityEngine.Object> _ctx; // 파괴 감지
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
                if (_ctx == null) return true; // 컨텍스트 없음 = 항상 alive
                if (!_ctx.TryGetTarget(out var o)) return false; // 수거됨
                return o != null; // UnityEngine.Object 파괴 감지
            }
        }

        public bool Matches(Delegate d, object ctx)
        {
            return Equals(Handler, d) && Equals(Context, ctx as UnityEngine.Object);
        }

        public void Invoke(object payload)
        {
            // 구독자별 예외 격리(SafeInvoke)
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

    // (Type, Scope) → 구독자 리스트
    private static readonly Dictionary<(Type, object), List<ISub>> _map = new();
    private static readonly object _gate = new();

    /// <summary>
    /// 구독: 컨텍스트(옵션)를 같이 넘기면 파괴 시 자동 해제됨.
    /// </summary>
    public static void Subscribe<T>(Action<T> handler, UnityEngine.Object context = null, object scope = null)
    {
        if (handler == null) return;
        var key = (typeof(T), scope);
        lock (_gate)
        {
            if (!_map.TryGetValue(key, out var list))
            {
                list = new List<ISub>(4);
                _map[key] = list;
            }
            list.Add(new Sub<T>(handler, context));
        }
    }

    /// <summary>
    /// 구독 해제: 동일 handler/context/scope 조합을 제거.
    /// </summary>
    public static void Unsubscribe<T>(Action<T> handler, UnityEngine.Object context = null, object scope = null)
    {
        var key = (typeof(T), scope);
        lock (_gate)
        {
            if (!_map.TryGetValue(key, out var list)) return;
            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (list[i].Matches(handler, context))
                    list.RemoveAt(i);
            }
            if (list.Count == 0) _map.Remove(key);
        }
    }

    /// <summary>
    /// 발행: 동일 (T, scope) 구독자에게만 전달.
    /// 컨텍스트가 죽은 구독자는 호출 전/후로 청소.
    /// </summary>
    public static void Publish<T>(T evt, object scope = null)
    {
        var key = (typeof(T), scope);
        List<ISub> snapshot = null;

        lock (_gate)
        {
            if (_map.TryGetValue(key, out var list) && list.Count > 0)
            {
                // 죽은 구독자 1차 청소
                for (int i = list.Count - 1; i >= 0; i--)
                    if (!list[i].IsAlive) list.RemoveAt(i);

                // 스냅샷 복사(순회 중 수정 보호)
                if (list.Count > 0) snapshot = new List<ISub>(list);
                if (list.Count == 0) _map.Remove(key);
            }
        }

        if (snapshot == null) return;

        // 안전 호출
        foreach (var sub in snapshot)
            sub.Invoke(evt);

        // 호출 후 2차 청소(선택)
        lock (_gate)
        {
            if (_map.TryGetValue(key, out var list))
            {
                for (int i = list.Count - 1; i >= 0; i--)
                    if (!list[i].IsAlive) list.RemoveAt(i);
                if (list.Count == 0) _map.Remove(key);
            }
        }
    }

    // ---- 디버깅/계측(선택) ----
    public static int Count(Type t, object scope = null)
    {
        lock (_gate)
            return _map.TryGetValue((t, scope), out var list) ? list.Count : 0;
    }
}