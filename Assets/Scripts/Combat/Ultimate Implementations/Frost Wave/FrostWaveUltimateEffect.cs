using UnityEngine;

[CreateAssetMenu(fileName = "FrostWaveUltEffect", menuName = "Game/Ultimate Effect/Frost Wave")]
public class FrostWaveUltimateEffect : UltimateEffect
{
    [SerializeField] float effectRadius = 5f;
    [SerializeField] int effectDuration = 5;

    public override void ExecuteUlt(int overchargeLevel, Transform player)
    {
        Debug.Log("Frost ult");

        foreach (EnemyController enemy in ActiveEnemyRegistry.All)
        {
            if (Vector2.Distance(enemy.transform.position, player.position) < effectRadius)
            {
                GameManager.Instance.AddEffect(enemy, GameManager.EnemyEffect.Chill, effectDuration);
            }
        }

        UltimateChargeTracker.Instance?.EndExecution();  // Call this when the ult is completely done, so your ult starts charging again
    }
}
