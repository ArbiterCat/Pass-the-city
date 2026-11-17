using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SvoiPut
{
    /// <summary>Подсветка кнопки при наведении и нажатии.</summary>
    public class ButtonFx : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        public Image background;
        public Image accent;
        public Text label;
        public Color normalBackground;
        public Color hoverBackground;
        public Color pressedBackground;
        public Color normalLabel;
        public Color hoverLabel;
        public Color normalAccent;
        public Color hoverAccent;

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (background != null) background.color = hoverBackground;
            if (label != null) label.color = hoverLabel;
            if (accent != null) accent.color = hoverAccent;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (background != null) background.color = normalBackground;
            if (label != null) label.color = normalLabel;
            if (accent != null) accent.color = normalAccent;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (background != null) background.color = pressedBackground;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (background != null) background.color = hoverBackground;
        }
    }
}
