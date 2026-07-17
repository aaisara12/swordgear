#nullable enable

using System.Collections;
using UnityEngine;

/// <summary>
/// Telegraph rectangle along aim line, then a brief laser hitbox that damages the player inside the beam.
/// </summary>
public class EnemyBeamLaser : MonoBehaviour, IPoolReset
{
    [SerializeField] private SpriteRenderer? telegraphGlowRenderer;
    [SerializeField] private SpriteRenderer? telegraphEdgeRenderer;
    [SerializeField] private SpriteRenderer? telegraphFillRenderer;
    [SerializeField] private SpriteRenderer? beamOuterGlowRenderer;
    [SerializeField] private SpriteRenderer? beamMidGlowRenderer;
    [SerializeField] private SpriteRenderer? beamCoreRenderer;
    [SerializeField] private SpriteRenderer? beamHotCoreRenderer;
    [SerializeField] private BoxCollider2D? damageCollider;
    [SerializeField] private ParticleSystem? telegraphParticles;
    [SerializeField] private ParticleSystem? beamParticles;
    [SerializeField] private ParticleSystem? muzzleFlashParticles;
    [SerializeField] private float beamLength = 16.5f;
    [SerializeField] private float beamWidth = 0.38f;
    [SerializeField] private float beamActiveDuration = 0.9f;
    // Arena floor tilemap is sortingOrder 0; enemy body is 2. Render beam/telegraph at 1.
    [SerializeField] private int sortingOrder = 1;

    private Element attackerElement = Element.Physical;
    private float damage;
    private bool beamActive;
    private bool playerDamagedThisBeam;
    private Coroutine? sequenceRoutine;
    private Transform? attachTransform;
    private Quaternion lockedWorldRotation = Quaternion.identity;
    private Color telegraphGlowBaseColor = new Color(1f, 0.45f, 0.08f, 0.22f);
    private Color telegraphEdgeBaseColor = new Color(1f, 0.55f, 0.1f, 0.9f);
    private Color telegraphFillBaseColor = new Color(0.45f, 0.08f, 0.05f, 0.35f);

    public float BeamActiveDuration => beamActiveDuration;

    public void OnSpawned()
    {
        beamActive = false;
        playerDamagedThisBeam = false;
        attachTransform = null;
        StopAllParticleSystems();
        if (damageCollider != null)
        {
            damageCollider.enabled = false;
        }
    }

    public void OnReleased()
    {
        beamActive = false;
        attachTransform = null;
        if (sequenceRoutine != null)
        {
            StopCoroutine(sequenceRoutine);
            sequenceRoutine = null;
        }

        ShowTelegraph(false);
        ShowBeam(false);
        StopAllParticleSystems();
        if (damageCollider != null)
        {
            damageCollider.enabled = false;
        }
    }

    public void Begin(
        Transform attachPoint,
        Vector2 direction,
        Element element,
        float finalDamage,
        float telegraphDuration)
    {
        attachTransform = attachPoint;
        attackerElement = element;
        damage = finalDamage;
        playerDamagedThisBeam = false;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
        lockedWorldRotation = Quaternion.Euler(0f, 0f, angle);
        SyncToAttachPoint();

        ApplyElementColors(element);
        LayoutVisuals();
        ShowTelegraph(true);
        ShowBeam(false);
        PlayTelegraphParticles();

        if (damageCollider != null)
        {
            damageCollider.enabled = false;
        }

        if (sequenceRoutine != null)
        {
            StopCoroutine(sequenceRoutine);
        }

        sequenceRoutine = StartCoroutine(BeamSequence(telegraphDuration));
    }

    /// <summary>
    /// Immediately end this beam if it still belongs to <paramref name="owner"/>. Called when the firing
    /// enemy dies mid-beam so the laser doesn't hang frozen in the air — or keep dealing damage — after the
    /// enemy is gone. No-op if the beam has already finished or been recycled onto another enemy.
    /// </summary>
    public void TerminateIfOwnedBy(Transform owner)
    {
        if (attachTransform != owner)
        {
            return;
        }

        if (sequenceRoutine != null)
        {
            StopCoroutine(sequenceRoutine);
            sequenceRoutine = null;
        }

        beamActive = false;
        attachTransform = null;
        ShowTelegraph(false);
        ShowBeam(false);
        StopAllParticleSystems();
        if (damageCollider != null)
        {
            damageCollider.enabled = false;
        }

        PrefabPool.Instance?.Release(gameObject);
    }

