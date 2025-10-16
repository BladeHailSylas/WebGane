using Intents;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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
	private static TheWorld _world;
	
	static BattleCore()
	{
		Initialize();
		_world = new TheWorld();
	}

	/// <summary>
	/// Global ticker operating with 60 ticks per real-time second.
	/// </summary>
	public static Ticker Ticker { get; } = new();

	public static IntentValidator Validator { get; } = new();

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
public readonly struct FixedVector2 : IEquatable<FixedVector2>
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

	public bool Equals(FixedVector2 other)
	{
		return _rawX == other._rawX && _rawY == other._rawY;
	}

	public override bool Equals(object obj)
	{
		return obj is FixedVector2 other && Equals(other);
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(_rawX, _rawY);
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
	public FixedVector2 center;
	public FixedVector2 halfSize;

	public HitBox(FixedVector2 center, FixedVector2 halfSize)
	{
		this.center = center;
		this.halfSize = halfSize;
	}

	public readonly int MinX => center.RawX - halfSize.RawX;
	public readonly int MaxX => center.RawX + halfSize.RawX;
	public readonly int MinY => center.RawY - halfSize.RawY;
	public readonly int MaxY => center.RawY + halfSize.RawY;

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
		long dx = Math.Abs((long)center.RawX - box.center.RawX);
		long dy = Math.Abs((long)center.RawY - box.center.RawY);
		long limitX = (long)halfSize.RawX + box.halfSize.RawX;
		long limitY = (long)halfSize.RawY + box.halfSize.RawY;
		bool separated = dx > limitX || dy > limitY;
		return !separated;
	}

	public readonly bool Overlaps(HitCircle circle)
	{
		int clampedX = Mathf.Clamp(circle.center.RawX, MinX, MaxX);
		int clampedY = Mathf.Clamp(circle.center.RawY, MinY, MaxY);

		long dx = circle.center.RawX - clampedX;
		long dy = circle.center.RawY - clampedY;
		long radius = circle.radius;
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
	public FixedVector2 center;
	public int radius; // in fixed units

	public HitCircle(FixedVector2 center, int radius)
	{
		this.center = center;
		this.radius = Math.Max(0, radius);
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
		long radii = (long)radius + circle.radius;
		return FixedVector2.DistanceSquared(center, circle.center) <= radii * radii;
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
		FixedVector2 diff = b.center - a.center;
		long distSq = (long)diff.RawX * diff.RawX + (long)diff.RawY * diff.RawY;
		long radii = (long)a.radius + b.radius;
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
		int clampedX = Math.Max(box.MinX, Math.Min(circle.center.RawX, box.MaxX));
		int clampedY = Math.Max(box.MinY, Math.Min(circle.center.RawY, box.MaxY));
		FixedVector2 closest = new FixedVector2(clampedX, clampedY);
		FixedVector2 diff = circle.center - closest;

		long distSq = (long)diff.RawX * diff.RawX + (long)diff.RawY * diff.RawY;
		if (distSq > (long)circle.radius * circle.radius)
			return null;

		double dist = Math.Sqrt(distSq);
		double depth = circle.radius - dist;
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
	public FixedVector2 position;
	public float rotation;

	public CoreTransform(FixedVector2 position, float rotation = 0f)
	{
		this.position = position;
		this.rotation = rotation;
	}

	public readonly Vector3 ToVector3(float z = 0f)
	{
		Vector2 pos2 = position.ToVector2();
		return new Vector3(pos2.x, pos2.y, z);
	}

	public readonly void ApplyTo(Transform transform)
	{
		if (!transform)
		{
			return;
		}

		Vector2 pos2 = position.ToVector2();
		Vector3 target = new(pos2.x, pos2.y, transform.position.z);
		transform.position = target;
		transform.rotation = Quaternion.Euler(0f, 0f, rotation);
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
	public CoreTransform coreTransform;

	[Tooltip("Automatically initializes the BattleCore singleton if necessary.")]
	public bool autoInitializeBattleCore = true;

	private void Awake()
	{
		if (autoInitializeBattleCore)
		{
			BattleCore.Initialize();
		}

		/** Optional: Enable to copy from Unity Transform on Awake for editor previews. */
		// CoreTransform = CoreTransform.FromTransform(transform);
	}

	private void LateUpdate()
	{
		coreTransform.ApplyTo(transform);
	}

	public void SyncFromUnityTransform()
	{
		coreTransform = CoreTransform.FromTransform(transform);
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
			try
			{
				_ticker.Step();
			}
			catch (TickCountOverflowException ex)
			{
				
			}
			yield return interval;
		}
		
	}
}

/// <summary>
/// Fixed-rate ticker that publishes a tick event 60 times per second.
/// </summary>
public sealed class Ticker
{
	public const byte TicksPerSecond = 60;
	public const byte TickIntervalMs = 1000 / TicksPerSecond;

	public event Action<ushort> OnTick;

	public ushort TickCount { get; private set; }

	public void Schedule(byte ticksFromNow, Action<int> action) //ticksFromNow is byte since max delay is 120 ticks(2 seconds), the smaller the better for memory and packet size
	{
		if (ticksFromNow <= 0)
		{
			throw new ArgumentOutOfRangeException(nameof(ticksFromNow), "Must be greater than zero.");
		}
		ushort targetTick = (TickCount + ticksFromNow < TickCount) ? (ushort)(TickCount + ticksFromNow) : (ushort)1;
		OnTick += Handler;
		return;

		void Handler(ushort currentTick)
		{
			
			if (currentTick < targetTick) return;
			action(currentTick);
			OnTick -= Handler;
		}
	}
	public void Reset() => TickCount = 0;

	// Deterministic: "한번 호출할 때마다 정확히 한 틱"만 진행
	public void Step()
	{
		TickCount++;
		if (TickCount == 65535) // wrap around to avoid overflow, though unlikely to happen in practice(it needs a battle that lasts more than 18 minutes)
		{
			TickCount = 0;
			throw new TickCountOverflowException("It seems ushort was too short");
		}
		OnTick?.Invoke(TickCount);
	}
}
public class TickCountOverflowException : Exception
{
	public TickCountOverflowException()
	{
		
	}

	public TickCountOverflowException(string msg) : base(msg)
	{
		
	}
}
#endregion

#region ===== Intent Validate =====
/// <summary>
/// This filters the available intents
/// </summary>
public sealed class IntentValidator
{
	private List<IIntent> _validIntents;
	private ushort[] _immovableIDs;
	private ushort[] _unattackableIDs;
	public IIntent[] ValidatedIntents => _validIntents.ToArray();
	public void GetFlush(IIntent[] intents)
	{
		_validIntents.Clear();
		foreach (var intent in intents.Where(intent => !(_immovableIDs.Contains(intent.OwnerID) || _unattackableIDs.Contains(intent.OwnerID))))
		{
			if(intent.Type == IntentType.Move || intent.Type == IntentType.Cast) _validIntents.Add(intent);
		}
	}
}
#endregion

#region ===== Battle Module =====
public static class BattleModule
{
	/*public static void ResolvePriority(params CastIntent[] intents)
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
	}*/
	// Placeholder for future battle-related utilities and systems.
}
#endregion