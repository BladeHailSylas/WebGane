using System;
using System.Collections.Generic;

/// <summary>
/// Pure deterministic system contract. Implementations must avoid random generators,
/// floating point math, or Unity API access to guarantee replay stability.
/// </summary>
public interface IWorldSystem
{
	string Name { get; }
	void Execute(TheWorld world);
}

/// <summary>
/// Serializable snapshot of the entire world state. Used for rollback, save/load,
/// or deterministic verification between peers.
/// </summary>
[Serializable]
public struct WorldSnapshot
{
	public int Tick;
	public ulong WorldVersion;
	public EntityData[] Entities;
}

/// <summary>
/// Deterministic container responsible for owning every <see cref="EntityData"/> instance.
/// The class enforces a strict update order, stable entity identifiers, and snapshot
/// capabilities that make lockstep or rollback netcode feasible.
/// </summary>
[Serializable]
public sealed class TheWorld
{
	[SerializeField] private readonly List<EntityData> _entities = new();
	[SerializeField] private readonly List<int> _freeIds = new();
	[NonSerialized] private readonly List<SystemRegistration> _systems = new();

	[SerializeField] private ulong _worldVersion;

	public int CurrentTick { get; private set; }
	public ulong WorldVersion => _worldVersion;
	public int ActiveEntityCount { get; private set; }

	/// <summary>
	/// Optional deterministic random access for the Unity bridge. The list reference must never be mutated externally.
	/// </summary>
	public IReadOnlyList<EntityData> Entities => _entities;

	/// <summary>
	/// Registers a deterministic system that will be executed each tick using the provided order.
	/// Lower order values run first. Registration is idempotent based on instance reference.
	/// </summary>
	public void RegisterSystem(IWorldSystem system, int order)
	{
		if (system == null)
		{
			throw new ArgumentNullException(nameof(system));
		}

		for (int i = 0; i < _systems.Count; i++)
		{
			if (ReferenceEquals(_systems[i].System, system))
			{
				return;
			}
		}

		_systems.Add(new SystemRegistration(order, system));
		_systems.Sort((a, b) => a.Order.CompareTo(b.Order));
	}

	/// <summary>
	/// Removes a previously registered system. Useful for deterministic scenario swapping.
	/// </summary>
	public bool UnregisterSystem(IWorldSystem system)
	{
		if (system == null)
		{
			return false;
		}

		for (int i = 0; i < _systems.Count; i++)
		{
			if (!ReferenceEquals(_systems[i].System, system))
			{
				continue;
			}
			_systems.RemoveAt(i);
			return true;
		}
		return false;
	}

	/// <summary>
	/// Spawns an entity by reserving a deterministic identifier and copying the template.
	/// The caller remains responsible for populating component fields before invoking this method.
	/// </summary>
	public EntityId SpawnEntity(EntityData template)
	{
		int index;
                if (_freeIds.Count > 0)
                {
                        int last = _freeIds.Count - 1;
                        index = _freeIds[last];
                        _freeIds.RemoveAt(last);
                }
		else
		{
			index = _entities.Count;
			_entities.Add(default);
		}

		template.Id = new EntityId(index);
		template.IsActive = true;
		template.Version++;
		template.LastProcessedTick = CurrentTick;
		_entities[index] = template;
		ActiveEntityCount++;
		_worldVersion++;
		return template.Id;
	}

	/// <summary>
	/// Marks an entity as inactive and recycles the identifier for future reuse.
	/// </summary>
	public bool DespawnEntity(EntityId id)
	{
		if (!TryGetEntity(id, out var entity) || !entity.IsActive)
		{
			return false;
		}

		entity.IsActive = false;
		entity.Version++;
		_entities[id.Value] = entity;
		ActiveEntityCount--;
		_freeIds.Add(id.Value);
		_worldVersion++;
		return true;
	}

	/// <summary>
	/// Attempts to fetch a copy of the entity. Use <see cref="WriteEntity"/> to commit mutations.
	/// </summary>
	public bool TryGetEntity(EntityId id, out EntityData entity)
	{
		if (!id.IsValid || id.Value >= _entities.Count)
		{
			entity = default;
			return false;
		}

		entity = _entities[id.Value];
		return entity.IsActive;
	}

