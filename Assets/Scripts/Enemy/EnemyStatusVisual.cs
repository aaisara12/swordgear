#nullable enable

using UnityEngine;

/// <summary>
/// Per-enemy status tint so burn / chill / static read differently on the enemy (the flyweight effect
/// singletons can't hold per-enemy state). Multiplies the sprite colour — burn = orange, chill = steady
/// cold blue, static = flickering yellow — so ice vs lightning are finally distinguishable at a glance.
///
/// Colour is a separate channel from the hit-flash / charge-telegraph (which swap the *material*), so the
/// two don't fight: while the enemy flashes white the tint is briefly hidden, then reappears.
/// </summary>
public class EnemyStatusVisual : MonoBehaviour
{
    private static readonly Color BurnTint = new(1f, 0.55f, 0.25f, 1f);
    private static readonly Color ChillTint = new(0.5f, 0.75f, 1f, 1f);
    private static readonly Color StaticTint = new(1f, 0.95f, 0.35f, 1f);
    private static readonly Color BuffettedTint = new(0.56f, 0.93f, 0.56f, 1f);
    private const float StaticFlickerHz = 12f;

    private SpriteRenderer[]? _sprites;
    private Color[]? _baseColors;
    private bool _burn;
    private bool _chill;
    private bool _static;
    private bool _buffetted;

    /// <summary>Gets (or lazily adds) the status-visual component on an enemy.</summary>
    public static EnemyStatusVisual For(EnemyController enemy)
    {
        EnemyStatusVisual? v = enemy.GetComponent<EnemyStatusVisual>();
        if (v == null)
        {
            v = enemy.gameObject.AddComponent<EnemyStatusVisual>();
        }

        return v;
    }

    private void Awake()
    {
        _sprites = GetComponentsInChildren<SpriteRenderer>(true);
        _baseColors = new Color[_sprites.Length];
        for (int i = 0; i < _sprites.Length; i++)
        {
            _baseColors[i] = _sprites[i] != null ? _sprites[i].color : Color.white;
        }
    }

    public void SetBurn(bool on) => _burn = on;
    public void SetChill(bool on) => _chill = on;
    public void SetStatic(bool on) => _static = on;
    public void SetBuffetted(bool on) => _buffetted = on;

    private void LateUpdate()
    {
        if (_sprites == null || _baseColors == null)
        {
            return;
        }

        // Priority when several stack (rare): burn > static > chill > buffetted. White == no tint (base colour).
        Color tint = Color.white;
        if (_burn)
        {
            tint = BurnTint;
        }
        else if (_static)
        {
            tint = Mathf.Repeat(Time.time * StaticFlickerHz, 1f) < 0.5f ? StaticTint : Color.white;
        }
        else if (_chill)
        {
            tint = ChillTint;
        }
        else if (_buffetted)
        {
            tint = BuffettedTint;
        }

        for (int i = 0; i < _sprites.Length; i++)
        {
            if (_sprites[i] != null)
            {
                _sprites[i].color = _baseColors[i] * tint;
            }
        }
    }
}
