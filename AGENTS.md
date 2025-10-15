# Goal
Create a **deterministic Entity-Component-World architecture** for a game project by defining two key components:
- `EntityData`: the atomic unit representing all state of a single entity.
- `TheWorld`: a deterministic container managing and updating all `EntityData` instances each frame.

The architecture must ensure **full determinism**, **reproducibility**, and **separation of simulation logic from Unity’s rendering layer**.

---

# Context
- `EntityData` represents the **atomic state** of the world. Every game object or agent exists as structured data inside the simulation layer, not as a Unity `GameObject`.  
- Unity’s `Transform`, `Rigidbody`, and other components are **view-only copies** for visualization and debugging.  
- The **true source of world state** lives entirely inside the deterministic engine.

### Layers:
1. **Simulation Layer**  
   - Owns `EntityData` and `TheWorld`.  
   - Executes deterministic system updates (e.g., movement, collision, effects).  
   - Maintains strict order of updates each frame.

2. **System (Processor) Layer**  
   - Composed of specialized processors like `MovementSystem`, `CollisionSystem`, `EffectSystem`.  
   - Each takes `TheWorld` as input and outputs a modified version.  
   - All systems are pure functions (no side effects, no randomness, fixed update order).  

3. **Debug / Unity Bridge Layer**  
   - Unity scene reads data from `TheWorld` to render gizmos, meshes, or effects.  
   - Maps `EntityID` to Unity GameObjects for visualization.  
   - Performs no logic that can affect the simulation.

# Input
The system requires:
- A **struct or record** defining `EntityData`, containing every necessary state component (position, velocity, rotation, health, etc.).
- A **class** defining `TheWorld`, responsible for:
  - Holding all entity data.
  - Providing deterministic access/update functions.
  - Executing a fixed update loop through registered systems.
- A **System pipeline**, where each processor modifies `TheWorld` deterministically.
- An optional **Unity bridge layer** for debugging and visualization.

---

# Output
Generate code that:
1. Defines a deterministic `EntityData` structure that encapsulates all per-entity state.
2. Defines a `TheWorld` container that:
   - Stores all entities in deterministic collections.
   - Provides read/write APIs for systems.
   - Runs an `UpdateWorld()` method that calls systems in fixed order.
3. Provides a deterministic system registration and execution mechanism.
4. Optionally outlines the Unity Bridge (view layer) that:
   - Reads data from `TheWorld` to update GameObjects or draw gizmos.
   - Never modifies simulation data.

The final design should be ready to plug into a lockstep or replayable simulation core.

---

# Constraints
- All math operations use **fixed-point arithmetic** (no floating-point).  
- Systems must be **pure and deterministic** — identical input yields identical output.  
- No direct calls to Unity physics, transforms, or random functions in the simulation layer. Use pre-defined types(FixedVector2, HitCircle...) or define a new type if required.
- `TheWorld` state must be **serializable** for replay or rollback.  
- Simulation update order must be **fixed and centralized** (e.g., via `BattleCore.Ticker.OnTick`).
- Unity objects must only **read** data from `TheWorld`; they cannot write back.

---

# Procedure
1. **Define EntityData**
   - Create a struct that includes all components representing the entity’s full logical state.
   - Ensure all fields use deterministic numeric types.

2. **Implement TheWorld**
   - Create a container class that manages all `EntityData`.
   - Include deterministic creation, retrieval, and mutation of entities.
   - Maintain entity IDs and indexing in a stable, predictable way.

3. **Implement Systems**
   - Each system receives `TheWorld` as input and returns it modified.
   - Systems execute in a strict, predefined sequence.
   - No system should depend on Unity components or global mutable state.

4. **Design Update Loop**
   - Replace Unity’s `Update()` with a deterministic `Ticker.OnTick()` that calls `TheWorld.UpdateWorld()`.
   - Ensure consistent time-step simulation.

5. **Implement Debug Layer**
   - Map `EntityID` to Unity GameObjects or visual gizmos.
   - Draw hitboxes, positions, and movement paths using Unity for debugging.
   - Never write back to `TheWorld`—it remains read-only from this layer.

6. **Verify Determinism**
   - Test by serializing and deserializing world states.
   - Run identical simulations and confirm binary-equivalent results across runs.

---

# Result
A fully deterministic and simulation-driven game architecture where:
- **`EntityData`** = atomic state container (true game state).
- **`TheWorld`** = deterministic container and update orchestrator.
- **Systems** = pure state transformers.
- **Unity** = visualization-only viewer.  
This structure ensures **reproducibility**, **rollback capability**, and **perfect simulation consistency** across all environments.