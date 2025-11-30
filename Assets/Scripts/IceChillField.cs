using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class IceChillField : MonoBehaviour
{
    public float lingerDuration = 1f;
    [SerializeField] float effectTickInterval = 0.2f;
    HashSet<EnemyController> enemiesInRange = new();

    public void BeginEffect()
    {
        StartCoroutine(ChillEffect());
    }

    IEnumerator ChillEffect()
    {
        float timeElapsed = 0f;

        while (timeElapsed < lingerDuration)
        {
            yield return null;
            timeElapsed += Time.deltaTime;

            if (timeElapsed % effectTickInterval < Time.deltaTime)
            {
                foreach (EnemyController enemy in enemiesInRange)
                {
                    GameManager.Instance.AddEffect(enemy, GameManager.EnemyEffect.Chill, 1);
                }
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Enemy"))
        {
            EnemyController enemy = collision.GetComponent<EnemyController>();
            enemiesInRange.Add(enemy);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Enemy"))
        {
            EnemyController enemy = collision.GetComponent<EnemyController>();
            enemiesInRange.Remove(enemy);
        }
    }
}
