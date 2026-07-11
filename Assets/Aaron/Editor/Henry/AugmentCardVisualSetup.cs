#if UNITY_EDITOR
#nullable enable

using Shop;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Creates augment tier card materials, inner flare layer, sword watermark, and wires them on the Augment Card prefab.
/// Removes the outer TierAura layer so card size stays unchanged.
/// Menu: Henry/Setup Augment Card Visuals
/// </summary>
public static class AugmentCardVisualSetup
{
    private const string CardShaderName = "Swordgear/UI/AugmentTierCard";
    private const string FlareShaderName = "Swordgear/UI/AugmentTierCardFlare";
    private const string CardMaterialPath = "Assets/Visuals/Materials/AugmentTierCard.mat";
    private const string FlareMaterialPath = "Assets/Visuals/Materials/AugmentTierCardFlare.mat";
    private const string PrefabPath = "Assets/Aaron/Prefabs/Shop/Augment Card.prefab";
    private const string SwordSpritePath = "Assets/Visuals/swordgear sword.png";
    private const string SwordSpriteName = "swordgear sword_0";
    private const string AuraChildName = "TierAura";
    private const string FlareChildName = "TierInnerFlare";
    private const string SwordWatermarkChildName = "SwordWatermark";
    private const int UiLayer = 5;

