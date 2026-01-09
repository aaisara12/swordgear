using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum UpgradeType
{
    // Every upgrade should be named in the format <element-name>_<upgrade-name>
    Nonelemental_DemoUpgrade,
    Ice_EmpowerMelee,
    Ice_RangedChill,
    Fire_ChargeMelee,
    Fire_RangedBurn,
    Lightning_DashStrike,
    Lightning_ApplyStatic,
}

public interface IElementalWeapon
{
    // Melee
    public void MeleeStrike(Transform player, HashSet<UpgradeType> upgrades);
    public void MeleeCharge(Transform player, HashSet<UpgradeType> upgrades, bool cancel = false);
    public void OnMeleeHit(Transform player, EnemyController enemy, HashSet<UpgradeType> upgrades);

    // Universal
    public void OnBuffStart(Transform player, SwordProjectile sword, HashSet<UpgradeType> upgrades);
    public void OnBuffEnd(Transform player, SwordProjectile sword, HashSet<UpgradeType> upgrades);

    // Ranged
    public void OnRangedFlight(Transform player, SwordProjectile sword, HashSet<UpgradeType> upgrades);
    public void OnRangedHit(Transform player, SwordProjectile sword, Transform hitSource, EnemyController enemy, HashSet<UpgradeType> upgrades);
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
            OnBuffEnd(GameManager.Instance.player.transform, SwordProjectile.Instance);
            activeWeapon = found;
            OnBuffStart(GameManager.Instance.player.transform, SwordProjectile.Instance);
        }
        else
            Debug.LogError($"No weapon registered for element {element}");
    }

    public void AddUpgrade(UpgradeType upgrade) => currentUpgrades.Add(upgrade);
    public void RemoveUpgrade(UpgradeType upgrade) => currentUpgrades.Remove(upgrade);
    public bool HasUpgrade(UpgradeType upgrade) => currentUpgrades.Contains(upgrade);
    public void ClearUpgrades() => currentUpgrades.Clear();


    // ----------- IElementalWeapon Forwards -----------

    public void MeleeStrike(Transform player)
    {
        if (activeWeapon == null) return;
        Debug.Log(activeWeapon);
        activeWeapon.MeleeStrike(player, currentUpgrades);
    }

    public void MeleeCharge(Transform player, bool cancel = false)
    {
        if (activeWeapon == null) return;
        activeWeapon.MeleeCharge(player, currentUpgrades, cancel);
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
