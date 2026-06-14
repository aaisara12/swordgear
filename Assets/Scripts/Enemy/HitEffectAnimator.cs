using UnityEngine;
using System.Collections;

public class HitEffectAnimator : MonoBehaviour, IAttackAnimator, IPoolReset
{
    [SerializeField] float effectDuration = 0.5f;
    [SerializeField] ParticleSystem effect;

    public void PlayAnimation()
    {
        StartCoroutine(PlayEffect());
    }

    public void OnSpawned() { }

    public void OnReleased() { }

    IEnumerator PlayEffect()
    {
        effect.Play();
        yield return new WaitForSeconds(effectDuration);
        PrefabPool.Instance?.Release(gameObject);
    }
}
