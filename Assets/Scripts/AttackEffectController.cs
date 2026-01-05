using UnityEngine;

// This interface will be called for all attack animations. This is to make adding different types of effects easier
public interface IAttackAnimator
{
    public void PlayAnimation();
}
public class AttackEffectController : MonoBehaviour, IAttackAnimator
{
    [SerializeField] ParticleSystem effect;
    public void PlayAnimation()
    {
        effect.Play();
    }
}