    private void LateUpdate()
    {
        SyncToAttachPoint();
    }

    private void SyncToAttachPoint()
    {
        if (attachTransform == null)
        {
            return;
        }

        transform.position = attachTransform.position;
        transform.rotation = lockedWorldRotation;
    }

    private IEnumerator BeamSequence(float telegraphDuration)
    {
        Coroutine pulseRoutine = StartCoroutine(PulseTelegraph(telegraphDuration));
        yield return new WaitForSeconds(telegraphDuration);
        if (pulseRoutine != null)
        {
            StopCoroutine(pulseRoutine);
        }

        StopTelegraphParticles();
        ShowTelegraph(false);
        ShowBeam(true);
        PlayBeamParticles();
        beamActive = true;

        if (damageCollider != null)
        {
            damageCollider.enabled = true;
        }

        ApplyDamageToOverlapping();

        yield return new WaitForSeconds(beamActiveDuration);

        beamActive = false;
        if (damageCollider != null)
        {
            damageCollider.enabled = false;
        }

        ShowBeam(false);
        StopBeamParticles();
        attachTransform = null;
        sequenceRoutine = null;
        PrefabPool.Instance?.Release(gameObject);
    }

    private IEnumerator PulseTelegraph(float duration)
    {
        float elapsed = 0f;
        Vector3 beamCenter = new Vector3(0f, beamLength * 0.5f, 0f);
        while (elapsed < duration)
        {
            float fastPulse = Mathf.PingPong(elapsed * 9f, 1f);
            float slowPulse = (Mathf.Sin(elapsed * 4.5f) + 1f) * 0.5f;
            float pulse = Mathf.Lerp(slowPulse, fastPulse, 0.6f);
            float shimmer = (Mathf.Sin(elapsed * 14f) + 1f) * 0.5f;

            if (telegraphGlowRenderer != null)
            {
                Color glow = telegraphGlowBaseColor;
                float glowAlpha = Mathf.Lerp(0.15f, 1f, pulse);
                glow.a = telegraphGlowBaseColor.a * glowAlpha;
                glow.r = Mathf.Lerp(glow.r * 0.65f, glow.r * 1.2f, pulse);
                glow.g = Mathf.Lerp(glow.g * 0.65f, glow.g * 1.2f, pulse);
                glow.b = Mathf.Lerp(glow.b * 0.65f, glow.b * 1.2f, pulse);
                telegraphGlowRenderer.color = glow;

                float glowWidth = Mathf.Lerp(1f, 1.45f, pulse);
                LayoutSpriteRect(
                    telegraphGlowRenderer,
                    new Vector2(beamWidth * 2.1f * glowWidth, beamLength),
                    beamCenter,
                    sortingOrder);
            }

            if (telegraphEdgeRenderer != null)
            {
                Color edge = telegraphEdgeBaseColor;
                float edgeAlpha = Mathf.Lerp(0.3f, 1f, pulse);
                edge.a = telegraphEdgeBaseColor.a * edgeAlpha;
                float brighten = Mathf.Lerp(0.75f, 1.15f + shimmer * 0.15f, pulse);
                edge.r = Mathf.Min(1f, edge.r * brighten);
                edge.g = Mathf.Min(1f, edge.g * brighten);
                edge.b = Mathf.Min(1f, edge.b * brighten);
                telegraphEdgeRenderer.color = edge;

                float edgeWidth = Mathf.Lerp(1f, 1.28f, pulse);
                LayoutSpriteRect(
                    telegraphEdgeRenderer,
                    new Vector2(beamWidth * 1.35f * edgeWidth, beamLength),
                    beamCenter,
                    sortingOrder);
            }

            if (telegraphFillRenderer != null)
            {
                float fillPulse = Mathf.PingPong(elapsed * 7f + 0.35f, 1f);
                Color fill = telegraphFillBaseColor;
                float fillAlpha = Mathf.Lerp(0.18f, 1f, fillPulse);
                fill.a = telegraphFillBaseColor.a * fillAlpha;
                float fillBrighten = Mathf.Lerp(0.7f, 1.1f, fillPulse);
                fill.r = Mathf.Min(1f, fill.r * fillBrighten);
                fill.g = Mathf.Min(1f, fill.g * fillBrighten);
                fill.b = Mathf.Min(1f, fill.b * fillBrighten);
                telegraphFillRenderer.color = fill;

                float fillWidth = Mathf.Lerp(0.92f, 1.12f, fillPulse);
                LayoutSpriteRect(
                    telegraphFillRenderer,
                    new Vector2(beamWidth * 0.7f * fillWidth, beamLength * 0.96f),
                    beamCenter,
                    sortingOrder);
            }

            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!beamActive)
        {
            return;
        }

        TryDamagePlayer(other);
    }