	/// <summary>
	/// Commits an updated entity struct back into the world buffer. The identifier must remain unchanged.
	/// </summary>
	public void WriteEntity(EntityData entity)
	{
		if (!entity.Id.IsValid)
		{
			throw new ArgumentException("Entity must have a valid identifier before writing.", nameof(entity));
		}
		if (entity.Id.Value >= _entities.Count)
		{
			throw new IndexOutOfRangeException("Entity identifier exceeds buffer capacity.");
		}
		_entities[entity.Id.Value] = entity;
		_worldVersion++;
	}

	/// <summary>
	/// Executes the deterministic tick loop. Order of operations:
	/// 1) Integrate velocity for every active entity.
	/// 2) Invoke registered systems in ascending order.
	/// </summary>
	public void UpdateWorld()
	{
		CurrentTick++;

		for (int i = 0; i < _entities.Count; i++)
		{
			var entity = _entities[i];
			if (!entity.IsActive)
			{
				continue;
			}

			entity.LastProcessedTick = CurrentTick;
			entity.IntegrateMotion();
			entity.Version++;
			_entities[i] = entity;
		}

		for (int i = 0; i < _systems.Count; i++)
		{
			_systems[i].System.Execute(this);
		}

		_worldVersion++;
	}

	/// <summary>
	/// Produces a deep copy snapshot of the world buffer for deterministic rollback.
	/// </summary>
	public WorldSnapshot CreateSnapshot()
	{
		return new WorldSnapshot
		{
			Tick = CurrentTick,
			WorldVersion = _worldVersion,
			Entities = _entities.ToArray()
		};
	}

	/// <summary>
	/// Restores world state from a snapshot generated by <see cref="CreateSnapshot"/>.
	/// </summary>
	public void ApplySnapshot(WorldSnapshot snapshot)
	{
		if (snapshot.Entities == null)
		{
			throw new ArgumentException("Snapshot must contain entity data.", nameof(snapshot));
		}

		_entities.Clear();
		_entities.AddRange(snapshot.Entities);
		_freeIds.Clear();
		for (int i = 0; i < _entities.Count; i++)
		{
			if (!_entities[i].IsActive)
			{
				_freeIds.Add(i);
			}
		}

		CurrentTick = snapshot.Tick;
		_worldVersion = snapshot.WorldVersion;
		ActiveEntityCount = CountActiveEntities();
	}

	/// <summary>
	/// Returns a deterministic enumeration of active entity identifiers.
	/// </summary>
	public IEnumerable<EntityId> EnumerateActiveEntities()
	{
		for (int i = 0; i < _entities.Count; i++)
		{
			if (_entities[i].IsActive)
			{
				yield return new EntityId(i);
			}
		}
	}

	/// <summary>
	/// Clears all entities and resets counters. Intended for deterministic tests.
	/// </summary>
	public void Clear()
	{
		_entities.Clear();
		_freeIds.Clear();
		ActiveEntityCount = 0;
		CurrentTick = 0;
		_worldVersion = 0;
	}

	/// <summary>
	/// Unity bridge helper: fetches the latest entity copy for visualization without mutating state.
	/// </summary>
	public EntityData PeekEntity(EntityId id)
	{
		if (!TryGetEntity(id, out var entity))
		{
			throw new KeyNotFoundException($"Entity {id} is not active.");
		}
		return entity;
	}

	/// <summary>
	/// Calculates the deterministic number of active entities, used when restoring snapshots.
	/// </summary>
	private int CountActiveEntities()
	{
		int count = 0;
		for (int i = 0; i < _entities.Count; i++)
		{
			if (_entities[i].IsActive)
			{
				count++;
			}
		}
		return count;
	}

	/// <summary>
	/// Internal record describing a registered deterministic system.
	/// </summary>
	[Serializable]
	private readonly struct SystemRegistration
	{
		public readonly int Order;
		public readonly IWorldSystem System;

		public SystemRegistration(int order, IWorldSystem system)
		{
			Order = order;
			System = system;
		}
	}

	/** Optional debug hook: enable to dump the active entity list per tick.
	public bool VerboseLogging;
	*/
}
