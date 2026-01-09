using UnityEngine;

namespace Shop
{
    public abstract class ItemShopViewModel : MonoBehaviour
    {
        public abstract void Initialize(ItemShopModel model);
    }
}