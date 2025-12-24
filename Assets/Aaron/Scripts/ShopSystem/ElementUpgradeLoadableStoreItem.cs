using UnityEngine;

namespace Shop
{
    [CreateAssetMenu(fileName = "ElementUpgradeLoadableStoreItem", menuName = "Scriptable Objects/Element Upgrade Loadable Store Item")]
    public class ElementUpgradeLoadableStoreItem : LoadableStoreItem
    {
        [SerializeField] private UpgradeType elementUpgrade;
    }
}