    [MenuItem("Henry/Setup Augment Card Visuals")]
    public static void SetupFromMenu()
    {
        Shader? cardShader = Shader.Find(CardShaderName);
        Shader? flareShader = Shader.Find(FlareShaderName);
        if (cardShader == null || flareShader == null)
        {
            Debug.LogError("AugmentCardVisualSetup: shaders not found. Wait for import/compile, then retry.");
            return;
        }

        Material cardMaterial = EnsureMaterial(CardMaterialPath, cardShader, "AugmentTierCard");
        Material flareMaterial = EnsureMaterial(FlareMaterialPath, flareShader, "AugmentTierCardFlare");
        Sprite? swordSprite = LoadSwordSprite();
        if (swordSprite == null)
        {
            Debug.LogError($"AugmentCardVisualSetup: could not load sword sprite '{SwordSpriteName}' from {SwordSpritePath}");
            return;
        }

        GameObject? prefabRoot = PrefabUtility.LoadPrefabContents(PrefabPath);
        if (prefabRoot == null)
        {
            Debug.LogError($"AugmentCardVisualSetup: missing prefab at {PrefabPath}");
            return;
        }

        try
        {
            var viewModel = prefabRoot.GetComponent<AugmentShopElementViewModel>();
            if (viewModel == null)
            {
                Debug.LogError("AugmentCardVisualSetup: AugmentShopElementViewModel missing on prefab root.");
                return;
            }

            Transform border = prefabRoot.transform.Find("Border")!;
            Transform main = prefabRoot.transform.Find("Main")!;
            Image mainImage = main.GetComponent<Image>()!;
            Image borderImage = border.GetComponent<Image>()!;

            RemoveAuraLayer(prefabRoot.transform);
            EnsureMainMask(main.gameObject, mainImage);
            ConfigureSwordWatermark(main, swordSprite);
            Image flareImage = ConfigureInnerFlareLayer(prefabRoot.transform, mainImage);

            int mainIndex = main.GetSiblingIndex();
            flareImage.transform.SetSiblingIndex(mainIndex + 1);

            var serialized = new SerializedObject(viewModel);
            serialized.FindProperty("tierCardMaterialTemplate").objectReferenceValue = cardMaterial;
            serialized.FindProperty("tierFlareMaterialTemplate").objectReferenceValue = flareMaterial;
            serialized.FindProperty("cardBackground").objectReferenceValue = mainImage;
            serialized.FindProperty("cardBorder").objectReferenceValue = borderImage;
            serialized.FindProperty("cardInnerFlare").objectReferenceValue = flareImage;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            mainImage.color = Color.white;
            flareImage.color = Color.white;
            flareImage.raycastTarget = false;

            PrefabUtility.SaveAsPrefabAsset(prefabRoot, PrefabPath);
            Debug.Log("AugmentCardVisualSetup: card + inner flare + sword watermark wiring complete.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static Sprite? LoadSwordSprite()
    {
        UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(SwordSpritePath);
        for (int i = 0; i < assets.Length; i++)
        {
            if (assets[i] is Sprite sprite && sprite.name == SwordSpriteName)
            {
                return sprite;
            }
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(SwordSpritePath);
    }

    private static Material EnsureMaterial(string path, Shader shader, string materialName)
    {
        Material? material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(shader) { name = materialName };
            AssetDatabase.CreateAsset(material, path);
        }
        else if (material.shader != shader)
        {
            material.shader = shader;
            EditorUtility.SetDirty(material);
        }

        return material;
    }

    private static void RemoveAuraLayer(Transform prefabRoot)
    {
        Transform? aura = prefabRoot.Find(AuraChildName);
        if (aura != null)
        {
            Object.DestroyImmediate(aura.gameObject);
        }
    }

    private static void EnsureMainMask(GameObject mainGo, Image mainImage)
    {
        Mask? mask = mainGo.GetComponent<Mask>();
        if (mask == null)
        {
            mask = mainGo.AddComponent<Mask>();
        }

        // Keep the tier card graphic visible; only children are clipped to Main's shape.
        mask.showMaskGraphic = true;
        mainImage.maskable = true;
    }

    private static void ConfigureSwordWatermark(Transform main, Sprite swordSprite)
    {
        Transform watermarkTransform = EnsureChildLayer(main, SwordWatermarkChildName);
        watermarkTransform.gameObject.layer = UiLayer;

        var watermarkRect = (RectTransform)watermarkTransform;
        watermarkRect.anchorMin = new Vector2(0.5f, 0.5f);
        watermarkRect.anchorMax = new Vector2(0.5f, 0.5f);
        watermarkRect.pivot = new Vector2(0.5f, 0.5f);
        watermarkRect.anchoredPosition = new Vector2(18f, -28f);
        watermarkRect.sizeDelta = new Vector2(520f, 286f);
        watermarkRect.localEulerAngles = new Vector3(0f, 0f, -38f);
        watermarkRect.localScale = Vector3.one;

        Image watermarkImage = watermarkTransform.GetComponent<Image>()!;
        watermarkImage.sprite = swordSprite;
        watermarkImage.type = Image.Type.Simple;
        watermarkImage.preserveAspect = true;
        watermarkImage.raycastTarget = false;
        watermarkImage.maskable = true;
        // Soft grey silhouette so it reads as part of the card background.
        watermarkImage.color = new Color(0.72f, 0.74f, 0.78f, 0.16f);
        watermarkTransform.SetAsFirstSibling();
    }

    private static Image ConfigureInnerFlareLayer(Transform prefabRoot, Image mainImage)
    {
        Transform flareTransform = EnsureChildLayer(prefabRoot, FlareChildName);
        flareTransform.gameObject.layer = UiLayer;

        var flareRect = (RectTransform)flareTransform;
        var mainRect = (RectTransform)mainImage.transform;
        flareRect.anchorMin = mainRect.anchorMin;
        flareRect.anchorMax = mainRect.anchorMax;
        flareRect.pivot = mainRect.pivot;
        flareRect.anchoredPosition = mainRect.anchoredPosition;
        flareRect.sizeDelta = mainRect.sizeDelta;

        var flareImage = flareTransform.GetComponent<Image>()!;
        CopyImageSprite(mainImage, flareImage);
        return flareImage;
    }

    private static Transform EnsureChildLayer(Transform parent, string childName)
    {
        Transform? existing = parent.Find(childName);
        if (existing != null)
        {
            return existing;
        }

        var layerGo = new GameObject(childName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        layerGo.transform.SetParent(parent, false);
        layerGo.layer = UiLayer;
        return layerGo.transform;
    }

    private static void CopyImageSprite(Image source, Image target)
    {
        target.sprite = source.sprite;
        target.type = source.type;
        target.pixelsPerUnitMultiplier = source.pixelsPerUnitMultiplier;
        target.raycastTarget = false;
    }
}
#endif
