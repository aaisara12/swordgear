#nullable enable

using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;

/// <summary>
/// Shows a brief on-screen banner and plays an audio cue when a new wave is incoming.
/// </summary>
public class WaveAnnouncer : MonoBehaviour
{
    [Header("Banner")]
    [SerializeField] private CanvasGroup? bannerGroup;
    [SerializeField] private RectTransform? bannerRect;
    [SerializeField] private TMP_Text? bannerText;
    [SerializeField] private string waveLabelFormat = "WAVE {0}";

    [Header("Timing")]
    [SerializeField] private float fadeInDuration = 0.28f;
    [SerializeField] private float holdDuration = 1.1f;
    [SerializeField] private float fadeOutDuration = 0.35f;

    [Header("Motion")]
    [SerializeField] private float introYOffset = 72f;
    [SerializeField] private float introStartScale = 0.35f;
    [SerializeField] private float punchScale = 1.14f;

    [Header("Audio")]
    [SerializeField] private AudioSystem.Sound incomingSound = AudioSystem.Sound.Sword_Stick;
    [SerializeField] private bool playClearedSound = true;
    [SerializeField] private AudioSystem.Sound clearedSound = AudioSystem.Sound.Bounce;

    private Coroutine? _bannerRoutine;
    private Vector2 _bannerRestAnchoredPosition;

    private void Awake()
    {
        if (bannerRect == null && bannerGroup != null)
        {
            bannerRect = bannerGroup.transform as RectTransform;
        }

        if (bannerGroup != null)
        {
            bannerGroup.alpha = 0f;
        }

        if (bannerRect != null)
        {
            _bannerRestAnchoredPosition = bannerRect.anchoredPosition;
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
        DOTween.Kill(this);
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
        if (bannerGroup == null || bannerRect == null)
        {
            _bannerRoutine = null;
            yield break;
        }

        DOTween.Kill(this);

        bannerGroup.alpha = 0f;
        bannerRect.localScale = Vector3.one * introStartScale;
        bannerRect.anchoredPosition = _bannerRestAnchoredPosition + new Vector2(0f, introYOffset);

        Sequence intro = DOTween.Sequence().SetId(this);
        intro.Join(DOTween.To(() => bannerGroup.alpha, value => bannerGroup.alpha = value, 1f, fadeInDuration));
        intro.Join(
            bannerRect
                .DOScale(punchScale, fadeInDuration)
                .SetEase(Ease.OutBack));
        intro.Join(
            bannerRect
                .DOAnchorPos(_bannerRestAnchoredPosition, fadeInDuration)
                .SetEase(Ease.OutCubic));
        yield return intro.WaitForCompletion();

        bannerRect.localScale = Vector3.one;

        if (holdDuration > 0f)
        {
            yield return new WaitForSeconds(holdDuration);
        }

        Sequence outro = DOTween.Sequence().SetId(this);
        outro.Join(DOTween.To(() => bannerGroup.alpha, value => bannerGroup.alpha = value, 0f, fadeOutDuration));
        outro.Join(
            bannerRect
                .DOScale(1.08f, fadeOutDuration)
                .SetEase(Ease.InQuad));
        outro.Join(
            bannerRect
                .DOAnchorPos(_bannerRestAnchoredPosition + new Vector2(0f, -36f), fadeOutDuration)
                .SetEase(Ease.InQuad));
        yield return outro.WaitForCompletion();

        bannerRect.localScale = Vector3.one;
        bannerRect.anchoredPosition = _bannerRestAnchoredPosition;
        bannerGroup.alpha = 0f;
        _bannerRoutine = null;
    }
}
