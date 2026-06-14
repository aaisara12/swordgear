using UnityEngine;
using System.Collections;

public class AnimatorEffectController : MonoBehaviour, IAttackAnimator, IPoolReset
{
    [SerializeField] string animName;
    [SerializeField] float duration = 1f;

    Animator anim = null!;

    public void PlayAnimation()
    {
        StartCoroutine(PlayAnimAndDestroy());
    }

    public void OnSpawned() { }

    public void OnReleased() { }

    void Awake()
    {
        anim = GetComponent<Animator>();
    }

    IEnumerator PlayAnimAndDestroy()
    {
        anim.Play(animName);
        yield return new WaitForSeconds(duration);
        PrefabPool.Instance?.Release(gameObject);
    }
}
