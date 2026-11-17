using UnityEngine;

namespace SvoiPut
{
    /// <summary>Триггер смертельной опасности с понятной причиной ошибки.</summary>
    public class Hazard : MonoBehaviour
    {
        public string reason = "УДАР ЛОВУШКИ";

        private void OnTriggerEnter(Collider other)
        {
            Trigger(other);
        }

        private void OnTriggerStay(Collider other)
        {
            Trigger(other);
        }

        private void Trigger(Collider other)
        {
            if (GameManager.I == null) return;
            if (other.GetComponentInParent<PlayerController>() == null) return;
            GameManager.I.PlayerFailed(reason);
        }
    }
}
