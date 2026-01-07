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

    void Awake()
    {
        // if (!display) display = GetComponent<TMP_Text>();
    }

    public void ShowNumber(float amt, Element element)
    {
        StartCoroutine(Show(amt, element));
    }

    IEnumerator Show(float num, Element element)
    {
        Color color;
        IAttackAnimator effect;
        GameObject effectObject;

        switch (element)
        {
            case Element.Fire:
                color = Color.red;
                effectObject = Instantiate(fireHitEffect, transform.position, Quaternion.identity);
                effect = effectObject.GetComponent<IAttackAnimator>();
                break;

            case Element.Lightning:
                color = Color.cyan;
                effectObject = Instantiate(lightningHitEffect, transform.position, Quaternion.identity);
                effect = effectObject.GetComponent<IAttackAnimator>();
                break;

            case Element.Ice:
                color = Color.blue;
                effectObject = Instantiate(iceHitEffect, transform.position, Quaternion.identity);
                effect = effectObject.GetComponent<IAttackAnimator>();
                break;

            default:
                color = Color.white;
                effectObject = Instantiate(physicalHitEffect, transform.position, Quaternion.identity);
                effect = effectObject.GetComponent<IAttackAnimator>();
                break;
        }


        display.text = $"{Mathf.RoundToInt(num)}";
        display.color = color;

        // --- SCALE BASED ON DAMAGE ---
        float t = Mathf.InverseLerp(minDamage, maxDamage, num);
        float targetScale = Mathf.Lerp(minSize, maxSize, t);

        // --- POP ANIMATION ---
        float elapsedPop = 0f;

        float popScale = targetScale * (1f + popOvershoot);

        // start slightly small for a snappier feel
        transform.localScale = Vector3.one * (targetScale * 0.6f);

        // play hit effect
        effect.PlayAnimation();

        while (elapsedPop < popDuration)
        {
            float p = elapsedPop / popDuration;

            // curve controls how snappy / bouncy the pop is
            float curved = popCurve.Evaluate(p);

            // interpolate from overshoot -> final size
            float scale = Mathf.Lerp(popScale, targetScale, curved);
            transform.localScale = Vector3.one * scale;

            elapsedPop += Time.deltaTime;
            yield return null;
        }

        // ensure exact final size
        transform.localScale = Vector3.one * targetScale;

        // --- FADE OUT ---
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            float alpha = Mathf.Lerp(1f, 0f, elapsedTime / duration);
            display.color = new Color(display.color.r, display.color.g, display.color.b, alpha);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        Destroy(effectObject);
        Destroy(gameObject);
    }
}
