#nullable enable

using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Cycles "Loading" through "", ".", "..", "..." for an indeterminate loading label.
/// </summary>
public class LoadingDotsText : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI? label;
    [SerializeField] private string baseText = "Loading";
    [SerializeField, Min(0.05f)] private float secondsPerStep = 0.4f;

    static readonly string[] DotSteps = { "", ".", "..", "..." };

    Coroutine? _loop;
    int _step;

    void Awake()
    {
        if (label == null)
            label = GetComponent<TextMeshProUGUI>();

        if (label == null)
        {
            Debug.LogError("LoadingDotsText: label is null");
            return;
        }

        ApplyStep(0);
    }

    void OnEnable()
    {
        Restart();
    }

    void OnDisable()
    {
        StopLoop();
    }

    public void Restart()
    {
        StopLoop();
        _step = 0;
        ApplyStep(0);
        if (isActiveAndEnabled)
            _loop = StartCoroutine(Loop());
    }

    void StopLoop()
    {
        if (_loop == null)
            return;
        StopCoroutine(_loop);
        _loop = null;
    }

    IEnumerator Loop()
    {
        var wait = new WaitForSecondsRealtime(secondsPerStep);
        while (true)
        {
            yield return wait;
            _step = (_step + 1) % DotSteps.Length;
            ApplyStep(_step);
        }
    }

    void ApplyStep(int step)
    {
        if (label == null)
            return;
        label.text = baseText + DotSteps[step];
    }
}
