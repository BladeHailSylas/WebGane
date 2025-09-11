using System;
using System.Collections.Generic;
using UnityEngine;
using StatsInterfaces;
public static class EventBus
{
    static readonly Dictionary<Type, Delegate> _map = new();

    public static void Subscribe<T>(Action<T> handler)
    {
        if (!_map.TryGetValue(typeof(T), out var d)) _map[typeof(T)] = handler;
        else _map[typeof(T)] = Delegate.Combine(d, handler);
    }

    public static void Unsubscribe<T>(Action<T> handler)
    {
        if (_map.TryGetValue(typeof(T), out var d))
        {
            d = Delegate.Remove(d, handler);
            if (d == null) _map.Remove(typeof(T));
            else _map[typeof(T)] = d;
        }
    }

    public static void Publish<T>(T evt)
    {
        if (_map.TryGetValue(typeof(T), out var d))
            (d as Action<T>)?.Invoke(evt);
    }
}
public readonly struct CastStarted { public readonly Transform Caster; public readonly object Skill; public CastStarted(Transform t, object s) { Caster = t; Skill = s; } }
public readonly struct CastEnded { public readonly Transform Caster; public readonly object Skill; public CastEnded(Transform t, object s) { Caster = t; Skill = s; } }
public readonly struct DamageDealt { public readonly Transform Attacker, Target; public readonly float RawDamage; public DamageDealt(Transform a, Transform t, float dmg) { Attacker = a; Target = t; RawDamage = dmg; } }
public readonly struct BuffApplyReq { public readonly Transform Target; public readonly IStatModifier Mod; public readonly float Duration; public BuffApplyReq(Transform t, IStatModifier m, float dur) { Target = t; Mod = m; Duration = dur; } }
public readonly struct BuffRemoveReq { public readonly Transform Target; public readonly IStatModifier Mod; public BuffRemoveReq(Transform t, IStatModifier m) { Target = t; Mod = m; } }