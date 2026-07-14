#nullable enable

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Mobile schematic minimap: runtime-generated map, player/enemy blips, and an exit-portal marker.
/// UI references must be assigned in the CombatHUD scene.
/// </summary>
public class MinimapController : MonoBehaviour
{
    public static MinimapController? Instance { get; private set; }

    private const int MaxEnemyBlips = 16;

    private static readonly Color PhysicalColor = new(0.816f, 0.816f, 0.816f, 1f);
    private static readonly Color FireColor = new(1f, 0.333f, 0.2f, 1f);
    private static readonly Color IceColor = new(0.267f, 0.8f, 1f, 1f);
    private static readonly Color LightningColor = new(1f, 0.839f, 0.2f, 1f);
    private static readonly Color PortalColor = new(0.2f, 0.75f, 1f, 1f); // matches the portal's halo ring

    [Header("UI")]
    [SerializeField] private RectTransform? minimapPanel;
    [SerializeField] private CanvasGroup? panelCanvasGroup;
    [SerializeField] private RawImage? mapImage;
    [SerializeField] private RectTransform? blipContainer;
    [SerializeField] private Image? playerBlip;
    [SerializeField] private Image? enemyBlipPrefab;
    [SerializeField] private Image? portalBlip;

    [Header("UI Size")]
    [SerializeField]
    [Range(140f, 320f)]
    [Tooltip("Diameter of the minimap on screen in canvas pixels.")]
    private float minimapPanelSize = 220f;

    [Header("Zoom")]
    [SerializeField]
    [Range(16f, 96f)]
    [Tooltip("World units shown in the minimap viewport. Lower = more zoomed in. The view follows the player.")]
    private float visibleWorldSize = 40f;

    private readonly List<Image> enemyBlips = new(MaxEnemyBlips);
    private Texture2D? mapTexture;
    private Bounds roomBounds;
    private bool hasActiveRoom;
    private float mapDisplaySize;
    private Rect currentViewportUv = new Rect(0f, 0f, 1f, 1f);
    private Vector2 lastPlayerMoveDirection = Vector2.up;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (!ValidateReferences())
        {
            enabled = false;
            return;
        }

