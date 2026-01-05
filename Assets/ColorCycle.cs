using UnityEngine;
using UnityEngine.UI;

public class ColorCycle : MonoBehaviour
{
    public float speed = 0.5f; // How fast the colors change
    private Image img;
    private float hue = 0f;

    void Start()
    {
        // Cache the Image component
        img = GetComponent<Image>();
    }

    void Update()
    {
        // Increase hue over time based on speed
        hue += speed * Time.deltaTime;

        // Reset hue to 0 once it passes 1 to keep it in range
        if (hue > 1.0f)
        {
            hue -= 1.0f;
        }

        // Convert HSV back to RGB and apply to the image
        // S = 1 (Full saturation), V = 1 (Full brightness)
        img.color = Color.HSVToRGB(hue, 1f, 0.5f);
    }
}