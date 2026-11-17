using UnityEngine;

namespace SvoiPut
{
    /// <summary>Объявление сектора при пересечении границы.</summary>
    public class SectionTrigger : CourseElement
    {
        public string title = "СЕКТОР 01";
        public string message = "СЕРВИСНЫЙ ТУННЕЛЬ";

        private bool fired;

        private void OnTriggerEnter(Collider other)
        {
            if (fired) return;
            if (other.GetComponentInParent<PlayerController>() == null) return;
            if (GameManager.I == null || GameManager.I.State != GameState.Playing) return;
            fired = true;
            GameManager.I.AnnounceSection(title, message);
        }

        public override void ResetElement()
        {
            fired = false;
        }
    }
}
