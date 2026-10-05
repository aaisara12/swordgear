# Element System

## Purpose
Tracks the player's active damage element and computes elemental interaction multipliers between attacker and defender elements.

---

## Key Scripts

| Script | Path |
|---|---|
| `ElementManager` | `Assets/Scripts/ElementManager.cs` |
| `ElementalInteractions` | `Assets/ElementalInteractions.cs` |
| `Embue` | `Assets/Embue.cs` |
| `FireEmbue` | `Assets/FireEmbue.cs` |
| `IceEmbue` | `Assets/IceEmbue.cs` |
| `LightningEmbue` | `Assets/LightningEmbue.cs` |
| `FireMelee` | `Assets/Scripts/FireMelee.cs` |
| `IceChillField` | `Assets/Scripts/IceChillField.cs` |
| `LightningMelee` | `Assets/Scripts/LightningMelee.cs` |

---

## Element Enum

```csharp
public enum Element { Physical, Fire, Ice, Lightning, Wind, Earth, Dark, Light }
```

Values are serialized as ints into prefabs and `.asset` files — **only ever append**. Inserting a value
silently remaps every existing reference to the elements after it.

Earth, Dark and Light are **player-only** (no enemy variants) and currently neutral in the damage matrix.
See [ElementExpansionPlan.md](ElementExpansionPlan.md).

---

## ElementManager

A singleton (`ElementManager.Instance`) that stores the current active element and fires a static event when it changes:

```csharp
public static event Action<Element>? OnActiveElementChanged;
public void SetActiveElement(Element element);
```

`GameManager.currentElement` is the primary setter — it delegates to `ElementManager.SetActiveElement`.

---

## Elemental Interactions

`ElementalInteractions.interactionMatrix` is a `Dictionary<Element, Dictionary<Element, float>>` where `[attacker][defender]` returns a damage multiplier. This is used in `GameManager.CalculateDamage`.

Read it through `ElementalInteractions.GetMultiplier(attacker, defender)`, never by indexing the matrix
directly — the table is sparse, and a missing pair resolves to `NeutralMultiplier` (1×) instead of
throwing `KeyNotFoundException` mid-combat. Adding a row is how an element opts into the matrix.

---

## Embue System

The gear flick (`GearManager.TryGrantElementFromDirection`) and the legacy Embue pickups call `GameManager.Instance.ApplyEmpowerment(element, multiplier)`, which sets the active element. An imbue doesn't time out: it lasts until the next one replaces it, or until the player enters a new node — `PlayerGameplayManager.SpawnPawnAtLocation` (each arena, the tutorial) calls `GameManager.ClearEmpowerment()`, which puts the player back on `Element.Physical`.

---

## Weapon Implementations

Located in `Assets/Scripts/Weapon Implementations/`. Each weapon (`FireWeapon`, `IceWeapon`, `LightningWeapon`, `PhysicalWeapon`) applies element-specific effects on hit (e.g., `GameManager.AddEffect` for status effects).