    private void ApplyDamageToOverlapping()
    {
        if (damageCollider == null)
        {
            return;
        }

        ContactFilter2D filter = new ContactFilter2D
        {
            useTriggers = true,
            useLayerMask = false,
        };

        Collider2D[] hits = new Collider2D[8];
        int count = damageCollider.Overlap(filter, hits);
        for (int i = 0; i < count; i++)
        {
            TryDamagePlayer(hits[i]);
        }
    }

    private void TryDamagePlayer(Collider2D other)
    {
        if (playerDamagedThisBeam || !other.CompareTag("Player"))
        {
            return;
        }

        PlayerController? player = other.GetComponent<PlayerController>();
        if (player == null || GameManager.Instance == null)
        {
            return;
        }

        Element defenderElement = GameManager.Instance.currentElement;
        // Attunement: a same-element beam passes through harmlessly — no hit, and don't latch
        // playerDamagedThisBeam so it can still connect if the player swaps element mid-beam.
        if (GameManager.Instance.IsAttunementBlocked(attackerElement, defenderElement))
        {
            return;
        }

        playerDamagedThisBeam = true;

        float finalDamage = GameManager.Instance.CalculateDamage(defenderElement, attackerElement, damage);
        player.TakeDamage(finalDamage);
    }

    private void LayoutVisuals()
    {
        Vector3 beamCenter = new Vector3(0f, beamLength * 0.5f, 0f);

        // All layers share the same order (between floor=0 and enemy=2); child order controls overlap.
        LayoutSpriteRect(telegraphGlowRenderer, new Vector2(beamWidth * 2.1f, beamLength), beamCenter, sortingOrder);
        LayoutSpriteRect(telegraphEdgeRenderer, new Vector2(beamWidth * 1.35f, beamLength), beamCenter, sortingOrder);
        LayoutSpriteRect(telegraphFillRenderer, new Vector2(beamWidth * 0.7f, beamLength * 0.96f), beamCenter, sortingOrder);

        // Wide soft glow -> narrow saturated core -> thin bright center line.
        LayoutSpriteRect(beamOuterGlowRenderer, new Vector2(beamWidth * 2.2f, beamLength), beamCenter, sortingOrder);
        LayoutSpriteRect(beamMidGlowRenderer, new Vector2(beamWidth * 1.3f, beamLength), beamCenter, sortingOrder);
        LayoutSpriteRect(beamCoreRenderer, new Vector2(beamWidth * 0.58f, beamLength), beamCenter, sortingOrder);
        LayoutSpriteRect(beamHotCoreRenderer, new Vector2(beamWidth * 0.2f, beamLength), beamCenter, sortingOrder);

        LayoutParticleShape(telegraphParticles, new Vector2(beamWidth * 1.18f, beamLength), beamCenter);
        LayoutParticleShape(beamParticles, new Vector2(beamWidth, beamLength), beamCenter);
        if (muzzleFlashParticles != null)
        {
            muzzleFlashParticles.transform.localPosition = Vector3.zero;
        }

        SetParticleSorting(telegraphParticles, sortingOrder);
        SetParticleSorting(beamParticles, sortingOrder);
        SetParticleSorting(muzzleFlashParticles, sortingOrder);

        if (damageCollider != null)
        {
            damageCollider.size = new Vector2(beamWidth, beamLength);
            damageCollider.offset = new Vector2(0f, beamLength * 0.5f);
        }
    }

