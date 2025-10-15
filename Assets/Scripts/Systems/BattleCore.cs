using Intents;
using System;
using System.Collections;
using UnityEditor;
using UnityEngine;
using static UnityEditor.ShaderData;

#region ===== Core =====
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
		Debug.Log("BattleCore initialized.");
	}
}
#endregion

#region ===== Fixed Vector2 =====
/// <summary>
/// Represents a deterministic 2D vector where 1.0f equals 1000 fixed units.
/// </summary>
[Serializable]
public readonly struct FixedVector2
{
	public const int UnitsPerFloat = 1000;

	[SerializeField] readonly int _rawX;
	[SerializeField] readonly int _rawY;

	/// <summary>
	/// Raw X component (in fixed units).
	/// </summary>
	public int RawX => _rawX;

	/// <summary>
	/// Raw Y component (in fixed units).
	/// </summary>
	public int RawY => _rawY;
	/// <summary>
	/// Normalized vector (unit length). Returns (0,0) if the vector is zero.
	/// </summary>
	public FixedVector2 Normalized 
	{ 
		get
		{
			double length = Math.Sqrt(RawX * RawX + RawY * RawY);
			if (length < 1e-6)
			{
				return new FixedVector2(0, 0);
			}
			return new FixedVector2((int)(RawX / length), (int)(RawY / length));
		}
	}
	/// <summary>
	/// The size (magnitude) of the vector in fixed units.
	/// </summary>
	public int Magnitude => (int)Math.Sqrt(RawX * RawX + RawY * RawY);

	public FixedVector2(int rawX, int rawY)
	{
		_rawX = rawX;
		_rawY = rawY;
	}

	public FixedVector2(float x, float y)
	{
		_rawX = (int)Math.Round(x * UnitsPerFloat);
		_rawY = (int)Math.Round(y * UnitsPerFloat);
	}

	public FixedVector2(Vector2 vector)
	{
		_rawX = (int)Math.Round(vector.x * UnitsPerFloat);
		_rawY = (int)Math.Round(vector.y * UnitsPerFloat);
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
	public static int DistanceSquared(FixedVector2 a, FixedVector2 b)
	{
		long dx = (long)a._rawX - b._rawX;
		long dy = (long)a._rawY - b._rawY;
		return (int)Math.Sqrt(dx * dx + dy * dy);
	}
}

#endregion

#region ===== Hit & Collision =====
public interface IHitShape
{
	bool Overlaps(IHitShape shape);
	IHitShape[] OverlapShapes();
}
/// <summary>
/// Axis-aligned box expressed in fixed-space coordinates.
/// </summary>
[Serializable]
public struct HitBox : IHitShape
{
	public FixedVector2 Center;
	public FixedVector2 HalfSize;

	public HitBox(FixedVector2 center, FixedVector2 halfSize)
	{
		Center = center;
		HalfSize = halfSize;
	}

	public readonly int MinX => Center.RawX - HalfSize.RawX;
	public readonly int MaxX => Center.RawX + HalfSize.RawX;
	public readonly int MinY => Center.RawY - HalfSize.RawY;
	public readonly int MaxY => Center.RawY + HalfSize.RawY;

	public readonly bool Overlaps(IHitShape shape)
	{
		return shape switch
		{
			HitBox box => Overlaps(box),
			HitCircle circle => Overlaps(circle),
			_ => throw new NotSupportedException($"Unsupported shape type: {shape.GetType().Name}"),
		};
	}
	public readonly bool Overlaps(HitBox box)
	{
		long dx = Math.Abs((long)Center.RawX - box.Center.RawX);
		long dy = Math.Abs((long)Center.RawY - box.Center.RawY);
		long limitX = (long)HalfSize.RawX + box.HalfSize.RawX;
		long limitY = (long)HalfSize.RawY + box.HalfSize.RawY;
		bool separated = dx > limitX || dy > limitY;
		return !separated;
	}

	public readonly bool Overlaps(HitCircle circle)
	{
		int clampedX = Mathf.Clamp(circle.Center.RawX, MinX, MaxX);
		int clampedY = Mathf.Clamp(circle.Center.RawY, MinY, MaxY);

		long dx = circle.Center.RawX - clampedX;
		long dy = circle.Center.RawY - clampedY;
		long radius = circle.Radius;
		return (dx * dx + dy * dy) <= radius * radius;
	}
	public readonly IHitShape[] OverlapShapes()
	{
		return new IHitShape[] { this };
	}
}

/// <summary>
/// Circle expressed in fixed-space coordinates.
/// </summary>
[Serializable]
public struct HitCircle : IHitShape
{
	public FixedVector2 Center;
	public int Radius; // in fixed units

	public HitCircle(FixedVector2 center, int radius)
	{
		Center = center;
		Radius = Math.Max(0, radius);
	}
	public readonly bool Overlaps(IHitShape shape)
	{
		return shape switch
		{
			HitBox box => Overlaps(box),
			HitCircle circle => Overlaps(circle),
			_ => throw new NotSupportedException($"Unsupported shape type: {shape.GetType().Name}"),
		};
	}
	public readonly bool Overlaps(HitCircle circle)
	{
		long radii = (long)Radius + circle.Radius;
		return FixedVector2.DistanceSquared(Center, circle.Center) <= radii * radii;
	}

	public readonly bool Overlaps(HitBox box)
	{
		return box.Overlaps(this);
	}
	public readonly IHitShape[] OverlapShapes()
	{
		return new IHitShape[] { this };
	}

}
public sealed class FixedCollision
{
	public static bool CheckOverlap(IHitShape shape1, IHitShape shape2)
	{
		return shape1 switch
		{
			HitCircle circle1 when shape2 is HitCircle circle2 => circle1.Overlaps(circle2),
			HitCircle circle when shape2 is HitBox box => box.Overlaps(circle),
			HitBox box when shape2 is HitCircle circle => box.Overlaps(circle),
			HitBox box1 when shape2 is HitBox box2 => box1.Overlaps(box2),
			_ => false,
		};
	}
	/// <summary>
	/// 두 충돌체 사이의 법선(normal)과 침투 깊이(depth)를 계산합니다.
	/// 겹치지 않은 경우 null을 반환합니다.
	/// </summary>
	public static ContactInfo? ComputeContact(IHitShape shape1, IHitShape shape2)
	{
		switch (shape1)
		{
			case HitCircle a when shape2 is HitCircle b:
				return ComputeCircleCircle(a, b);
			case HitCircle a when shape2 is HitBox b:
				return ComputeCircleBox(a, b);
			default:
				return null;
		}
	}

	private static ContactInfo? ComputeCircleCircle(HitCircle a, HitCircle b)
	{
		FixedVector2 diff = b.Center - a.Center;
		long distSq = (long)diff.RawX * diff.RawX + (long)diff.RawY * diff.RawY;
		long radii = (long)a.Radius + b.Radius;
		long radiiSq = radii * radii;
		if (distSq >= radiiSq)
			return null;

		double dist = Math.Sqrt(distSq);
		double depth = radii - dist;
		FixedVector2 normal = (dist > 1e-6)
			? new FixedVector2((int)(diff.RawX / dist), (int)(diff.RawY / dist))
			: new FixedVector2(0, 0);

		return new ContactInfo
		{
			normal = normal,
			depth = (int)Math.Round(depth),
			owner = b
		};
	}

	private static ContactInfo? ComputeCircleBox(HitCircle circle, HitBox box)
	{
		int clampedX = Math.Max(box.MinX, Math.Min(circle.Center.RawX, box.MaxX));
		int clampedY = Math.Max(box.MinY, Math.Min(circle.Center.RawY, box.MaxY));
		FixedVector2 closest = new FixedVector2(clampedX, clampedY);
		FixedVector2 diff = circle.Center - closest;

		long distSq = (long)diff.RawX * diff.RawX + (long)diff.RawY * diff.RawY;
		if (distSq > (long)circle.Radius * circle.Radius)
			return null;

		double dist = Math.Sqrt(distSq);
		double depth = circle.Radius - dist;
		FixedVector2 normal = (dist > 1e-6)
			? new FixedVector2((int)(diff.RawX / dist), (int)(diff.RawY / dist))
			: new FixedVector2(0, 0);

		return new ContactInfo
		{
			normal = normal,
			depth = (int)Math.Round(depth),
			owner = box
		};
	}
}
public struct ContactInfo
{
	public FixedVector2 normal; // 침투 방향 (정규화)
	public int depth;           // 침투 깊이 (fixed 단위)
	public object owner;        // 충돌체 소유자 (선택적)
}
#endregion

#region ===== Transform =====
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

	public readonly Vector3 ToVector3(float z = 0f)
	{
		Vector2 pos2 = Position.ToVector2();
		return new Vector3(pos2.x, pos2.y, z);
	}

	public readonly void ApplyTo(Transform transform)
	{
		if (!transform)
		{
			return;
		}

		Vector2 pos2 = Position.ToVector2();
		Vector3 target = new(pos2.x, pos2.y, transform.position.z);
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
#endregion

#region ===== Ticker =====
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
		_ticker ??= BattleCore.Ticker;
		StartCoroutine(TickLoop());
	}

	IEnumerator TickLoop()
	{
		var interval = new WaitForSecondsRealtime(1f / Ticker.TicksPerSecond);
		while (true)
		{
			_ticker.Step();
			yield return interval;
		}
	}
}

