#nullable enable

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tracks pool membership and restores cached component state on each spawn/release.
/// </summary>
[DisallowMultipleComponent]
public sealed class PooledInstance : MonoBehaviour
{
    GameObject? _sourcePrefab;
    Transform? _cachedTransform;
    Vector3 _defaultLocalScale;
    Quaternion _defaultLocalRotation;
    Vector3 _defaultLocalPosition;

    SpriteRenderer[]? _spriteRenderers;
    Color[]? _defaultSpriteColors;

    Animator[]? _animators;
    Collider2D[]? _colliders;
    bool[]? _defaultColliderEnabled;

    LineRenderer[]? _lineRenderers;
    Color[]? _defaultLineStartColors;
    Color[]? _defaultLineEndColors;

    Rigidbody2D[]? _rigidbodies;

    ParticleSystem[]? _particleSystems;
    ParticleSystemStopAction[]? _originalStopActions;

    IPoolReset[]? _poolResets;

    bool _isActive;
    Coroutine? _releaseCoroutine;

    public GameObject? SourcePrefab => _sourcePrefab;
    public bool IsActive => _isActive;

    public void Initialize(GameObject sourcePrefab)
    {
        _sourcePrefab = sourcePrefab;
        _cachedTransform = transform;
        _defaultLocalScale = _cachedTransform.localScale;
        _defaultLocalRotation = _cachedTransform.localRotation;
        _defaultLocalPosition = _cachedTransform.localPosition;

        CacheComponents();
        FixParticleStopActions();
    }

    void CacheComponents()
    {
        _spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        _defaultSpriteColors = new Color[_spriteRenderers.Length];
        for (int i = 0; i < _spriteRenderers.Length; i++)
            _defaultSpriteColors[i] = _spriteRenderers[i].color;

        _animators = GetComponentsInChildren<Animator>(true);

        _colliders = GetComponentsInChildren<Collider2D>(true);
        _defaultColliderEnabled = new bool[_colliders.Length];
        for (int i = 0; i < _colliders.Length; i++)
            _defaultColliderEnabled[i] = _colliders[i].enabled;

        _lineRenderers = GetComponentsInChildren<LineRenderer>(true);
        _defaultLineStartColors = new Color[_lineRenderers.Length];
        _defaultLineEndColors = new Color[_lineRenderers.Length];
        for (int i = 0; i < _lineRenderers.Length; i++)
        {
            _defaultLineStartColors[i] = _lineRenderers[i].startColor;
            _defaultLineEndColors[i] = _lineRenderers[i].endColor;
        }

        _rigidbodies = GetComponentsInChildren<Rigidbody2D>(true);

        _particleSystems = GetComponentsInChildren<ParticleSystem>(true);
        _originalStopActions = new ParticleSystemStopAction[_particleSystems.Length];

        _poolResets = GetComponentsInChildren<IPoolReset>(true);
    }

    void FixParticleStopActions()
    {
        if (_particleSystems == null) return;

        for (int i = 0; i < _particleSystems.Length; i++)
        {
            var main = _particleSystems[i].main;
            _originalStopActions![i] = main.stopAction;
            if (main.stopAction == ParticleSystemStopAction.Destroy)
            {
                main.stopAction = ParticleSystemStopAction.Callback;
            }
        }
    }

    public void OnSpawnedFromPool()
    {
        _isActive = true;
        if (_releaseCoroutine != null)
        {
            StopCoroutine(_releaseCoroutine);
            _releaseCoroutine = null;
        }

        ResetTransform();
        ResetSpriteRenderers();
        ResetAnimators();
        ResetColliders();
        ResetLineRenderers();
        ResetRigidbodies();
        ResetParticleSystems();

        if (_poolResets != null)
        {
            foreach (var reset in _poolResets)
                reset.OnSpawned();
        }
    }

    public void OnReleasedToPool()
    {
        if (!_isActive) return;
        _isActive = false;

        if (_releaseCoroutine != null)
        {
            StopCoroutine(_releaseCoroutine);
            _releaseCoroutine = null;
        }

        StopAllCoroutines();

        if (_particleSystems != null)
        {
            foreach (var ps in _particleSystems)
            {
                if (ps != null)
                    ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        if (_poolResets != null)
        {
            foreach (var reset in _poolResets)
                reset.OnReleased();
        }
    }

    void ResetTransform()
    {
        if (_cachedTransform == null) return;
        _cachedTransform.localScale = _defaultLocalScale;
        _cachedTransform.localRotation = _defaultLocalRotation;
        _cachedTransform.localPosition = _defaultLocalPosition;
    }

    void ResetSpriteRenderers()
    {
        if (_spriteRenderers == null || _defaultSpriteColors == null) return;
        for (int i = 0; i < _spriteRenderers.Length; i++)
            _spriteRenderers[i].color = _defaultSpriteColors[i];
    }

    void ResetAnimators()
    {
        if (_animators == null) return;
        foreach (var anim in _animators)
        {
            if (anim == null) continue;
            anim.Rebind();
            anim.Update(0f);
        }
    }

    void ResetColliders()
    {
        if (_colliders == null || _defaultColliderEnabled == null) return;
        for (int i = 0; i < _colliders.Length; i++)
            _colliders[i].enabled = _defaultColliderEnabled[i];
    }

    void ResetLineRenderers()
    {
        if (_lineRenderers == null || _defaultLineStartColors == null || _defaultLineEndColors == null) return;
        for (int i = 0; i < _lineRenderers.Length; i++)
        {
            _lineRenderers[i].positionCount = 0;
            _lineRenderers[i].startColor = _defaultLineStartColors[i];
            _lineRenderers[i].endColor = _defaultLineEndColors[i];
        }
    }

    void ResetRigidbodies()
    {
        if (_rigidbodies == null) return;
        foreach (var rb in _rigidbodies)
        {
            if (rb == null) continue;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
    }

    void ResetParticleSystems()
    {
        if (_particleSystems == null) return;
        foreach (var ps in _particleSystems)
        {
            if (ps == null) continue;
            ps.Clear(true);
        }
    }

    public void ReleaseAfter(float seconds)
    {
        if (_releaseCoroutine != null)
            StopCoroutine(_releaseCoroutine);
        _releaseCoroutine = StartCoroutine(ReleaseAfterRoutine(seconds));
    }

    IEnumerator ReleaseAfterRoutine(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        PrefabPool.Instance?.Release(gameObject);
    }

    public void ReleaseWhenParticlesDone()
    {
        if (_releaseCoroutine != null)
            StopCoroutine(_releaseCoroutine);
        _releaseCoroutine = StartCoroutine(ReleaseWhenParticlesDoneRoutine());
    }

    IEnumerator ReleaseWhenParticlesDoneRoutine()
    {
        if (_particleSystems == null || _particleSystems.Length == 0)
        {
            PrefabPool.Instance?.Release(gameObject);
            yield break;
        }

        float maxDuration = 0f;
        foreach (var ps in _particleSystems)
        {
            if (ps == null) continue;
            var main = ps.main;
            float duration = main.duration + main.startLifetime.constantMax;
            if (duration > maxDuration)
                maxDuration = duration;
        }

        yield return new WaitForSeconds(maxDuration);
        PrefabPool.Instance?.Release(gameObject);
    }

    void OnParticleSystemStopped()
    {
        if (!_isActive) return;
        if (_particleSystems == null) return;

        foreach (var ps in _particleSystems)
        {
            if (ps != null && ps.IsAlive(true))
                return;
        }

        PrefabPool.Instance?.Release(gameObject);
    }
}
