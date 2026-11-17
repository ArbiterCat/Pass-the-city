using UnityEngine;

namespace SvoiPut
{
    /// <summary>Базовый класс элемента трассы, который возвращается в исходное состояние при старте/рестарте.</summary>
    public abstract class CourseElement : MonoBehaviour
    {
        public virtual void ResetElement() { }
    }
}
