using System;
using UnityEngine;

/// <summary>
/// Provides the foundational global systems for the battle simulation layer.
/// Ensure this class is touched before using other BattleCore utilities to
/// guarantee that the internal runner MonoBehaviour exists.
/// </summary>
public static class BattleCore
{
    private static bool _initialized;
    private static GameObject _runner;

    static BattleCore()
    {
        Initialize();
    }

    /// <summary>
    /// Global ticker operating with 60 ticks per real-time second.
    /// </summary>
    public static Ticker Ticker { get; } = new();

    /// <summary>
    /// Creates an invisible runner GameObject (if required) and keeps it alive across scenes.
    /// </summary>
    public static void Initialize()
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;
        _runner = new GameObject("[BattleCore]")
        {
            hideFlags = HideFlags.HideAndDontSave
        };
        UnityEngine.Object.DontDestroyOnLoad(_runner);

        var runner = _runner.AddComponent<BattleCoreTickerRunner>();
        runner.Initialize(Ticker);
    }
}

/// <summary>
/// Represents a deterministic 2D vector where 1.0f equals 1000 fixed units.
/// </summary>
[Serializable]
public readonly struct FixedVector2
{
    public const int UnitsPerFloat = 1000;

    [SerializeField] private readonly int _rawX;
    [SerializeField] private readonly int _rawY;

    /// <summary>
    /// Raw X component (in fixed units).
    /// </summary>
    public int RawX => _rawX;

    /// <summary>
    /// Raw Y component (in fixed units).
    /// </summary>
    public int RawY => _rawY;

    public FixedVector2(int rawX, int rawY)
    {
        _rawX = rawX;
        _rawY = rawY;
    }

    public FixedVector2(float x, float y)
    {
        _rawX = Mathf.RoundToInt(x * UnitsPerFloat);
        _rawY = Mathf.RoundToInt(y * UnitsPerFloat);
    }

    public FixedVector2(Vector2 vector)
    {
        _rawX = Mathf.RoundToInt(vector.x * UnitsPerFloat);
        _rawY = Mathf.RoundToInt(vector.y * UnitsPerFloat);
    }

    /// <summary>
    /// Converts to the Unity floating-point representation.
    /// </summary>
    public Vector2 ToVector2()
    {
        return new Vector2(_rawX / (float)UnitsPerFloat, _rawY / (float)UnitsPerFloat);
    }

    public static FixedVector2 FromVector2(Vector2 vector)
    {
        return new FixedVector2(vector);
    }

    public static FixedVector2 operator +(FixedVector2 a, FixedVector2 b)
    {
        return new FixedVector2(a._rawX + b._rawX, a._rawY + b._rawY);
    }

    public static FixedVector2 operator -(FixedVector2 a, FixedVector2 b)
    {
        return new FixedVector2(a._rawX - b._rawX, a._rawY - b._rawY);
    }

    public static FixedVector2 operator -(FixedVector2 value)
    {
        return new FixedVector2(-value._rawX, -value._rawY);
    }

    public override string ToString()
    {
        return $"({ToVector2().x:F3}, {ToVector2().y:F3})";
    }

    /// <summary>
    /// Squared distance between two fixed vectors in squared fixed units.
    /// </summary>
    public static long DistanceSquared(FixedVector2 a, FixedVector2 b)
    {
        long dx = (long)a._rawX - b._rawX;
        long dy = (long)a._rawY - b._rawY;
        return dx * dx + dy * dy;
    }
}

/// <summary>
/// Axis-aligned box expressed in fixed-space coordinates.
/// </summary>
[Serializable]
public struct HitBox
{
    public FixedVector2 Center;
    public FixedVector2 HalfSize;

    public HitBox(FixedVector2 center, FixedVector2 halfSize)
    {
        Center = center;
        HalfSize = halfSize;
    }

    public int MinX => Center.RawX - HalfSize.RawX;
    public int MaxX => Center.RawX + HalfSize.RawX;
    public int MinY => Center.RawY - HalfSize.RawY;
    public int MaxY => Center.RawY + HalfSize.RawY;

    public bool Overlaps(HitBox other)
    {
        long dx = Math.Abs((long)Center.RawX - other.Center.RawX);
        long dy = Math.Abs((long)Center.RawY - other.Center.RawY);
        long limitX = (long)HalfSize.RawX + other.HalfSize.RawX;
        long limitY = (long)HalfSize.RawY + other.HalfSize.RawY;
        bool separated = dx > limitX || dy > limitY;
        return !separated;
    }

