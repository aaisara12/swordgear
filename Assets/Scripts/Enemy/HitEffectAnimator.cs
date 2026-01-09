using UnityEngine;
using System.Collections;

public class HitEffectAnimator : MonoBehaviour, IAttackAnimator
{
    [SerializeField] float effectDuration = 0.5f;
    [SerializeField] ParticleSystem effect;

    public void PlayAnimation()
    {
        StartCoroutine(PlayEffect());
    }

    IEnumerator PlayEffect()
    {
        effect.Play();
        yield return new WaitForSeconds(effectDuration);
        Destroy(gameObject);
    }
}
