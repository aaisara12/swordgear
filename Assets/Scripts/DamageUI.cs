using UnityEngine;
using System.Collections;
using TMPro;

public class DamageUI : MonoBehaviour
{
    [SerializeField] TMP_Text display;
    [SerializeField] float duration = 0.5f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        // display = GetComponent<TMP_Text>();
    }

    public void ShowNumber(float amt)
    {
        StartCoroutine(Show(amt));
    }

    IEnumerator Show(float num)
    {
        display.text = $"{Mathf.RoundToInt(num)}";
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            float alpha = Mathf.Lerp(1f, 0f, elapsedTime / duration);
            display.color = new Color(display.color.r, display.color.g, display.color.b, alpha);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        Destroy(gameObject);
    }

}