    public bool Overlaps(HitCircle circle)
    {
        int clampedX = Mathf.Clamp(circle.Center.RawX, MinX, MaxX);
        int clampedY = Mathf.Clamp(circle.Center.RawY, MinY, MaxY);

        long dx = circle.Center.RawX - clampedX;
        long dy = circle.Center.RawY - clampedY;
        long radius = circle.Radius;
        return (dx * dx + dy * dy) <= radius * radius;
    }
}

/// <summary>
/// Circle expressed in fixed-space coordinates.
/// </summary>
[Serializable]
public struct HitCircle
{
    public FixedVector2 Center;
    public int Radius; // in fixed units

    public HitCircle(FixedVector2 center, int radius)
    {
        Center = center;
        Radius = Mathf.Max(0, radius);
    }

    public bool Overlaps(HitCircle other)
    {
        long radii = (long)Radius + other.Radius;
        return FixedVector2.DistanceSquared(Center, other.Center) <= radii * radii;
    }

    public bool Overlaps(HitBox box)
    {
        return box.Overlaps(this);
    }
}

/// <summary>
/// Core transform that stores a deterministic position alongside an optional planar rotation.
/// </summary>
[Serializable]
public struct CoreTransform
{
    public FixedVector2 Position;
    public float Rotation;

    public CoreTransform(FixedVector2 position, float rotation = 0f)
    {
        Position = position;
        Rotation = rotation;
    }

    public Vector3 ToVector3(float z = 0f)
    {
        Vector2 pos2 = Position.ToVector2();
        return new Vector3(pos2.x, pos2.y, z);
    }

    public void ApplyTo(Transform transform)
    {
        if (!transform)
        {
            return;
        }

        Vector2 pos2 = Position.ToVector2();
        Vector3 target = new Vector3(pos2.x, pos2.y, transform.position.z);
        transform.position = target;
        transform.rotation = Quaternion.Euler(0f, 0f, Rotation);
    }

    public static CoreTransform FromTransform(Transform transform)
    {
        if (!transform)
        {
            return default;
        }

        Vector3 pos = transform.position;
        return new CoreTransform(new FixedVector2(pos.x, pos.y), transform.eulerAngles.z);
    }
}

/// <summary>
/// Keeps a Unity Transform in sync with a deterministic CoreTransform.
/// </summary>
[DisallowMultipleComponent]
public sealed class TransformSync : MonoBehaviour
{
    [Tooltip("Deterministic transform data that should be mirrored to the Unity Transform.")]
    public CoreTransform CoreTransform;

    [Tooltip("Automatically initializes the BattleCore singleton if necessary.")]
    public bool AutoInitializeBattleCore = true;

    private void Awake()
    {
        if (AutoInitializeBattleCore)
        {
            BattleCore.Initialize();
        }

        /** Optional: Enable to copy from Unity Transform on Awake for editor previews. */
        // CoreTransform = CoreTransform.FromTransform(transform);
    }

    private void LateUpdate()
    {
        CoreTransform.ApplyTo(transform);
    }

    public void SyncFromUnityTransform()
    {
        CoreTransform = CoreTransform.FromTransform(transform);
    }
}

/// <summary>
/// Internal runner that bridges Unity's Update loop to the deterministic ticker.
/// </summary>
internal sealed class BattleCoreTickerRunner : MonoBehaviour
{
    private Ticker _ticker;

    public void Initialize(Ticker ticker)
    {
        _ticker = ticker;
    }

    private void Awake()
    {
        if (_ticker == null)
        {
            _ticker = BattleCore.Ticker;
        }
    }

    private void Update()
    {
        _ticker?.Step(Time.deltaTime);
    }
}

/// <summary>
/// Fixed-rate ticker that publishes a tick event 60 times per second.
/// </summary>
public sealed class Ticker
{
    public const int TicksPerSecond = 60;

    public event Action<int> OnTick;

    private float _accumulator;
    private int _tickCount;

    public int TickCount => _tickCount;

    public void Reset()
    {
        _accumulator = 0f;
        _tickCount = 0;
    }

    public void Step(float deltaTime)
    {
        if (deltaTime <= 0f)
        {
            return;
        }

        _accumulator += deltaTime;
        float interval = 1f / TicksPerSecond;

        while (_accumulator >= interval)
        {
            _accumulator -= interval;
            _tickCount++;
            OnTick?.Invoke(_tickCount);
        }
    }
}
