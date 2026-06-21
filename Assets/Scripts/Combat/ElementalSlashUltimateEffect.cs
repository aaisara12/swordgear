#nullable enable
using UnityEngine;

[CreateAssetMenu(fileName = "ElementalSlashUlt", menuName = "Game/Ultimate Effect/Elemental Slash")]
public class ElementalSlashUltimateEffect : UltimateEffect
{
    [SerializeField] private GameObject? _controllerPrefab;

    public override void Execute(Transform player)
    {
        if (_controllerPrefab == null)
        {
            Debug.LogError("ElementalSlashUltimateEffect: _controllerPrefab is not assigned.");
            return;
        }

        GameObject obj = Object.Instantiate(_controllerPrefab, player.position, Quaternion.identity);
        ElementalSlashUltimateController? controller = obj.GetComponent<ElementalSlashUltimateController>();

        if (controller == null)
        {
            Debug.LogError("ElementalSlashUltimateEffect: prefab is missing ElementalSlashUltimateController.");
            Object.Destroy(obj);
            return;
        }

        controller.Begin(player);
    }
}
