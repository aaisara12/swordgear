#nullable enable annotations

using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum UpgradeType
{
    // Every upgrade should be named in the format <element-name>_<upgrade-name>
    // NOTE: serialized by integer index in store-item assets — APPEND new values, never insert.
    Nonelemental_DemoUpgrade,
    Ice_EmpowerMelee,
    Ice_RangedChill,
    Fire_ChargeMelee,
    Fire_RangedBurn,
    Lightning_DashStrike,
    Lightning_ApplyStatic,
    Wind_Windstorm,
    Wind_RendingGale,
    Lightning_Thunderstep,        // dash while sword is out -> blink to the sword, cleave + pick it up
    Nonelemental_Attunement,      // same-element attacks deal 0 damage (both player and enemy side)
    Light_LearnTune,              // repeatable: each copy teaches the harp one random tune it doesn't know (HarpRepertoire)
}

/// <summary>
/// One element's weapon behaviour. Every element — melee, ranged or summoner — is driven by the same
/// input shape: <b>tap, hold, release</b>. That's why there is no melee/ranged split; the difference
/// between elements is what those inputs <em>do</em>, not how they're delivered.
/// <para>
/// Only <see cref="OnTap"/> is required — every element must do something when tapped. Everything else
/// has a no-op default so a weapon implements only the hooks it actually uses; see the note on each.
/// </para>
/// </summary>
public interface IElementalWeapon
{
    // ---- Required ----

    /// <summary>Tap. Returns the cooldown in seconds before the next attack is allowed.</summary>
    public float OnTap(Transform player, HashSet<UpgradeType> upgrades);

    // ---- Optional: default no-ops, override only if the element uses them ----

    /// <summary>
    /// Hold-to-charge. Called repeatedly while charging, and once with <paramref name="cancel"/> true when
    /// the charge is aborted. Elements without a charge attack (Physical, Wind) leave this alone.
    /// </summary>
    public void OnCharge(Transform player, HashSet<UpgradeType> upgrades, bool cancel = false) { }

    /// <summary>
    /// A melee hitbox spawned by this weapon connected with an enemy — apply damage and any on-hit effect.
    /// <para>
    /// ⚠️ Defaults to doing nothing, which means <b>no damage</b>. Any weapon that spawns a melee hitbox
    /// must override this. Elements that never spawn one (Earth's turret, Light's harp) correctly leave it.
    /// </para>
    /// </summary>
    public void OnMeleeHit(Transform player, EnemyController enemy, HashSet<UpgradeType> upgrades) { }

    /// <summary>
    /// How far this element's aim snaps onto a nearby enemy.
    /// </summary>
    /// <remarks>
    /// Reach is a property of the weapon, not of the pointer drawing it: a turret should lock onto
    /// something across the arena where a sword has no business reaching past its own swing. Defaults to
    /// the shared radius, so a weapon only overrides this when its range genuinely differs.
    /// </remarks>
    public float AutoAimRadius => ActiveEnemyRegistry.AutoTargetRadius;

    /// <summary>
    /// Short label for what the weapon is doing right now, or empty when there is nothing to say.
    /// </summary>
    /// <remarks>
    /// For multi-stage charges whose stages look identical without it. Earth's charge builds a ballista
    /// before it starts charging a shot, and releasing during the build fires nothing — invisible, and
    /// therefore unfair, unless the player is told which stage they're in.
    /// </remarks>
    public string ChargePhaseLabel => string.Empty;

    /// <summary>Called when this element's imbue starts / ends. Override to reset cross-swing state.</summary>
    public void OnBuffStart(Transform player, SwordProjectile sword, HashSet<UpgradeType> upgrades) { }

    /// <inheritdoc cref="OnBuffStart"/>
    public void OnBuffEnd(Transform player, SwordProjectile sword, HashSet<UpgradeType> upgrades) { }

    /// <summary>Triggered when the player catches the thrown sword. Override for a bespoke cleave.</summary>
    public void Cleave(Transform player, HashSet<UpgradeType> upgrades) { }

    /// <summary>
    /// Per-element dash override. Called (via ElementManager) when the player dashes with the sword out.
    /// Return true if this element handled the dash with a custom behaviour (e.g. Lightning's blink-to-sword),
    /// false to fall through to the normal directional dash. Only the ACTIVE element's weapon is consulted,
    /// so an override is automatically gated to that imbue. Default: no override.
    /// </summary>
    public bool TryOverrideDash(PlayerController player, HashSet<UpgradeType> upgrades) => false;

    // ---- Ranged: dormant while the sword throw is the ultimate, deliberately kept ----

    public void OnRangedFlight(Transform player, SwordProjectile sword, HashSet<UpgradeType> upgrades) { }
    public void OnRangedHit(Transform player, SwordProjectile sword, Transform hitSource, EnemyController enemy, HashSet<UpgradeType> upgrades) { }
}

