#nullable enable annotations

using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Shows a brief on-screen banner and plays an audio cue when a new wave is incoming,
/// giving the player feedback and a sense of respite between waves. Lives on the persistent
/// CombatHUD and listens to <see cref="LevelLoader"/>'s static wave events so it survives Arena reloads.
/// </summary>
public class WaveAnnouncer : MonoBehaviour
{
    [Header("Banner")]
    [SerializeField] private CanvasGroup? bannerGroup;
    [SerializeField] private TMP_Text? bannerText;
    [SerializeField] private string waveLabelFormat = "WAVE {0}";

    [Header("Timing")]
    [SerializeField] private float fadeInDuration = 0.2f;
    [SerializeField] private float holdDuration = 1.2f;
    [SerializeField] private float fadeOutDuration = 0.4f;

    [Header("Audio")]
    [SerializeField] private AudioSystem.Sound incomingSound = AudioSystem.Sound.Sword_Stick;
    [SerializeField] private bool playClearedSound = true;
    [SerializeField] private AudioSystem.Sound clearedSound = AudioSystem.Sound.Bounce;

    private Coroutine? _bannerRoutine;

    private void Awake()
    {
        if (bannerGroup != null)
        {
            bannerGroup.alpha = 0f;
        }
    }

    private void OnEnable()
    {
        LevelLoader.OnWaveIncoming += HandleWaveIncoming;
        LevelLoader.OnWaveCleared += HandleWaveCleared;
    }

    private void OnDisable()
    {
        LevelLoader.OnWaveIncoming -= HandleWaveIncoming;
        LevelLoader.OnWaveCleared -= HandleWaveCleared;
    }

    private void HandleWaveIncoming(int waveNumber)
    {
        if (bannerText != null)
        {
            bannerText.text = string.Format(waveLabelFormat, waveNumber);
        }

        AudioSystem.Play(incomingSound);

        if (_bannerRoutine != null)
        {
            StopCoroutine(_bannerRoutine);
        }
        _bannerRoutine = StartCoroutine(BannerRoutine());
    }

    private void HandleWaveCleared()
    {
        if (playClearedSound)
        {
            AudioSystem.Play(clearedSound);
        }
    }

    private IEnumerator BannerRoutine()
    {
        if (bannerGroup == null)
        {
            _bannerRoutine = null;
            yield break;
        }

        yield return Fade(bannerGroup, 1f, fadeInDuration);

        if (holdDuration > 0f)
        {
            yield return new WaitForSeconds(holdDuration);
        }

        yield return Fade(bannerGroup, 0f, fadeOutDuration);
        _bannerRoutine = null;
    }

    private static IEnumerator Fade(CanvasGroup group, float targetAlpha, float duration)
    {
        if (duration <= 0f)
        {
            group.alpha = targetAlpha;
            yield break;
        }

        float startAlpha = group.alpha;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            group.alpha = Mathf.Lerp(startAlpha, targetAlpha, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }
        group.alpha = targetAlpha;
    }
}
