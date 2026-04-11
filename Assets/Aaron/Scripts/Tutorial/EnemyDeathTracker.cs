#nullable enable

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Tutorial
{
    public class EnemyDeathTracker : MonoBehaviour
    {
        public UnityEvent onAllEnemiesDefeated = new ();
        public List<EnemyController> enemies = new ();

        private HashSet<EnemyController> remainingEnemies = new ();
        
        private void Awake()
        {
            if (enemies.Count == 0)
            {
                onAllEnemiesDefeated.Invoke();
                return;
            }
            
            foreach (var enemy in enemies)
            {
                remainingEnemies.Add(enemy);
                enemy.OnDeath += () => HandleEnemyDeath(enemy);
            }
        }

        private void HandleEnemyDeath(EnemyController enemy)
        {
            remainingEnemies.Remove(enemy);
            
            if (remainingEnemies.Count == 0)
            {
                onAllEnemiesDefeated.Invoke();
            }
            
            // don't need to unsubscribe because enemy will just get destroyed
        }
    }
}