    private static void LayoutSpriteRect(
        SpriteRenderer? renderer,
        Vector2 size,
        Vector3 localCenter,
        int order)
    {
        if (renderer == null)
        {
            return;
        }

        renderer.transform.localPosition = localCenter;
        renderer.transform.localScale = Vector3.one;
        renderer.drawMode = SpriteDrawMode.Sliced;
        renderer.size = size;
        renderer.sortingOrder = order;
    }

    private static void LayoutParticleShape(ParticleSystem? particles, Vector2 size, Vector3 localCenter)
    {
        if (particles == null)
        {
            return;
        }

        particles.transform.localPosition = localCenter;
        ParticleSystem.ShapeModule shape = particles.shape;
        shape.scale = new Vector3(size.x, size.y, 0f);
    }

    private static void SetParticleSorting(ParticleSystem? particles, int order)
    {
        if (particles == null)
        {
            return;
        }

        ParticleSystemRenderer? renderer = particles.GetComponent<ParticleSystemRenderer>();
        if (renderer != null)
        {
            renderer.sortingOrder = order;
        }
    }

    private void ApplyElementColors(Element element)
    {
        switch (element)
        {
            case Element.Fire:
                telegraphGlowBaseColor = new Color(1f, 0.45f, 0.08f, 0.28f);
                telegraphEdgeBaseColor = new Color(1f, 0.62f, 0.12f, 0.92f);
                telegraphFillBaseColor = new Color(0.55f, 0.08f, 0.02f, 0.38f);
                SetBeamColors(
                    new Color(1f, 0.28f, 0.05f, 0.32f),
                    new Color(1f, 0.4f, 0.08f, 0.58f),
                    new Color(1f, 0.55f, 0.18f, 0.95f),
                    new Color(1f, 0.95f, 0.75f, 1f));
                TintParticles(new Color(1f, 0.55f, 0.2f, 1f), new Color(1f, 0.85f, 0.35f, 1f));
                break;
            case Element.Ice:
                telegraphGlowBaseColor = new Color(0.3f, 0.75f, 1f, 0.28f);
                telegraphEdgeBaseColor = new Color(0.45f, 0.88f, 1f, 0.92f);
                telegraphFillBaseColor = new Color(0.05f, 0.2f, 0.45f, 0.38f);
                SetBeamColors(
                    new Color(0.1f, 0.4f, 0.95f, 0.32f),
                    new Color(0.2f, 0.55f, 1f, 0.58f),
                    new Color(0.4f, 0.78f, 1f, 0.95f),
                    new Color(0.9f, 0.99f, 1f, 1f));
                TintParticles(new Color(0.45f, 0.85f, 1f, 1f), new Color(0.75f, 0.95f, 1f, 1f));
                break;
            case Element.Lightning:
                telegraphGlowBaseColor = new Color(1f, 0.95f, 0.2f, 0.28f);
                telegraphEdgeBaseColor = new Color(1f, 0.98f, 0.35f, 0.92f);
                telegraphFillBaseColor = new Color(0.35f, 0.3f, 0.02f, 0.38f);
                SetBeamColors(
                    new Color(0.8f, 0.72f, 0.05f, 0.32f),
                    new Color(0.95f, 0.85f, 0.1f, 0.58f),
                    new Color(1f, 0.95f, 0.3f, 0.95f),
                    new Color(1f, 1f, 0.9f, 1f));
                TintParticles(new Color(1f, 0.98f, 0.45f, 1f), new Color(1f, 1f, 0.75f, 1f));
                break;
            case Element.Wind:
                telegraphGlowBaseColor = new Color(0.3f, 1f, 0.4f, 0.28f);
                telegraphEdgeBaseColor = new Color(0.5f, 1f, 0.55f, 0.92f);
                telegraphFillBaseColor = new Color(0.05f, 0.3f, 0.1f, 0.38f);
                SetBeamColors(
                    new Color(0.1f, 0.7f, 0.2f, 0.32f),
                    new Color(0.2f, 0.85f, 0.35f, 0.58f),
                    new Color(0.45f, 1f, 0.55f, 0.95f),
                    new Color(0.85f, 1f, 0.9f, 1f));
                TintParticles(new Color(0.45f, 1f, 0.5f, 1f), new Color(0.8f, 1f, 0.85f, 1f));
                break;
            default:
                telegraphGlowBaseColor = new Color(0.55f, 0.72f, 1f, 0.28f);
                telegraphEdgeBaseColor = new Color(0.82f, 0.9f, 1f, 0.92f);
                telegraphFillBaseColor = new Color(0.12f, 0.18f, 0.32f, 0.38f);
                SetBeamColors(
                    new Color(0.35f, 0.55f, 0.95f, 0.32f),
                    new Color(0.45f, 0.65f, 1f, 0.58f),
                    new Color(0.62f, 0.8f, 1f, 0.95f),
                    new Color(0.97f, 0.99f, 1f, 1f));
                TintParticles(new Color(0.7f, 0.85f, 1f, 1f), new Color(0.95f, 0.98f, 1f, 1f));
                break;
        }

        if (telegraphGlowRenderer != null)
        {
            telegraphGlowRenderer.color = telegraphGlowBaseColor;
        }

        if (telegraphEdgeRenderer != null)
        {
            telegraphEdgeRenderer.color = telegraphEdgeBaseColor;
        }

        if (telegraphFillRenderer != null)
        {
            telegraphFillRenderer.color = telegraphFillBaseColor;
        }
    }

