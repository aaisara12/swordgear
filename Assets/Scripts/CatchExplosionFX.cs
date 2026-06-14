using UnityEngine;

public class CatchExplosionFX : MonoBehaviour
{
    [SerializeField] private float lifetime = 0.7f;

    public void Play(Color color)
    {
        foreach (ParticleSystem ps in GetComponentsInChildren<ParticleSystem>(includeInactive: true))
        {
            var main = ps.main;
            main.startColor = color;
            ps.Clear();
            ps.Play();
        }

        Destroy(gameObject, lifetime);
    }
}
