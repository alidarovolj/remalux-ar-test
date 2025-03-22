using UnityEngine;
using UnityEditor;
using Remalux.AR.Vision;

public static class RemaluxMenu
{
    [MenuItem("Remalux/Создать сцену покраски реальных стен")]
    public static void CreateRealWallPaintingScene()
    {
        // Вызываем метод создания сцены из класса RealWallPaintingSetup
        RealWallPaintingSetup.CreateRealWallPaintingScene();
    }
} 