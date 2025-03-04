using UnityEngine;
using UnityEditor;

namespace Remalux.AR.Vision
{
    /// <summary>
    /// Класс для добавления пункта меню для настройки системы покраски реальных стен
    /// </summary>
    public static class RealWallPaintingSetupMenu
    {
        [MenuItem("Window/Remalux AR/Setup Real Wall Painting System")]
        public static void SetupRealWallPaintingSystem()
        {
            RealWallPaintingSetup.CreateRealWallPaintingScene();
        }
    }
} 