    private void SetBeamColors(Color outer, Color mid, Color core, Color hot)
    {
        if (beamOuterGlowRenderer != null)
        {
            beamOuterGlowRenderer.color = outer;
        }

        if (beamMidGlowRenderer != null)
        {
            beamMidGlowRenderer.color = mid;
        }

        if (beamCoreRenderer != null)
        {
            beamCoreRenderer.color = core;
        }

        if (beamHotCoreRenderer != null)
        {
            beamHotCoreRenderer.color = hot;
        }
    }

    private void TintParticles(Color beamColor, Color flashColor)
    {
        TintParticleSystem(telegraphParticles, beamColor);
        TintParticleSystem(beamParticles, beamColor);
        TintParticleSystem(muzzleFlashParticles, flashColor);
    }

    private static void TintParticleSystem(ParticleSystem? particles, Color color)
    {
        if (particles == null)
        {
            return;
        }

        ParticleSystem.MainModule main = particles.main;
        main.startColor = color;
    }

    private void ShowTelegraph(bool visible)
    {
        if (telegraphGlowRenderer != null)
        {
            telegraphGlowRenderer.enabled = visible;
        }

        if (telegraphEdgeRenderer != null)
        {
            telegraphEdgeRenderer.enabled = visible;
        }

        if (telegraphFillRenderer != null)
        {
            telegraphFillRenderer.enabled = visible;
        }
    }

    private void ShowBeam(bool visible)
    {
        if (beamOuterGlowRenderer != null)
        {
            beamOuterGlowRenderer.enabled = visible;
        }

        if (beamMidGlowRenderer != null)
        {
            beamMidGlowRenderer.enabled = visible;
        }

        if (beamCoreRenderer != null)
        {
            beamCoreRenderer.enabled = visible;
        }

        if (beamHotCoreRenderer != null)
        {
            beamHotCoreRenderer.enabled = visible;
        }
    }

    private void PlayTelegraphParticles()
    {
        if (telegraphParticles != null)
        {
            telegraphParticles.Clear(true);
            telegraphParticles.Play(true);
        }
    }

    private void StopTelegraphParticles()
    {
        if (telegraphParticles != null)
        {
            telegraphParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    private void PlayBeamParticles()
    {
        if (beamParticles != null)
        {
            beamParticles.Clear(true);
            beamParticles.Play(true);
        }

        if (muzzleFlashParticles != null)
        {
            muzzleFlashParticles.Clear(true);
            muzzleFlashParticles.Play(true);
        }
    }

    private void StopBeamParticles()
    {
        if (beamParticles != null)
        {
            beamParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        if (muzzleFlashParticles != null)
        {
            muzzleFlashParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    private void StopAllParticleSystems()
    {
        StopTelegraphParticles();
        StopBeamParticles();
    }
}