[System.Serializable]
class ElementWeaponPair
{
    public Element element;
    public MonoBehaviour weapon; // Must implement IElementalWeapon
}

public class ElementManager : InitializeableGameComponent
{
    [SerializeField] List<ElementWeaponPair> elementalWeapons;

    Dictionary<Element, IElementalWeapon> weapons = new Dictionary<Element, IElementalWeapon>();

    private IElementalWeapon activeWeapon = null;
    private HashSet<UpgradeType> currentUpgrades = new HashSet<UpgradeType>();
    
    private IReadOnlyPlayerBlob _playerBlob;

    public static ElementManager Instance = null;

    public Element ActiveElement { get; private set; } = Element.Physical;

    /// <summary>
    /// Fired when the player switches active element (e.g. bouncing between embues).
    /// </summary>
    public static event Action<Element>? OnActiveElementChanged;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(this);
            return;
        }
        Instance = this;

        // Build dictionary from serialized list
        weapons.Clear();
        foreach (var pair in elementalWeapons)
        {
            if (pair.weapon is IElementalWeapon weaponImpl)
                weapons[pair.element] = weaponImpl;
            else
                Debug.LogWarning($"Weapon on {pair.element} does not implement IElementalWeapon.");
        }
    }

    private void OnDestroy()
    {
        if (_playerBlob != null)
        {
            _playerBlob.InventoryItems.DictionaryChanged -= HandlePlayerInventoryChanged;
        }
    }

    // ----------- Public API -----------

    public void SetActiveElement(Element element)
    {
        if (weapons.TryGetValue(element, out var found))
        {
            if (activeWeapon != null)
            {
                OnCharge(GameManager.Instance.player.transform, cancel: true);
                OnBuffEnd(GameManager.Instance.player.transform, SwordProjectile.Instance);
            }

            activeWeapon = found;
            ActiveElement = element;
            OnBuffStart(GameManager.Instance.player.transform, SwordProjectile.Instance);
            OnActiveElementChanged?.Invoke(element);
        }
        else
            Debug.LogError($"No weapon registered for element {element}");
    }

    /// <summary>
    /// Returns charge display data when the active weapon supports charging and is currently charging.
    /// </summary>
    public bool TryGetMeleeChargeDisplayState(out MeleeChargeDisplayState state)
    {
        state = default;

        if (activeWeapon is not IMeleeChargeProvider chargeProvider)
        {
            return false;
        }

        PlayerController? player = ResolvePlayer();
        if (player == null)
        {
            return false;
        }

        if (!chargeProvider.CanShowChargeIndicator(currentUpgrades, player))
        {
            return false;
        }

        if (!chargeProvider.IsCharging)
        {
            return false;
        }

        state = new MeleeChargeDisplayState(
            chargeProvider.ChargeProgress,
            ActiveElement,
            chargeProvider.IsMaxCharge);
        return true;
    }

    /// <summary>
    /// True while the active weapon's charge is rooting the player. Polled by PlayerController every
    /// frame — see <see cref="IAimLockProvider"/> for why this is pulled rather than pushed.
    /// </summary>
    public bool IsAimLocked => activeWeapon is IAimLockProvider aimLock && aimLock.IsAimLocked;

    /// <summary>
    /// True when the active weapon has no tap attack and wants the charge to begin on the press itself,
    /// rather than waiting out the tap/hold split. See <see cref="IAimLockProvider.ChargesOnPress"/>.
    /// </summary>
    public bool ChargesOnPress => activeWeapon is IAimLockProvider aimLock && aimLock.ChargesOnPress;

    /// <summary>
    /// The active element's auto-aim reach, falling back to the shared radius before any element is
    /// active. See <see cref="IElementalWeapon.AutoAimRadius"/>.
    /// </summary>
    public float AutoAimRadius => activeWeapon?.AutoAimRadius ?? ActiveEnemyRegistry.AutoTargetRadius;

    /// <summary>
    /// What the active weapon is doing right now, or empty when there is nothing to show.
    /// See <see cref="IElementalWeapon.ChargePhaseLabel"/>.
    /// </summary>
    public string ChargePhaseLabel => activeWeapon?.ChargePhaseLabel ?? string.Empty;

    private static PlayerController? ResolvePlayer()
    {
        if (GameManager.Instance == null || GameManager.Instance.player == null)
        {
            return null;
        }

        return GameManager.Instance.player.GetComponent<PlayerController>();
    }

    public void AddUpgrade(UpgradeType upgrade) => currentUpgrades.Add(upgrade);
    public void RemoveUpgrade(UpgradeType upgrade) => currentUpgrades.Remove(upgrade);
    public bool HasUpgrade(UpgradeType upgrade) => currentUpgrades.Contains(upgrade);
    public void ClearUpgrades() => currentUpgrades.Clear();


    // ----------- IElementalWeapon Forwards -----------

    public float OnTap(Transform player)
    {
        if (activeWeapon == null) return 0f;
        float baseCooldown = activeWeapon.OnTap(player, currentUpgrades);
        return MeleeAugmentUtility.ScaleCooldown(baseCooldown);
    }

    public void OnCharge(Transform player, bool cancel = false)
    {
        if (activeWeapon == null) return;
        activeWeapon.OnCharge(player, currentUpgrades, cancel);
    }

    public void OnMeleeHit(Transform player, EnemyController enemy)
    {
        if (activeWeapon == null) return;
        activeWeapon.OnMeleeHit(player, enemy, currentUpgrades);
    }

    public void OnBuffStart(Transform player, SwordProjectile sword)
    {
        if (activeWeapon == null) return;
        activeWeapon.OnBuffStart(player, sword, currentUpgrades);
    }

    public void OnBuffEnd(Transform player, SwordProjectile sword)
    {
        if (activeWeapon == null) return;
        activeWeapon.OnBuffEnd(player, sword, currentUpgrades);
    }

    /// <summary> Triggered when the player catches the thrown sword; each element performs its own cleave attack. </summary>
    public void Cleave(Transform player)
    {
        if (activeWeapon == null) return;
        activeWeapon.Cleave(player, currentUpgrades);
    }

    /// <summary>Asks the ACTIVE element's weapon to override the dash; returns true if it did.</summary>
    public bool TryOverrideDash(PlayerController player)
    {
        if (activeWeapon == null) return false;
        return activeWeapon.TryOverrideDash(player, currentUpgrades);
    }

    public void OnRangedFlight(Transform player, SwordProjectile sword)
    {
        if (activeWeapon == null) return;
        activeWeapon.OnRangedFlight(player, sword, currentUpgrades);
    }

    public void OnRangedHit(Transform player, SwordProjectile sword, Transform hitSource, EnemyController enemy)
    {
        if (activeWeapon == null) return;
        activeWeapon.OnRangedHit(player, sword, hitSource, enemy, currentUpgrades);
    }

    // ----------- Entry points called by hitboxes / collisions -----------

    public void OnEnemyMeleeHit(EnemyController enemy)
    {
        if (activeWeapon == null) return;

        // Normally your melee hitbox script will pass `player`, so fetch it from somewhere:
        Transform player = transform; // Replace with your actual player reference
        activeWeapon.OnMeleeHit(player, enemy, currentUpgrades);
    }

    public void OnEnemyRangedHit(EnemyController enemy, Transform hitSource)
    {
        if (activeWeapon == null) return;

        Transform player = transform; // Or a PlayerManager.PlayerTransform reference

        activeWeapon.OnRangedHit(player, SwordProjectile.Instance, hitSource, enemy, currentUpgrades);
    }

    public override void InitializeOnGameStart(IReadOnlyPlayerBlob playerBlob)
    {
        _playerBlob = playerBlob;
        
        GameUtility.LoadElementUpgradesFromPlayerBlob(_playerBlob, this);
        
        _playerBlob.InventoryItems.DictionaryChanged += HandlePlayerInventoryChanged;
    }

    private void HandlePlayerInventoryChanged(ObservableDictionaryChangedEventArgs<string, int> obj)
    {
        switch (obj.Action)
        {
            case ObservableDictionaryChangedEventArgs<string, int>.ChangeType.Add:
            {
                string? itemId = obj.Key;
                itemId.ThrowIfNull(nameof(itemId));
                
                HandleInventoryCountChanged(itemId, obj.NewValue);

                break;
            }
            case ObservableDictionaryChangedEventArgs<string, int>.ChangeType.Remove:
            {
                string? itemId = obj.Key;
                itemId.ThrowIfNull(nameof(itemId));
                
                HandleInventoryCountChanged(itemId, 0);

                break;
            }
            case ObservableDictionaryChangedEventArgs<string, int>.ChangeType.Replace:
            {
                string? itemId = obj.Key;
                itemId.ThrowIfNull(nameof(itemId));
                
                HandleInventoryCountChanged(itemId, obj.NewValue);

                break;
            }
            case ObservableDictionaryChangedEventArgs<string, int>.ChangeType.Clear:
            {
                HandleInventoryCleared();
                break;
            }
        }
    }

    private void HandleInventoryCountChanged(string itemId, int newCount)
    {
        if (UpgradeTypeSerializer.TryDeserialize(itemId, out UpgradeType upgrade))
        {
            if (newCount == 0)
            {
                RemoveUpgrade(upgrade);
            }
            else if (newCount >= 1)
            {
                if (HasUpgrade(upgrade))
                {
                    return;
                }
                
                AddUpgrade(upgrade);
            }
        }
    }
    
    private void HandleInventoryCleared()
    {
        ClearUpgrades();
    }
}
