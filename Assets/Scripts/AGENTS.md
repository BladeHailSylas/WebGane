# Goal
Refactor all code that uses `int` for `EntityId` to use `ushort` instead, reducing network packet size while maintaining system integrity and avoiding overflow or compatibility issues.

# Context
- The existing codebase defines `EntityId` as an `int` type.
- You want to change it to `ushort` to reduce bandwidth usage (smaller packet size).
- The total number of entities is small enough to safely fit within `ushort` (0–65535).
- You want to avoid risky underflow/overflow edge cases that could occur with `byte`.
- The code may involve serialization, deserialization, network transmission, and database or file persistence.

# Input
- The codebase where `EntityId` (currently an `int`) is used — including:
    - Class or struct definitions (`Entity`, `Component`, etc.)
    - Network serialization/deserialization code.
    - Packet and protocol structures.
    - Dictionaries, maps, and arrays keyed by `EntityId`.
    - Any numeric casts, arithmetic, or range checks involving `EntityId`.

# Output
A complete refactoring plan or code transformation that:
1. Replaces all occurrences of `int` used for entity identifiers with `ushort`.
2. Ensures compatibility in serialization/deserialization layers (convert between `ushort` and network byte order correctly).
3. Preserves API clarity (e.g., define a strong typedef/alias for `EntityId`).
4. Adds range assertions or compile-time checks to prevent overflow (e.g., entity count exceeding `ushort.MaxValue`).
5. Optionally includes a migration utility if persistence format or save files use `int`.

# Constraints
- Do **not** change unrelated `int` usages (only those representing `EntityId`).
- Ensure no implicit cast warnings or truncation errors occur.
- Maintain deterministic behavior in network or ECS logic.
- No output examples required.
- Code must remain compatible with existing networking or ECS architecture.

# Procedure
1. **Define a Type Alias:**  
   Create a typedef or alias, e.g.,
   ```csharp
   using EntityId = ushort;
   ```  
   This allows easy modification later if type needs to change again.

2. **Global Replacement:**  
   Replace all direct usages of `int` for entity identifiers with `EntityId`.  
   Search for:
    - Field declarations (`public int Id;`)
    - Function signatures (`void Spawn(int entityId)`)
    - Network structs (`Packet { int entityId; }`)

3. **Serialization Layer Update:**
    - Update serializers to read/write `ushort` instead of `int`.
    - Adjust network byte order (e.g., `BinaryWriter.Write((ushort)entityId)` and `BinaryReader.ReadUInt16()`).

4. **Validation Logic:**
    - Add assertions:
      ```csharp
      Debug.Assert(entityId <= ushort.MaxValue);
      ```  
    - During entity creation, check that the entity pool doesn’t exceed the `ushort` limit.

5. **Refactor Hash Structures:**
    - If using hash-based structures (`Dictionary<int, Entity>`), redefine them as `Dictionary<EntityId, Entity>`.

6. **Test Serialization Compatibility:**
    - Verify that network clients using the new type still parse packets correctly.
    - Optionally maintain backward compatibility via versioned packets.

7. **Optional Migration Utility:**
    - If data files or saves used `int`, write a conversion step that safely reads `int` and writes `ushort`.  