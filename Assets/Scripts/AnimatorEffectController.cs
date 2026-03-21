using UnityEngine;
using System.Collections;

public class AnimatorEffectController : MonoBehaviour, IAttackAnimator
{
    [SerializeField] string animName;
    [SerializeField] float duration = 1f;

    Animator anim;
    public void PlayAnimation()
    {
        StartCoroutine(PlayAnimAndDestroy());
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        anim = GetComponent<Animator>();
    }

    IEnumerator PlayAnimAndDestroy()
    {
        anim.Play(animName);
        yield return new WaitForSeconds(duration);
        Destroy(gameObject);
    }
}
