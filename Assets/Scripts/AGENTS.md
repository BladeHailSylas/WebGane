# Goal
Design a simplified and maintainable **IntentRouter** system to replace the overly complex **IntentOrchestrator** and **CastIntent** architecture, focusing on minimal routing behavior for modular extensibility.

# Context
The current `IntentOrchestrator` and `CastIntent` structures were built to support complex, skill-based intent pipelines involving multiple chained executions, follow-ups, and RNG.  
However, the new design goal is **simplicity** and **deterministic intent routing** for a single-module environment (`CoreMotor2D`), starting with only `MoveIntent`.

Thus, the following simplifications are applied:

- **Rename**: `IntentOrchestrator → IntentRouter`
- **Reduce responsibility**: Only routes intents to modules based on their type.
- **Remove complexity**: Eliminate `CastIntent`, `CastContext`, `FollowUp`, `GuardKey`, and `DedupKey`.
- **Preserve determinism**: Keep routing order predictable and per-tick consistent.
- **Keep IntentCollector**: It remains responsible for gathering intents per tick and passing them to the router.

# Input
- **Previous system files**:
    - `IntentOrchestrator.cs`: A monolithic orchestrator handling scheduling, deduplication, and validation.
    - `IntentTypes.cs`: Defines complex intent data (`CastIntent`, `FollowUpTemplate`, etc.).
    - `IntentCollector.cs`: Already designed to collect and flush intents per tick.

- **Requirements**:
    1. Create a new, modular `IntentRouter` that:
        - Accepts an array or list of `IIntent` from `IntentCollector`.
        - Routes each intent to its target subsystem based on `IntentType`.
    2. Initially support only `MoveIntent`.
    3. Retain clear extension points for future intents (e.g., `SkillIntent`).
    4. Enforce deterministic operation (no randomization, no async, no coroutines).

# Output
A simplified architecture:
- `IntentRouter`: Central routing component.
- `IIntent` and its implementations (`MoveIntent`, etc.) remain minimal.
- `IntentCollector`: Unchanged, responsible for gathering intents.
- No CastIntent or CastContext logic remains.

System data flow per tick:  
`IntentCollector → BattleCore → IntentRouter → Target Module (CoreMotor2D)`

# Constraints
- **Deterministic**: Behavior must be identical for the same input sequence.
- **Single responsibility**: IntentRouter only routes; does not validate, queue, or schedule.
- **No dependencies**: Avoid coupling with gameplay or RNG systems.
- **Extensible**: Must allow future addition of new Intent types via clean branching logic.
- **Minimal runtime overhead**: Operates per-tick with predictable performance.

# Procedure
1. **Deprecate and remove**:
    - `IntentOrchestrator.cs` (and its MonoBehaviour dependency).
    - `CastIntent`, `CastContext`, and all related types from `IntentTypes.cs`.

2. **Introduce `IntentRouter`**:
    - A pure C# class that takes a list of `IIntent` and routes them by intent type.
    - Example routing flow:
        - `MoveIntent` → Calls `CoreMotor2D.Move()`.
        - Unrecognized types → Log a warning, skip processing.

3. **Keep `IntentCollector`**:
    - Continue using it to gather all `IIntent` objects each tick.
    - Pass the collected list to `IntentRouter` during the fixed update or tick event.

4. **Integrate deterministically**:
    - Each tick executes the routing step exactly once.
    - Ensure that all intents are processed in the same order they were collected.

5. **Future expansion**:
    - Extend with `SkillIntent`, `InteractionIntent`, etc.
    - Add modular handler methods or a handler registry in `IntentRouter`.
    - Maintain isolation: each handler should only touch its own subsystem.

---

✅ **End Result:**
A clean, deterministic intent handling flow:
- **Simple:** Only one router; no orchestration logic.
- **Extensible:** Future intents can be added via straightforward routing.
- **Maintainable:** Minimal complexity, no unnecessary data objects.
- **Deterministic:** Predictable per-tick intent behavior for all actors.