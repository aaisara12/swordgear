#nullable enable

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Plays the shared spawn Animator clip and keeps combat disabled until the pop-in finishes.
/// Elite aura is an authored child toggled on when <see cref="Begin"/> is called with isElite.
/// </summary>
public class EnemySpawnPresentation : MonoBehaviour
{
    [SerializeField] private Animator? visualAnimator;
    [SerializeField] private string spawnStateName = "Spawn";
    [SerializeField] private float spawnDurationFallback = 0.65f;
    [SerializeField] private GameObject? eliteAuraChild;
    [SerializeField] private Collider2D? bodyCollider;

    private readonly List<Behaviour> _disabledForSpawn = new();
    private Rigidbody2D? _rb;
    private Coroutine? _routine;
    private bool _presentationComplete;

    public bool IsPresentationComplete => _presentationComplete;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        if (bodyCollider == null)
        {
            bodyCollider = GetComponent<Collider2D>();
        }

        if (eliteAuraChild != null)
        {
            eliteAuraChild.SetActive(false);
        }
    }

    /// <summary>Starts spawn presentation. Call after ApplySpawnModifiers so elite scale is already applied.</summary>
    public void Begin(bool isElite)
    {
        _presentationComplete = false;
        if (eliteAuraChild != null)
        {
            eliteAuraChild.SetActive(isElite);
        }

        if (_routine != null)
        {
            StopCoroutine(_routine);
        }

        _routine = StartCoroutine(SpawnRoutine());
    }

    private IEnumerator SpawnRoutine()
    {
        DisableCombat();

        if (visualAnimator != null)
        {
            visualAnimator.Rebind();
            visualAnimator.Update(0f);
            visualAnimator.Play(spawnStateName, 0, 0f);
        }

        float elapsed = 0f;
        float timeout = Mathf.Max(0.1f, spawnDurationFallback);
        while (elapsed < timeout)
        {
            if (visualAnimator != null)
            {
                AnimatorStateInfo info = visualAnimator.GetCurrentAnimatorStateInfo(0);
                if (info.IsName(spawnStateName) && info.normalizedTime >= 1f)
                {
                    break;
                }
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        EnableCombat();
        _presentationComplete = true;
        _routine = null;
    }

    private void DisableCombat()
    {
        _disabledForSpawn.Clear();

        EnemyController? controller = GetComponent<EnemyController>();
        if (controller != null && controller.enabled)
        {
            controller.enabled = false;
            _disabledForSpawn.Add(controller);
        }

        MonoBehaviour[] behaviours = GetComponents<MonoBehaviour>();
        for (int i = 0; i < behaviours.Length; i++)
        {
            MonoBehaviour behaviour = behaviours[i];
            if (behaviour == null || !behaviour.enabled || behaviour == this)
            {
                continue;
            }

            if (behaviour is IAttackStrategy || behaviour is IMovementStrategy)
            {
                behaviour.enabled = false;
                _disabledForSpawn.Add(behaviour);
            }
        }

        if (bodyCollider != null && bodyCollider.enabled)
        {
            bodyCollider.enabled = false;
            _disabledForSpawn.Add(bodyCollider);
        }

        if (_rb != null)
        {
            _rb.linearVelocity = Vector2.zero;
        }
    }

    private void EnableCombat()
    {
        for (int i = 0; i < _disabledForSpawn.Count; i++)
        {
            Behaviour? behaviour = _disabledForSpawn[i];
            if (behaviour != null)
            {
                behaviour.enabled = true;
            }
        }

        _disabledForSpawn.Clear();
    }
}
