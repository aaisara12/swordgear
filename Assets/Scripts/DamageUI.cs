using UnityEngine;
using System.Collections;
using TMPro;

public class DamageUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] TMP_Text display;
    [SerializeField] GameObject physicalHitEffect;
    [SerializeField] GameObject fireHitEffect;
    [SerializeField] GameObject iceHitEffect;
    [SerializeField] GameObject lightningHitEffect;

    [Header("Timing")]
    [SerializeField] float duration = 0.5f;

    [Header("Size Scaling (Linear)")]
    [SerializeField] float minSize = 0.5f;
    [SerializeField] float maxSize = 1.8f;
    [SerializeField] float minDamage = 5f;
    [SerializeField] float maxDamage = 30f;

    [Header("Pop Animation")]
    [SerializeField] float popOvershoot = 0.3f;   // % above target scale
    [SerializeField] float popDuration = 0.12f;
    [SerializeField]
    AnimationCurve popCurve =
        AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    public void ShowNumber(float amt, Element element)
    {
        StartCoroutine(Show(amt, element));
    }

    // Only three elements have authored hit VFX; everything else uses the physical burst. Wind already
    // did before this was a method.
    GameObject ResolveHitEffect(Element element)
    {
        switch (element)
        {
            case Element.Fire: return fireHitEffect;
            case Element.Ice: return iceHitEffect;
            case Element.Lightning: return lightningHitEffect;
            default: return physicalHitEffect;
        }
    }

    IEnumerator Show(float num, Element element)
    {
        // Colour comes from ElementVisuals so damage numbers can't drift from the ring, charge VFX and
        // projectiles. This used to be a second hardcoded palette, which left every element it didn't
        // list — Earth, Dark and Light — rendering white.
        Color color = ElementVisuals.GetColor(element);
        GameObject effectObject = PrefabPool.Instance!.Spawn(ResolveHitEffect(element), transform.position, Quaternion.identity);
        IAttackAnimator effect = effectObject.GetComponent<IAttackAnimator>();


        display.text = $"{Mathf.RoundToInt(num)}";
        display.color = color;

        float t = Mathf.InverseLerp(minDamage, maxDamage, num);
        float targetScale = Mathf.Lerp(minSize, maxSize, t);

        float elapsedPop = 0f;

        float popScale = targetScale * (1f + popOvershoot);

        transform.localScale = Vector3.one * (targetScale * 0.6f);

        effect.PlayAnimation();

        while (elapsedPop < popDuration)
        {
            float p = elapsedPop / popDuration;

            float curved = popCurve.Evaluate(p);

            float scale = Mathf.Lerp(popScale, targetScale, curved);
            transform.localScale = Vector3.one * scale;

            elapsedPop += Time.deltaTime;
            yield return null;
        }

        transform.localScale = Vector3.one * targetScale;

        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            float alpha = Mathf.Lerp(1f, 0f, elapsedTime / duration);
            display.color = new Color(display.color.r, display.color.g, display.color.b, alpha);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        PrefabPool.Instance!.Release(effectObject);
        PrefabPool.Instance!.Release(gameObject);
    }
}
