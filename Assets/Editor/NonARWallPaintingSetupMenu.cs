using UnityEngine;
using UnityEditor;

namespace Remalux.AR.Editor
{
    /// <summary>
    /// Класс для добавления пункта меню для настройки сцены без AR
    /// </summary>
    public static class NonARWallPaintingSetupMenu
    {
        [MenuItem("Window/Remalux/Настроить систему покраски стен (без AR)")]
        public static void SetupNonARWallPaintingSystem()
        {
            Vision.NonARWallPaintingSetup.CreateNonARWallPaintingScene();
        }
    }
} 