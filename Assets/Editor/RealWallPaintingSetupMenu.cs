using UnityEngine;
using UnityEditor;
using Remalux.WallPainting.Vision;

namespace Remalux.Editor
{
    /// <summary>
    /// Класс для добавления пункта меню для настройки системы покраски реальных стен
    /// </summary>
    public static class RealWallPaintingSetupMenu
    {
        [MenuItem("Window/Remalux/Setup Wall Painting System")]
        public static void SetupRealWallPaintingSystem()
        {
            RealWallPaintingSetup.CreateRealWallPaintingScene();
        }
    }
}