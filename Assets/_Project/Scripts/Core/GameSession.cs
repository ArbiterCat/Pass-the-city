using UnityEngine;

namespace SvoiPut
{
    /// <summary>Данные, которые переживают перезагрузку сцены (меню -> игра -> рестарт).</summary>
    public static class GameSession
    {
        public static bool AutoStart;
        public static bool Tutorial;
    }
}