/// <summary>
/// Fixed-rate ticker that publishes a tick event 60 times per second.
/// </summary>
public sealed class Ticker
{
	public const int TicksPerSecond = 60;
	public const int TickIntervalMs = 1000 / TicksPerSecond;

	public event Action<int> OnTick;

	private ushort _tickCount;
	public ushort TickCount => _tickCount;

	public void Reset() => _tickCount = 0;

	// Deterministic: "한번 호출할 때마다 정확히 한 틱"만 진행
	public void Step()
	{
		_tickCount++;
		if(_tickCount % TicksPerSecond == 0)
		{
			Debug.Log($"Tick {_tickCount} at {Time.realtimeSinceStartup:F3}s"); // Time.realtimeSinceStartup is just for debugging, not used for real timing. IT IS NOT QUITE DETERMINISTIC
		}
		if (_tickCount == 65535) // wrap around to avoid overflow, though unlikely to happen in practice(it needs a battle that lasts more than 18 minutes)
		{
			Debug.LogError("Overflow has occurred. Perhaps ushort is too short...");
			_tickCount = 0;
		}
		OnTick?.Invoke(_tickCount);
	}
}
#endregion

#region ===== Battle Module =====
public static class BattleModule
{
	public static void ResolvePriority(params CastIntent[] intents)
	{
		CastIntent best = null;
		// Simple priority resolution example (to be replaced with actual logic)
		foreach (var intent in intents)
		{
			if(best == null || intent.PriorityLevel > best.PriorityLevel)
			{
				best = intent;
			}
			// Placeholder for priority resolution logic.
			Debug.Log($"Resolving priority for intent: {intent}");
		}
		if(best != null)
		{
			Debug.Log($"Best intent: {best}");
		}
	}
	// Placeholder for future battle-related utilities and systems.
}
#endregion