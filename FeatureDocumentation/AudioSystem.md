# Audio System

## Purpose
Centralizes audio playback behind a simple API, decoupling game logic from Unity's `AudioSource` management.

---

## Key Scripts

| Script | Path |
|---|---|
| `AudioSystem` | `Assets/Scripts/AudioSystem.cs` |
| `AudioLibrary` | `Assets/Scripts/AudioLibrary.cs` |

---

## AudioLibrary

`AudioLibrary` is a ScriptableObject (or MonoBehaviour) that holds a catalogue of named `AudioClip` references. Systems request clips by name/key rather than holding direct `AudioClip` references, making it easy to swap sounds without touching gameplay code.

---

## AudioSystem

`AudioSystem` is the runtime playback manager. It exposes methods for playing sound effects and music, pooling or managing `AudioSource` components internally.

---

## Usage Pattern

Reference `AudioSystem` (typically a singleton or injected dependency) and call play methods with a clip identifier:

```csharp
AudioSystem.Instance.PlaySFX("enemy_death");
AudioSystem.Instance.PlayMusic("combat_theme");
```

Clip identifiers are resolved through `AudioLibrary`.

---

## Notes

- Keep audio triggering in gameplay scripts minimal — prefer raising events that an audio listener reacts to, so audio logic doesn't bleed into combat/movement code.
- For UI sounds, prefer connecting `AudioSystem` calls to Unity UI events in the Inspector rather than in code.
