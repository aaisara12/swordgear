using UnityEngine;

namespace Testing
{
    public class StartNextRound : MonoBehaviour
    {
        [SerializeField] private Transform view;

        public void CallStartNextRound()
        {
            RoundStarter.Instance.StartRound();
            view.gameObject.SetActive(false);
        }
    }
}

