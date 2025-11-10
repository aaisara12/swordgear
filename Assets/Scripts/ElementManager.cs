using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum UpgradeType
{
    // Every upgrade should be named in the format <element-name>_<upgrade-name>
    Nonelemental_DemoUpgrade,
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

public class ElementManager : MonoBehaviour
{
    [SerializeField] List<ElementWeaponPair> elementalWeapons;

    Dictionary<Element, IElementalWeapon> weapons = new Dictionary<Element, IElementalWeapon>();

    private IElementalWeapon activeWeapon = null;
    private HashSet<UpgradeType> currentUpgrades = new HashSet<UpgradeType>();

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

    // ----------- Public API -----------

    public void SetActiveElement(Element element)
    {
        if (weapons.TryGetValue(element, out var found))
            activeWeapon = found;
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

        // hitSource should be the projectile transform.
        SwordProjectile proj = hitSource.GetComponent<SwordProjectile>();
        Transform player = transform; // Or a PlayerManager.PlayerTransform reference

        if (proj != null)
            activeWeapon.OnRangedHit(player, proj, hitSource, enemy, currentUpgrades);
    }
}
