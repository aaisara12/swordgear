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
public enum Element { Physical, Fire, Ice, Lightning }
```

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

---

## Embue System

Embue scripts (attached to in-world pickup objects) call `GameManager.Instance.ApplyEmpowerment(element, multiplier, duration)`, which runs a coroutine that:
1. Sets the active element and damage multiplier for the duration.
2. Resets back to `Element.Physical` when the timer expires.

Only one empowerment can be active at a time — starting a new one cancels the previous coroutine.

---

## Weapon Implementations

Located in `Assets/Scripts/Weapon Implementations/`. Each weapon (`FireWeapon`, `IceWeapon`, `LightningWeapon`, `PhysicalWeapon`) applies element-specific effects on hit (e.g., `GameManager.AddEffect` for status effects).
