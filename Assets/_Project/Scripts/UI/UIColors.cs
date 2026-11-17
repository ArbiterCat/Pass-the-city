using UnityEngine;

namespace SvoiPut
{
    /// <summary>Палитра интерфейса: ржавый, хром и графит. Семантические имена для HUD и меню.</summary>
    public static class UIColors
    {
        public static readonly Color Rust = new Color32(0xB7, 0x47, 0x2A, 0xFF);
        public static readonly Color RustBright = new Color32(0xE2, 0x5C, 0x36, 0xFF);
        public static readonly Color Chrome = new Color32(0xBF, 0xC4, 0xC7, 0xFF);
        public static readonly Color ChromeBright = new Color32(0xE8, 0xEC, 0xEF, 0xFF);
        public static readonly Color Graphite = new Color32(0x20, 0x25, 0x2B, 0xFF);
        public static readonly Color GraphiteLight = new Color32(0x2E, 0x34, 0x3B, 0xFF);
        public static readonly Color GraphiteLighter = new Color32(0x3C, 0x43, 0x4B, 0xFF);

        public static readonly Color Text = ChromeBright;
        public static readonly Color Dim = new Color32(0x8F, 0x97, 0x9D, 0xFF);
        public static readonly Color Panel = new Color(0.125f, 0.145f, 0.169f, 0.95f);
        public static readonly Color PanelLight = new Color(0.18f, 0.20f, 0.23f, 0.96f);
        public static readonly Color PanelHover = new Color(0.26f, 0.29f, 0.32f, 1f);
        public static readonly Color Line = new Color(0.37f, 0.40f, 0.43f, 0.65f);

        public static readonly Color Danger = RustBright;
        public static readonly Color Checkpoint = ChromeBright;
        public static readonly Color Finish = ChromeBright;
        public static readonly Color Chrono = Chrome;
        public static readonly Color Electric = new Color32(0x9A, 0xD9, 0xF5, 0xFF);

        // Совместимость со старыми обращениями: холодный свет — хром, опасность — ржавчина.
        public static readonly Color Cyan = Chrome;
        public static readonly Color Amber = Rust;
        public static readonly Color Red = RustBright;
        public static readonly Color Green = ChromeBright;
    }
}
