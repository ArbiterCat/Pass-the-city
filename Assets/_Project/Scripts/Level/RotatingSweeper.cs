using UnityEngine;

namespace SvoiPut
{
    /// <summary>Вращающаяся ферма с опасной перекладиной. Может быть быстрой — для хроно-модуля.</summary>
    public class RotatingSweeper : CourseElement
    {
        public Transform rotor;
        public float speed = 85f;
        public float startAngle = 0f;
        public Light hubLight;

        private float angle;
        private float baseLight;

        private void Awake()
        {
            if (rotor != null)
            {
                if (rotor.GetComponent<Rigidbody>() == null)
                {
                    var rb = rotor.gameObject.AddComponent<Rigidbody>();
                    rb.isKinematic = true;
                    rb.useGravity = false;
                }
            }
            if (hubLight != null) baseLight = hubLight.intensity;
        }

        private void Update()
        {
            angle += speed * Time.deltaTime;
            if (rotor != null) rotor.localRotation = Quaternion.Euler(0f, startAngle + angle, 0f);
            if (hubLight != null) hubLight.intensity = baseLight * (0.75f + 0.25f * Mathf.Sin(Time.time * 6f));
        }

        public override void ResetElement()
        {
            angle = 0f;
            if (rotor != null) rotor.localRotation = Quaternion.Euler(0f, startAngle, 0f);
        }
    }
}