        ApplyPanelSize();
        EnsureEnemyBlips();
        EnsurePortalBlip();
        ApplyPlayerBlipSprite();
        CacheMapDisplaySize();
        EnsurePanelCanvasGroup();
        SetPanelVisible(false);
    }

    private void OnEnable()
    {
        LevelLoader.Instance?.RefreshMinimapIfLoaded();
    }

    private void EnsurePanelCanvasGroup()
    {
        if (panelCanvasGroup == null && minimapPanel != null)
        {
            panelCanvasGroup = minimapPanel.GetComponent<CanvasGroup>();
            if (panelCanvasGroup == null)
            {
                panelCanvasGroup = minimapPanel.gameObject.AddComponent<CanvasGroup>();
            }
        }
    }

    private bool ValidateReferences()
    {
        if (minimapPanel == null)
        {
            Debug.LogError("MinimapController: minimapPanel is not assigned.");
            return false;
        }

        if (mapImage == null)
        {
            Debug.LogError("MinimapController: mapImage is not assigned.");
            return false;
        }

        if (blipContainer == null)
        {
            Debug.LogError("MinimapController: blipContainer is not assigned.");
            return false;
        }

        if (playerBlip == null)
        {
            Debug.LogError("MinimapController: playerBlip is not assigned.");
            return false;
        }

        if (enemyBlipPrefab == null)
        {
            Debug.LogError("MinimapController: enemyBlipPrefab is not assigned.");
            return false;
        }

        return true;
    }

    private void ApplyPanelSize()
    {
        if (minimapPanel == null)
        {
            return;
        }

        minimapPanel.sizeDelta = new Vector2(minimapPanelSize, minimapPanelSize);
        CacheMapDisplaySize();
    }

    private void ApplyPlayerBlipSprite()
    {
        if (playerBlip == null)
        {
            return;
        }

        playerBlip.sprite = CreateArrowSprite();
        playerBlip.color = Color.white;
    }

    private static Sprite? arrowSprite;

    private static Sprite CreateArrowSprite()
    {
        if (arrowSprite != null)
        {
            return arrowSprite;
        }

        const int size = 32;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color32[] pixels = new Color32[size * size];
        Vector2 tip = new Vector2(size * 0.5f, size * 0.88f);
        Vector2 left = new Vector2(size * 0.2f, size * 0.2f);
        Vector2 right = new Vector2(size * 0.8f, size * 0.2f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                pixels[y * size + x] = PointInTriangle(new Vector2(x, y), tip, left, right)
                    ? new Color32(255, 255, 255, 255)
                    : new Color32(0, 0, 0, 0);
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply();
        arrowSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        return arrowSprite;
    }

    private static bool PointInTriangle(Vector2 point, Vector2 a, Vector2 b, Vector2 c)
    {
        float denominator = (b.y - c.y) * (a.x - c.x) + (c.x - b.x) * (a.y - c.y);
        if (Mathf.Approximately(denominator, 0f))
        {
            return false;
        }

        float alpha = ((b.y - c.y) * (point.x - c.x) + (c.x - b.x) * (point.y - c.y)) / denominator;
        float beta = ((c.y - a.y) * (point.x - c.x) + (a.x - c.x) * (point.y - c.y)) / denominator;
        float gamma = 1f - alpha - beta;
        return alpha >= 0f && beta >= 0f && gamma >= 0f;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        DestroyTexture(ref mapTexture);
    }

    private void LateUpdate()
    {
        if (!hasActiveRoom || mapImage == null || playerBlip == null || blipContainer == null)
        {
            return;
        }

        GameObject? player = GameManager.Instance?.player;
        if (player == null)
        {
            playerBlip.enabled = false;
            HideEnemyBlips();
            HidePortalBlip();
            return;
        }

        Vector2 playerPos = GetMapPosition(player);
        CacheMapDisplaySize();
        UpdateViewport(playerPos);
        UpdateEnemyBlips();
        UpdatePortalBlip();
        UpdatePlayerBlip(playerPos, GetPlayerMovementDirection(player));
    }

    private const float PortalBlipInset = 11f;

    // Rounded-square panel: clamp to the box (per-axis), not a circle, so a marker off the viewport lands on
    // the correct edge instead of somewhere inside the corners. Inset keeps the whole icon on-panel.
    private Vector2 ClampBlipToEdge(Vector2 blipPos, float inset)
    {
        float half = Mathf.Max(0f, GetViewportRadiusPixels() - inset);
        blipPos.x = Mathf.Clamp(blipPos.x, -half, half);
        blipPos.y = Mathf.Clamp(blipPos.y, -half, half);
        return blipPos;
    }

    public void Refresh(GameObject? roomRoot)
    {
        DestroyTexture(ref mapTexture);
        hasActiveRoom = false;

        if (roomRoot == null || mapImage == null)
        {
            ClearDisplay();
            return;
        }

        RegenerateMap(roomRoot);
    }

    private void RegenerateMap(GameObject roomRoot)
    {
        if (mapImage == null)
        {
            return;
        }

        (Texture2D? map, Bounds bounds, bool hasContent) = MinimapMapGenerator.Generate(roomRoot);
        if (!hasContent || map == null)
        {
            ClearDisplay();
            return;
        }

        mapTexture = map;
        roomBounds = bounds;
        hasActiveRoom = true;
        currentViewportUv = new Rect(0f, 0f, 1f, 1f);
        lastPlayerMoveDirection = Vector2.up;

        mapImage.texture = mapTexture;
        mapImage.enabled = true;
        SetPanelVisible(true);
    }

    private void ClearDisplay()
    {
        if (mapImage != null)
        {
            mapImage.texture = null;
            mapImage.enabled = false;
        }

        HideEnemyBlips();

        if (playerBlip != null)
        {
            playerBlip.enabled = false;
        }

        HidePortalBlip();
        SetPanelVisible(false);
    }

    private void SetPanelVisible(bool visible)
    {
        EnsurePanelCanvasGroup();
        if (panelCanvasGroup != null)
        {
            panelCanvasGroup.alpha = visible ? 1f : 0f;
            panelCanvasGroup.interactable = visible;
            panelCanvasGroup.blocksRaycasts = visible;
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        visibleWorldSize = Mathf.Clamp(visibleWorldSize, 16f, 96f);
        minimapPanelSize = Mathf.Clamp(minimapPanelSize, 140f, 320f);
        ApplyPanelSize();
    }
#endif

    private void EnsurePortalBlip()
    {
        if (portalBlip != null || blipContainer == null || enemyBlipPrefab == null)
        {
            return;
        }

        portalBlip = Instantiate(enemyBlipPrefab, blipContainer);
        portalBlip.gameObject.name = "PortalBlip";
        portalBlip.sprite = CreatePortalBlipSprite();
        portalBlip.color = PortalColor;
        portalBlip.preserveAspect = true;
        portalBlip.rectTransform.sizeDelta = new Vector2(17f, 24f); // taller than wide (vertical oval)
        portalBlip.gameObject.SetActive(false);
    }

    private static Sprite? portalBlipSprite;

    private static Sprite CreatePortalBlipSprite()
    {
        if (portalBlipSprite != null)
        {
            return portalBlipSprite;
        }

        // Vertical oval ring — reads as a portal/gateway (blue via PortalColor), distinct from the enemy dots.
        const int w = 20;
        const int h = 28;
        Texture2D texture = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color32[] pixels = new Color32[w * h];
        float cx = (w - 1) * 0.5f;
        float cy = (h - 1) * 0.5f;
        float outerRx = w * 0.46f;
        float outerRy = h * 0.46f;
        float innerRx = w * 0.30f;
        float innerRy = h * 0.30f;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float nxO = (x - cx) / outerRx;
                float nyO = (y - cy) / outerRy;
                float nxI = (x - cx) / innerRx;
                float nyI = (y - cy) / innerRy;
                bool insideOuter = nxO * nxO + nyO * nyO <= 1f;
                bool outsideInner = nxI * nxI + nyI * nyI >= 1f;
                pixels[y * w + x] = insideOuter && outsideInner
                    ? new Color32(255, 255, 255, 255)
                    : new Color32(0, 0, 0, 0);
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply();
        portalBlipSprite = Sprite.Create(texture, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
        return portalBlipSprite;
    }

    private void UpdatePortalBlip()
    {
        if (portalBlip == null)
        {
            return;
        }

        Transform? portal = LevelLoader.Instance != null ? LevelLoader.Instance.ExitPortalTransform : null;
        if (portal == null)
        {
            HidePortalBlip();
            return;
        }

        // The exit is the objective — keep it on-panel by clamping to the edge when it's off-view so it
        // always points the way out.
        Vector2 blipPos = ClampBlipToEdge(WorldToBlipPosition(portal.position), PortalBlipInset);

        portalBlip.gameObject.SetActive(true);
        portalBlip.enabled = true;
        portalBlip.rectTransform.anchoredPosition = blipPos;
        portalBlip.transform.SetAsLastSibling();
    }

    private void HidePortalBlip()
    {
        if (portalBlip != null)
        {
            portalBlip.gameObject.SetActive(false);
        }
    }

    private void EnsureEnemyBlips()
    {
        if (enemyBlipPrefab == null || blipContainer == null)
        {
            return;
        }

        enemyBlipPrefab.gameObject.SetActive(false);

        while (enemyBlips.Count < MaxEnemyBlips)
        {
            Image blip = Instantiate(enemyBlipPrefab, blipContainer);
            blip.gameObject.SetActive(false);
            enemyBlips.Add(blip);
        }
    }

    private void CacheMapDisplaySize()
    {
        if (mapImage != null)
        {
            mapDisplaySize = mapImage.rectTransform.rect.width;
            if (mapDisplaySize > 1f)
            {
                return;
            }
        }

        if (blipContainer != null)
        {
            mapDisplaySize = blipContainer.rect.width;
            return;
        }

        if (minimapPanel != null)
        {
            mapDisplaySize = minimapPanel.rect.width - 16f;
        }
    }

    private static Vector2 GetMapPosition(GameObject target)
    {
        Rigidbody2D? rb = target.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            return rb.position;
        }

        return target.transform.position;
    }

    private void UpdateViewport(Vector2 playerPos)
    {
        if (mapImage == null)
        {
            return;
        }

        Vector2 playerNormalized = MinimapMapGenerator.WorldToNormalized(playerPos, roomBounds);
        float viewportWidth = Mathf.Min(1f, visibleWorldSize / roomBounds.size.x);
        float viewportHeight = Mathf.Min(1f, visibleWorldSize / roomBounds.size.y);

        float halfWidth = viewportWidth * 0.5f;
        float halfHeight = viewportHeight * 0.5f;

        float uvX = Mathf.Clamp(playerNormalized.x - halfWidth, 0f, 1f - viewportWidth);
        float uvY = Mathf.Clamp(playerNormalized.y - halfHeight, 0f, 1f - viewportHeight);

        currentViewportUv = new Rect(uvX, uvY, viewportWidth, viewportHeight);
        mapImage.uvRect = currentViewportUv;
    }

    private Vector2 WorldToBlipPosition(Vector2 worldPos)
    {
        Vector2 normalized = MinimapMapGenerator.WorldToNormalized(worldPos, roomBounds);
        float size = mapDisplaySize > 0f ? mapDisplaySize : 160f;

        float viewportX = (normalized.x - currentViewportUv.x) / currentViewportUv.width;
        float viewportY = (normalized.y - currentViewportUv.y) / currentViewportUv.height;

        return new Vector2(
            (viewportX - 0.5f) * size,
            (viewportY - 0.5f) * size);
    }

    private void UpdatePlayerBlip(Vector2 playerPos, Vector2 facing)
    {
        if (playerBlip == null || blipContainer == null)
        {
            return;
        }

        playerBlip.enabled = true;
        playerBlip.rectTransform.anchoredPosition = WorldToBlipPosition(playerPos);
        playerBlip.transform.SetAsLastSibling();

        if (facing.sqrMagnitude > 0.001f)
        {
            float angle = Mathf.Atan2(facing.y, facing.x) * Mathf.Rad2Deg - 90f;
            playerBlip.rectTransform.localRotation = Quaternion.Euler(0f, 0f, angle);
        }
    }

    private void UpdateEnemyBlips()
    {
        IReadOnlyList<EnemyController> enemies = ActiveEnemyRegistry.All;
        int blipIndex = 0;
        float halfViewport = GetViewportRadiusPixels();

        for (int i = 0; i < enemies.Count && blipIndex < enemyBlips.Count; i++)
        {
            EnemyController enemy = enemies[i];
            if (enemy == null)
            {
                continue;
            }

            Vector2 blipPos = WorldToBlipPosition(GetMapPosition(enemy.gameObject));
            if (blipPos.sqrMagnitude > halfViewport * halfViewport)
            {
                continue;
            }

            Image blip = enemyBlips[blipIndex];
            blip.gameObject.SetActive(true);
            blip.color = GetElementColor(enemy.element);
            blip.rectTransform.anchoredPosition = blipPos;
            blipIndex++;
        }

        for (int i = blipIndex; i < enemyBlips.Count; i++)
        {
            enemyBlips[i].gameObject.SetActive(false);
        }
    }

    private void HideEnemyBlips()
    {
        for (int i = 0; i < enemyBlips.Count; i++)
        {
            enemyBlips[i].gameObject.SetActive(false);
        }
    }

    private float GetViewportRadiusPixels()
    {
        float size = mapDisplaySize > 0f ? mapDisplaySize : 160f;
        return size * 0.5f;
    }

    private Vector2 GetPlayerMovementDirection(GameObject player)
    {
        Rigidbody2D? rb = player.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            Vector2 velocity = rb.linearVelocity;
            if (velocity.sqrMagnitude > 0.05f)
            {
                lastPlayerMoveDirection = velocity.normalized;
            }
        }

        return lastPlayerMoveDirection;
    }

    private static Color GetElementColor(Element element)
    {
        return element switch
        {
            Element.Fire => FireColor,
            Element.Ice => IceColor,
            Element.Lightning => LightningColor,
            _ => PhysicalColor
        };
    }

    private static void DestroyTexture(ref Texture2D? texture)
    {
        if (texture == null)
        {
            return;
        }

        Destroy(texture);
        texture = null;
    }
}
