using UnityEngine;

namespace SvoiPut
{
    /// <summary>Конвейер: прокручивает разметку и тянет игрока в заданную сторону.</summary>
    public class ConveyorBelt : CourseElement
    {
        [Header("Ссылки")]
        public Transform[] chevrons;
        public Light beltLight;

        [Header("Настройки")]
        public float beltLength = 5f;
        public float beltSpeed = 2.4f;
        public Vector3 pushDirection = Vector3.back;
        public float pushStrength = 2.2f;

        private PlayerController rider;
        private float scroll;
        private Vector3[] chevronBase;
        private float lightBase;

        private void Awake()
        {
            chevronBase = new Vector3[chevrons != null ? chevrons.Length : 0];
            for (int i = 0; i < chevronBase.Length; i++)
            {
                if (chevrons[i] != null) chevronBase[i] = chevrons[i].localPosition;
            }
            if (beltLight != null) lightBase = beltLight.intensity;
        }

        private void Update()
        {
            scroll += beltSpeed * Time.deltaTime;
            if (chevrons != null)
            {
                for (int i = 0; i < chevrons.Length; i++)
                {
                    if (chevrons[i] == null) continue;
                    float z = Mathf.Repeat(chevronBase[i].z - scroll + beltLength * 0.5f, beltLength) - beltLength * 0.5f;
                    chevrons[i].localPosition = new Vector3(chevronBase[i].x, chevronBase[i].y, z);
                }
            }

            if (rider != null)
            {
                // После респауна OnTriggerExit не приходит — отпускаем игрока по дистанции.
                if (Vector3.Distance(rider.transform.position, transform.position) > beltLength)
                {
                    rider.SetConveyorPush(Vector3.zero);
                    rider = null;
                }
                else if (GameManager.I != null && GameManager.I.State == GameState.Playing)
                {
                    rider.SetConveyorPush(pushDirection.normalized * pushStrength);
                }
            }

            if (beltLight != null)
            {
                beltLight.intensity = lightBase * (0.8f + 0.2f * Mathf.Sin(Time.time * 7f));
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            var player = other.GetComponentInParent<PlayerController>();
            if (player != null) rider = player;
        }

        private void OnTriggerExit(Collider other)
        {
            var player = other.GetComponentInParent<PlayerController>();
            if (player != null && rider == player) rider = null;
        }

        public override void ResetElement()
        {
            scroll = 0f;
            rider = null;
            if (chevrons != null)
            {
                for (int i = 0; i < chevrons.Length; i++)
                {
                    if (chevrons[i] != null) chevrons[i].localPosition = chevronBase[i];
                }
            }
        }
    }
}
