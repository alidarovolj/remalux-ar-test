using UnityEngine;

namespace Remalux.WallPainting.Setup
{
      // Запускается автоматически при загрузке сцены
      public class WallPaintingAutoSetup : MonoBehaviour
      {
            private void Awake()
            {
                  SetupScene();
            }

            public void SetupScene()
            {
                  try
                  {
                        // Создаем настройщик сцены
                        GameObject setupObj = new GameObject("WallPaintingSetup");
                        var sceneSetup = setupObj.AddComponent<WallPaintingSceneSetup>();

                        // Вызываем автоматическую настройку
                        sceneSetup.StartSetup();

                        Debug.Log("Автоматическая настройка сцены WallPainting завершена");
                  }
                  catch (System.Exception e)
                  {
                        Debug.LogError($"Ошибка при автоматической настройке сцены WallPainting: {e.Message}");
                        Debug.LogException(e);
                  }
            }
      }
}