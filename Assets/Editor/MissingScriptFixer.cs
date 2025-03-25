#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using System.Reflection;
using System.Linq;
using UnityEditor.Callbacks;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using Remalux.WallPainting;

namespace Remalux.AR
{
      public static class MissingScriptFixer
      {
            [MenuItem("Tools/Wall Painting/Fix/Find Missing Scripts")]
            public static void FindMissingScripts()
            {
                  Debug.Log("=== Поиск отсутствующих скриптов ===");
                  GameObject[] allObjects = Object.FindObjectsOfType<GameObject>();
                  int missingCount = 0;

                  foreach (GameObject go in allObjects)
                  {
                        Component[] components = go.GetComponents<Component>();
                        for (int i = 0; i < components.Length; i++)
                        {
                              if (components[i] == null)
                              {
                                    missingCount++;
                                    Debug.LogWarning($"Объект '{go.name}' имеет отсутствующий скрипт в позиции {i}", go);
                              }
                        }
                  }

                  Debug.Log($"=== Найдено {missingCount} отсутствующих скриптов ===");
            }

            [MenuItem("Tools/Wall Painting/Fix/Remove Missing Scripts")]
            public static void RemoveMissingScripts()
            {
                  Debug.Log("=== Удаление отсутствующих скриптов ===");
                  GameObject[] allObjects = Object.FindObjectsOfType<GameObject>();
                  int removedCount = 0;

                  try
                  {
                        foreach (GameObject go in allObjects)
                        {
                              try
                              {
                                    int count = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
                                    if (count > 0)
                                    {
                                          removedCount += count;
                                          Debug.Log($"Удалено {count} отсутствующих скриптов с объекта '{go.name}'");
                                    }
                              }
                              catch (System.Exception e)
                              {
                                    Debug.LogError($"Ошибка при удалении скриптов с объекта '{go.name}': {e.Message}");
                              }
                        }

                        Debug.Log($"=== Удалено {removedCount} отсутствующих скриптов ===");

                        // Сохраняем изменения в сцене
                        if (removedCount > 0)
                        {
                              EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
                              Debug.Log("Сцена помечена как измененная. Не забудьте сохранить изменения.");
                        }
                  }
                  catch (System.Exception e)
                  {
                        Debug.LogError($"Критическая ошибка при удалении скриптов: {e.Message}");
                  }
            }

            [MenuItem("Tools/Wall Painting/Fix/Fix Wall Painting System")]
            public static void FixWallPaintingSystem()
            {
                  Debug.Log("=== Исправление системы покраски стен ===");

                  try
                  {
                        // Сначала удаляем отсутствующие скрипты
                        RemoveMissingScripts();

                        // Проверяем и исправляем слои стен
                        CheckWallLayers();

                        // Убедимся, что у стен есть тег "Wall"
                        EnsureWallTags();

                        // Проверяем и добавляем коллайдеры к стенам
                        EnsureWallColliders();

                        // Проверяем и исправляем WallMaterialInstanceTracker на всех стенах
                        FixWallMaterialTrackers();

                        // Находим все компоненты MonoBehaviour в сцене
                        MonoBehaviour[] allComponents = Object.FindObjectsOfType<MonoBehaviour>();
                        List<MonoBehaviour> wallPainters = new List<MonoBehaviour>();

                        // Проверяем и исправляем WallPainter
                        foreach (MonoBehaviour component in allComponents)
                        {
                              try
                              {
                                    if (component != null && component.GetType().Name == "WallPainter")
                                    {
                                          Debug.Log($"Найден WallPainter на объекте {component.gameObject.name}");
                                          wallPainters.Add(component);

                                          // Получаем тип компонента
                                          System.Type wallPainterType = component.GetType();

                                          // Ищем поля с учетом различных вариантов именования
                                          System.Reflection.FieldInfo mainCameraField = FindField(wallPainterType, "mainCamera");
                                          System.Reflection.FieldInfo wallLayerMaskField = FindField(wallPainterType, "wallLayerMask");
                                          System.Reflection.FieldInfo availablePaintsField = FindField(wallPainterType, "availablePaints");
                                          System.Reflection.FieldInfo currentPaintMaterialField = FindField(wallPainterType, "currentPaintMaterial");

                                          // Проверяем и исправляем ссылку на камеру
                                          if (mainCameraField != null)
                                          {
                                                Camera mainCamera = mainCameraField.GetValue(component) as Camera;
                                                if (mainCamera == null)
                                                {
                                                      mainCamera = Camera.main;
                                                      if (mainCamera != null)
                                                      {
                                                            mainCameraField.SetValue(component, mainCamera);
                                                            Debug.Log($"Установлена камера {mainCamera.name} для WallPainter на объекте {component.gameObject.name}");
                                                      }
                                                      else
                                                      {
                                                            Debug.LogWarning($"Не удалось найти главную камеру для WallPainter на объекте {component.gameObject.name}");
                                                      }
                                                }
                                          }
                                          else
                                          {
                                                Debug.LogWarning($"Поле камеры не найдено в WallPainter на объекте {component.gameObject.name}");
                                          }

                                          // Проверяем и исправляем маску слоя стен
                                          if (wallLayerMaskField != null)
                                          {
                                                object wallLayerMaskObj = wallLayerMaskField.GetValue(component);
                                                int wallLayerMask = 0;

                                                if (wallLayerMaskObj is int)
                                                {
                                                      wallLayerMask = (int)wallLayerMaskObj;
                                                }
                                                else if (wallLayerMaskObj is LayerMask)
                                                {
                                                      wallLayerMask = ((LayerMask)wallLayerMaskObj).value;
                                                }

                                                if (wallLayerMask == 0)
                                                {
                                                      int wallLayerIndex = LayerMask.NameToLayer("Wall");
                                                      if (wallLayerIndex != -1)
                                                      {
                                                            int newMask = 1 << wallLayerIndex;
                                                            wallLayerMaskField.SetValue(component, newMask);
                                                            Debug.Log($"Установлена маска слоя Wall для WallPainter на объекте {component.gameObject.name}");
                                                      }
                                                      else
                                                      {
                                                            Debug.LogWarning("Слой 'Wall' не найден в проекте. Создайте слой с именем 'Wall' в настройках проекта.");
                                                      }
                                                }
                                          }
                                          else
                                          {
                                                Debug.LogWarning($"Поле маски слоя не найдено в WallPainter на объекте {component.gameObject.name}");
                                          }

                                          // Проверяем и исправляем материалы для покраски
                                          if (availablePaintsField != null)
                                          {
                                                try
                                                {
                                                      Material[] availablePaints = availablePaintsField.GetValue(component) as Material[];
                                                      if (availablePaints == null || availablePaints.Length == 0)
                                                      {
                                                            Debug.LogWarning($"WallPainter на объекте {component.gameObject.name} не имеет материалов для покраски");

                                                            // Попытка найти материалы в ресурсах
                                                            Material[] paintMaterials = Resources.FindObjectsOfTypeAll<Material>();
                                                            List<Material> suitableMaterials = new List<Material>();

                                                            foreach (Material mat in paintMaterials)
                                                            {
                                                                  if (mat.name.Contains("Paint") || mat.name.Contains("Wall") ||
                                                                      mat.name.Contains("Color") || mat.name.Contains("Краска"))
                                                                  {
                                                                        suitableMaterials.Add(mat);
                                                                  }
                                                            }

                                                            if (suitableMaterials.Count > 0)
                                                            {
                                                                  availablePaintsField.SetValue(component, suitableMaterials.ToArray());
                                                                  Debug.Log($"Установлено {suitableMaterials.Count} материалов для WallPainter на объекте {component.gameObject.name}");

                                                                  // Также устанавливаем текущий материал, если он не установлен
                                                                  if (currentPaintMaterialField != null)
                                                                  {
                                                                        Material currentPaintMaterial = currentPaintMaterialField.GetValue(component) as Material;
                                                                        if (currentPaintMaterial == null)
                                                                        {
                                                                              currentPaintMaterialField.SetValue(component, suitableMaterials[0]);
                                                                              Debug.Log($"Установлен текущий материал {suitableMaterials[0].name} для WallPainter на объекте {component.gameObject.name}");
                                                                        }
                                                                  }
                                                            }
                                                      }
                                                      else if (currentPaintMaterialField != null)
                                                      {
                                                            // Проверяем, установлен ли текущий материал
                                                            Material currentPaintMaterial = currentPaintMaterialField.GetValue(component) as Material;
                                                            if (currentPaintMaterial == null && availablePaints.Length > 0)
                                                            {
                                                                  currentPaintMaterialField.SetValue(component, availablePaints[0]);
                                                                  Debug.Log($"Установлен текущий материал {availablePaints[0].name} для WallPainter на объекте {component.gameObject.name}");
                                                            }
                                                      }
                                                }
                                                catch (System.Exception e)
                                                {
                                                      Debug.LogError($"Ошибка при настройке материалов для WallPainter: {e.Message}");
                                                }
                                          }
                                          else
                                          {
                                                Debug.LogWarning($"Поле доступных материалов не найдено в WallPainter на объекте {component.gameObject.name}");
                                          }
                                    }
                              }
                              catch (System.Exception e)
                              {
                                    Debug.LogError($"Ошибка при обработке компонента {component.GetType().Name}: {e.Message}");
                              }
                        }

                        // Проверяем и исправляем EnhancedWallPainterInput
                        foreach (MonoBehaviour component in allComponents)
                        {
                              try
                              {
                                    if (component != null && component.GetType().Name == "EnhancedWallPainterInput")
                                    {
                                          Debug.Log($"Найден EnhancedWallPainterInput на объекте {component.gameObject.name}");

                                          // Проверяем ссылку на WallPainter
                                          System.Reflection.FieldInfo wallPainterField = FindField(component.GetType(), "wallPainter");
                                          if (wallPainterField == null)
                                          {
                                                Debug.LogWarning($"Поле wallPainter не найдено в EnhancedWallPainterInput на объекте {component.gameObject.name}");
                                          }
                                          else
                                          {
                                                MonoBehaviour wp = wallPainterField.GetValue(component) as MonoBehaviour;
                                                if (wp == null && wallPainters.Count > 0)
                                                {
                                                      wallPainterField.SetValue(component, wallPainters[0]);
                                                      Debug.Log($"Установлена ссылка на WallPainter для EnhancedWallPainterInput на объекте {component.gameObject.name}");
                                                }
                                                else if (wp != null)
                                                {
                                                      Debug.Log($"EnhancedWallPainterInput уже имеет ссылку на WallPainter: {wp.gameObject.name}");
                                                }
                                          }

                                          // Проверяем метод PaintWall
                                          System.Reflection.MethodInfo paintWallMethod = null;
                                          string[] methodNames = new string[] {
                                                "PaintWall", "paintWall", "PaintWallAtPosition", "paintWallAtPosition",
                                                "Paint", "paint", "ApplyPaint", "applyPaint"
                                          };

                                          foreach (string methodName in methodNames)
                                          {
                                                System.Reflection.MethodInfo method = component.GetType().GetMethod(methodName,
                                                      System.Reflection.BindingFlags.Public |
                                                      System.Reflection.BindingFlags.NonPublic |
                                                      System.Reflection.BindingFlags.Instance);

                                                if (method != null)
                                                {
                                                      paintWallMethod = method;
                                                      Debug.Log($"Найден метод покраски в EnhancedWallPainterInput: {methodName}");
                                                      break;
                                                }
                                          }

                                          if (paintWallMethod == null)
                                          {
                                                Debug.LogWarning($"Метод покраски не найден в EnhancedWallPainterInput на объекте {component.gameObject.name}");
                                          }
                                    }
                              }
                              catch (System.Exception e)
                              {
                                    Debug.LogError($"Ошибка при обработке EnhancedWallPainterInput: {e.Message}");
                              }
                        }

                        // Сохраняем изменения в сцене
                        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
                        Debug.Log("Сцена помечена как измененная. Не забудьте сохранить изменения.");
                  }
                  catch (System.Exception e)
                  {
                        Debug.LogError($"Критическая ошибка при исправлении системы покраски стен: {e.Message}");
                  }

                  Debug.Log("=== Исправление системы покраски стен завершено ===");

                  // Спрашиваем пользователя, хочет ли он протестировать систему покраски
                  if (EditorUtility.DisplayDialog("Тестирование системы покраски",
                      "Система покраски стен была исправлена. Хотите протестировать её сейчас?",
                      "Да, протестировать", "Нет, позже"))
                  {
                        TestWallPainting();
                  }
            }

            [MenuItem("Tools/Wall Painting/Fix/Fix Wall Material Trackers")]
            public static void FixWallMaterialTrackersMenuItem()
            {
                  FixWallMaterialTrackers();
            }

            private static void FixWallMaterialTrackers()
            {
                  Debug.Log("=== Проверка компонентов WallMaterialInstanceTracker ===");

                  try
                  {
                        // Проверяем, существует ли слой Wall
                        int wallLayerIndex = LayerMask.NameToLayer("Wall");
                        if (wallLayerIndex == -1)
                        {
                              Debug.LogWarning("Слой 'Wall' не найден в проекте. Используем все объекты с Renderer.");
                              wallLayerIndex = -1;
                        }
                        else
                        {
                              Debug.Log($"Слой 'Wall' найден с индексом {wallLayerIndex}");
                        }

                        // Находим все объекты с Renderer
                        GameObject[] allObjects = Object.FindObjectsOfType<GameObject>();
                        int fixedCount = 0;

                        foreach (GameObject obj in allObjects)
                        {
                              try
                              {
                                    // Проверяем, находится ли объект на слое Wall или имеет ключевые слова в имени
                                    bool isWall = (wallLayerIndex != -1 && obj.layer == wallLayerIndex);

                                    if (!isWall)
                                    {
                                          string objName = obj.name.ToLower();
                                          isWall = objName.Contains("wall") || objName.Contains("стена") ||
                                                   objName.Contains("plane") || objName.Contains("surface") ||
                                                   objName.Contains("плоскость");
                                    }

                                    if (isWall)
                                    {
                                          Renderer renderer = obj.GetComponent<Renderer>();
                                          if (renderer != null)
                                          {
                                                try
                                                {
                                                      // Проверяем наличие WallMaterialInstanceTracker
                                                      WallMaterialInstanceTracker tracker = obj.GetComponent<WallMaterialInstanceTracker>();
                                                      if (tracker == null)
                                                      {
                                                            tracker = obj.AddComponent<WallMaterialInstanceTracker>();
                                                            tracker.OriginalSharedMaterial = renderer.sharedMaterial;
                                                            Debug.Log($"Добавлен WallMaterialInstanceTracker к объекту {obj.name} с оригинальным материалом {tracker.OriginalSharedMaterial.name}");
                                                            fixedCount++;
                                                      }
                                                      else if (tracker.OriginalSharedMaterial == null && renderer.sharedMaterial != null)
                                                      {
                                                            tracker.OriginalSharedMaterial = renderer.sharedMaterial;
                                                            Debug.Log($"Обновлен оригинальный материал для WallMaterialInstanceTracker на объекте {obj.name}");
                                                            fixedCount++;
                                                      }

                                                      // Check if we need to create a unique material instance
                                                      if (!renderer.sharedMaterial.name.Contains("_Instance_"))
                                                      {
                                                            Material instanceMaterial = new Material(renderer.sharedMaterial);
                                                            instanceMaterial.name = $"{renderer.sharedMaterial.name}_Instance_{obj.name}";
                                                            renderer.sharedMaterial = instanceMaterial;
                                                            tracker.SetInstancedMaterial(instanceMaterial, true);
                                                            Debug.Log($"Created unique material instance for {obj.name}");
                                                      }

                                                      // Mark objects as dirty
                                                      EditorUtility.SetDirty(obj);
                                                      EditorUtility.SetDirty(renderer);
                                                      EditorUtility.SetDirty(tracker);
                                                }
                                                catch (System.Exception e)
                                                {
                                                      Debug.LogError($"Ошибка при обработке WallMaterialInstanceTracker для объекта {obj.name}: {e.Message}");
                                                }
                                          }
                                    }
                              }
                              catch (System.Exception e)
                              {
                                    Debug.LogError($"Ошибка при обработке объекта {obj.name}: {e.Message}");
                              }
                        }

                        Debug.Log($"=== Исправлено {fixedCount} компонентов WallMaterialInstanceTracker ===");
                  }
                  catch (System.Exception e)
                  {
                        Debug.LogError($"Критическая ошибка при исправлении WallMaterialInstanceTracker: {e.Message}");
                  }
            }

            [MenuItem("Tools/Wall Painting/Debug/Test Wall Painting")]
            public static void TestWallPainting()
            {
                  Debug.Log("=== Тестирование функции покраски стен ===");

                  try
                  {
                        // Находим WallPainter в сцене
                        MonoBehaviour wallPainter = FindWallPainterInScene();

                        if (wallPainter == null)
                        {
                              Debug.LogError("WallPainter не найден в сцене. Тестирование невозможно.");
                              return;
                        }

                        Debug.Log($"Найден WallPainter на объекте {wallPainter.gameObject.name}");

                        // Получаем тип компонента
                        System.Type wallPainterType = wallPainter.GetType();

                        // Проверяем настройки WallPainter
                        System.Reflection.FieldInfo mainCameraField = FindField(wallPainterType, "mainCamera");
                        if (mainCameraField == null)
                        {
                              Debug.LogWarning("Поле камеры не найдено в WallPainter. Тестирование может работать некорректно.");
                        }
                        else
                        {
                              Camera mainCamera = mainCameraField.GetValue(wallPainter) as Camera;
                              if (mainCamera == null)
                              {
                                    Debug.LogWarning("WallPainter не имеет ссылки на камеру. Пытаемся найти и установить камеру...");
                                    mainCamera = Camera.main;
                                    if (mainCamera != null)
                                    {
                                          mainCameraField.SetValue(wallPainter, mainCamera);
                                          Debug.Log($"Установлена камера {mainCamera.name} для WallPainter");
                                    }
                                    else
                                    {
                                          Debug.LogError("Не удалось найти камеру в сцене. Тестирование может работать некорректно.");
                                    }
                              }
                        }

                        // Проверяем маску слоя
                        System.Reflection.FieldInfo wallLayerMaskField = FindField(wallPainterType, "wallLayerMask");
                        if (wallLayerMaskField == null)
                        {
                              Debug.LogWarning("Поле маски слоя не найдено в WallPainter. Тестирование может работать некорректно.");
                        }
                        else
                        {
                              object wallLayerMaskObj = wallLayerMaskField.GetValue(wallPainter);
                              int wallLayerMask = 0;

                              if (wallLayerMaskObj is int)
                              {
                                    wallLayerMask = (int)wallLayerMaskObj;
                              }
                              else if (wallLayerMaskObj is LayerMask)
                              {
                                    wallLayerMask = ((LayerMask)wallLayerMaskObj).value;
                              }

                              if (wallLayerMask == 0)
                              {
                                    int wallLayerIndex = LayerMask.NameToLayer("Wall");
                                    if (wallLayerIndex != -1)
                                    {
                                          int newMask = 1 << wallLayerIndex;
                                          wallLayerMaskField.SetValue(wallPainter, newMask);
                                          Debug.Log($"Установлена маска слоя Wall для WallPainter");
                                          wallLayerMask = newMask;
                                    }
                                    else
                                    {
                                          Debug.LogWarning("Слой 'Wall' не найден в проекте. Тестирование может работать некорректно.");
                                    }
                              }

                              // Проверяем наличие объектов на слое Wall
                              bool wallsFound = false;
                              GameObject[] allObjects = Object.FindObjectsOfType<GameObject>();
                              foreach (GameObject obj in allObjects)
                              {
                                    if ((((1 << obj.layer) & wallLayerMask) != 0) && obj.CompareTag("Wall"))
                                    {
                                          wallsFound = true;
                                          Debug.Log($"Найден объект на слое Wall: {obj.name}");
                                    }
                              }

                              if (!wallsFound)
                              {
                                    Debug.LogError("Не найдено ни одного объекта на слое Wall. Тестирование может работать некорректно.");
                              }
                        }

                        // Проверяем текущий материал для покраски
                        System.Reflection.FieldInfo currentPaintMaterialField = FindField(wallPainterType, "currentPaintMaterial");
                        if (currentPaintMaterialField == null)
                        {
                              Debug.LogWarning("Поле текущего материала не найдено в WallPainter. Тестирование может работать некорректно.");
                        }
                        else
                        {
                              Material currentPaintMaterial = currentPaintMaterialField.GetValue(wallPainter) as Material;
                              if (currentPaintMaterial == null)
                              {
                                    Debug.LogWarning("WallPainter не имеет текущего материала для покраски. Пытаемся найти и установить материал...");

                                    // Пытаемся получить доступные материалы
                                    System.Reflection.FieldInfo availablePaintsField = FindField(wallPainterType, "availablePaints");
                                    if (availablePaintsField == null)
                                    {
                                          Debug.LogWarning("Поле доступных материалов не найдено в WallPainter. Тестирование может работать некорректно.");
                                    }
                                    else
                                    {
                                          Material[] availablePaints = availablePaintsField.GetValue(wallPainter) as Material[];
                                          if (availablePaints != null && availablePaints.Length > 0)
                                          {
                                                currentPaintMaterial = availablePaints[0];
                                                currentPaintMaterialField.SetValue(wallPainter, currentPaintMaterial);
                                                Debug.Log($"Установлен материал {currentPaintMaterial.name} для WallPainter");
                                          }
                                          else
                                          {
                                                Debug.LogError("Не удалось найти материалы для покраски. Тестирование может работать некорректно.");
                                          }
                                    }
                              }
                        }

                        // Ищем метод покраски стен
                        MethodInfo paintMethod = null;
                        string[] methodNames = new string[] {
                              "PaintWallAtPosition", "paintWallAtPosition", "PaintWall", "paintWall",
                              "Paint", "paint", "ApplyPaint", "applyPaint"
                        };

                        foreach (string methodName in methodNames)
                        {
                              MethodInfo method = wallPainterType.GetMethod(methodName,
                                    System.Reflection.BindingFlags.Public |
                                    System.Reflection.BindingFlags.NonPublic |
                                    System.Reflection.BindingFlags.Instance);

                              if (method != null)
                              {
                                    paintMethod = method;
                                    Debug.Log($"Найден метод покраски: {methodName}");
                                    break;
                              }
                        }

                        if (paintMethod == null)
                        {
                              Debug.LogError("Метод покраски стен не найден в компоненте WallPainter. Тестирование невозможно.");
                              return;
                        }

                        // Тестируем покраску в центре экрана
                        Vector2 screenCenter = new Vector2(Screen.width / 2, Screen.height / 2);
                        Debug.Log($"Тестирование покраски в центре экрана: {screenCenter}");

                        try
                        {
                              paintMethod.Invoke(wallPainter, new object[] { screenCenter });
                        }
                        catch (System.Exception e)
                        {
                              Debug.LogError($"Ошибка при вызове метода покраски: {e.Message}");
                              Debug.LogException(e);
                              return; // Прекращаем тестирование, если метод не работает
                        }

                        // Тестируем покраску в разных частях экрана
                        Vector2[] testPositions = new Vector2[]
                        {
                              new Vector2(Screen.width * 0.25f, Screen.height * 0.25f),
                              new Vector2(Screen.width * 0.75f, Screen.height * 0.25f),
                              new Vector2(Screen.width * 0.25f, Screen.height * 0.75f),
                              new Vector2(Screen.width * 0.75f, Screen.height * 0.75f)
                        };

                        foreach (Vector2 position in testPositions)
                        {
                              Debug.Log($"Тестирование покраски в позиции: {position}");
                              try
                              {
                                    paintMethod.Invoke(wallPainter, new object[] { position });
                              }
                              catch (System.Exception e)
                              {
                                    Debug.LogError($"Ошибка при вызове метода покраски для позиции {position}: {e.Message}");
                              }
                        }

                        Debug.Log("=== Тестирование функции покраски стен завершено ===");
                  }
                  catch (System.Exception e)
                  {
                        Debug.LogError($"Критическая ошибка при тестировании покраски стен: {e.Message}");
                        Debug.LogException(e);
                  }
            }

            [MenuItem("Tools/Wall Painting/Fix/Check Wall Layers")]
            public static void CheckWallLayers()
            {
                  Debug.Log("=== Проверка слоев стен ===");

                  try
                  {
                        // Проверяем, существует ли слой Wall
                        int wallLayerIndex = LayerMask.NameToLayer("Wall");
                        if (wallLayerIndex == -1)
                        {
                              Debug.LogError("Слой 'Wall' не найден в проекте. Пожалуйста, создайте слой с именем 'Wall' в настройках проекта (Edit > Project Settings > Tags and Layers).");
                              return;
                        }

                        Debug.Log($"Слой 'Wall' найден с индексом {wallLayerIndex}");

                        // Находим все объекты с Renderer и Collider, которые могут быть стенами
                        GameObject[] allObjects = Object.FindObjectsOfType<GameObject>();
                        int wallCount = 0;
                        int fixedCount = 0;

                        foreach (GameObject obj in allObjects)
                        {
                              try
                              {
                                    // Проверяем, есть ли у объекта Renderer и Collider
                                    Renderer renderer = obj.GetComponent<Renderer>();
                                    Collider collider = obj.GetComponent<Collider>();

                                    if (renderer != null && collider != null)
                                    {
                                          // Проверяем имя объекта на наличие ключевых слов, связанных со стенами
                                          string objName = obj.name.ToLower();
                                          bool isLikelyWall = objName.Contains("wall") || objName.Contains("стена") ||
                                                              objName.Contains("plane") || objName.Contains("surface") ||
                                                              objName.Contains("плоскость");

                                          // Если объект похож на стену и не находится на слое Wall
                                          if (isLikelyWall && obj.layer != wallLayerIndex)
                                          {
                                                obj.layer = wallLayerIndex;
                                                fixedCount++;
                                                Debug.Log($"Объект '{obj.name}' перемещен на слой 'Wall'");
                                          }

                                          // Считаем объекты на слое Wall
                                          if (obj.layer == wallLayerIndex)
                                          {
                                                wallCount++;
                                          }
                                    }
                              }
                              catch (System.Exception e)
                              {
                                    Debug.LogError($"Ошибка при обработке объекта {obj.name}: {e.Message}");
                              }
                        }

                        Debug.Log($"=== Всего объектов на слое Wall: {wallCount}, исправлено: {fixedCount} ===");

                        // Если не найдено ни одного объекта на слое Wall, выводим предупреждение
                        if (wallCount == 0)
                        {
                              Debug.LogWarning("Не найдено ни одного объекта на слое Wall. Система покраски стен может работать некорректно.");
                        }

                        // Сохраняем изменения в сцене
                        if (fixedCount > 0)
                        {
                              EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
                              Debug.Log("Сцена помечена как измененная. Не забудьте сохранить изменения.");
                        }
                  }
                  catch (System.Exception e)
                  {
                        Debug.LogError($"Критическая ошибка при проверке слоев стен: {e.Message}");
                  }
            }

            [MenuItem("Tools/Wall Painting/Debug/Diagnose Wall Painting System")]
            public static void DiagnoseWallPaintingSystem()
            {
                  Debug.Log("=== Диагностика системы покраски стен ===");

                  try
                  {
                        // Проверяем наличие WallPainter в сцене
                        MonoBehaviour wallPainter = FindWallPainterInScene();
                        if (wallPainter == null)
                        {
                              Debug.LogError("WallPainter не найден в сцене. Попытка создать новый...");
                              CreateWallPainterInScene();
                        }
                        else
                        {
                              Debug.Log($"WallPainter найден на объекте {wallPainter.gameObject.name}");

                              // Проверяем настройки WallPainter
                              DiagnoseWallPainterComponent(wallPainter);
                        }
                  }
                  catch (System.Exception e)
                  {
                        Debug.LogError($"Критическая ошибка при диагностике системы покраски стен: {e.Message}");
                        Debug.LogException(e);
                  }

                  Debug.Log("=== Диагностика системы покраски стен завершена ===");
            }

            private static MonoBehaviour FindWallPainterInScene()
            {
                  MonoBehaviour[] allComponents = Object.FindObjectsOfType<MonoBehaviour>();
                  foreach (MonoBehaviour component in allComponents)
                  {
                        if (component != null && component.GetType().Name == "WallPainter")
                        {
                              return component;
                        }
                  }
                  return null;
            }

            private static void CreateWallPainterInScene()
            {
                  try
                  {
                        // Создаем новый GameObject
                        GameObject wallPainterObj = new GameObject("WallPainter");

                        // Пытаемся найти тип WallPainter
                        System.Type wallPainterType = null;
                        foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
                        {
                              wallPainterType = assembly.GetType("WallPainter");
                              if (wallPainterType != null)
                                    break;

                              // Проверяем в пространстве имен Remalux.AR
                              wallPainterType = assembly.GetType("Remalux.AR.WallPainter");
                              if (wallPainterType != null)
                                    break;
                        }

                        if (wallPainterType == null)
                        {
                              Debug.LogError("Не удалось найти тип WallPainter. Убедитесь, что скрипт WallPainter существует в проекте.");
                              return;
                        }

                        // Добавляем компонент WallPainter
                        Component wallPainter = wallPainterObj.AddComponent(wallPainterType);
                        if (wallPainter == null)
                        {
                              Debug.LogError("Не удалось добавить компонент WallPainter.");
                              return;
                        }

                        Debug.Log($"Создан новый объект с компонентом WallPainter: {wallPainterObj.name}");

                        // Настраиваем основные параметры
                        System.Reflection.FieldInfo mainCameraField = wallPainterType.GetField("mainCamera");
                        if (mainCameraField != null)
                        {
                              Camera mainCamera = Camera.main;
                              if (mainCamera != null)
                              {
                                    mainCameraField.SetValue(wallPainter, mainCamera);
                                    Debug.Log($"Установлена камера {mainCamera.name} для WallPainter");
                              }
                        }

                        System.Reflection.FieldInfo wallLayerMaskField = wallPainterType.GetField("wallLayerMask");
                        if (wallLayerMaskField != null)
                        {
                              int wallLayerIndex = LayerMask.NameToLayer("Wall");
                              if (wallLayerIndex != -1)
                              {
                                    int newMask = 1 << wallLayerIndex;
                                    wallLayerMaskField.SetValue(wallPainter, newMask);
                                    Debug.Log($"Установлена маска слоя Wall для WallPainter");
                              }
                        }

                        // Помечаем сцену как измененную
                        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
                  }
                  catch (System.Exception e)
                  {
                        Debug.LogError($"Ошибка при создании WallPainter: {e.Message}");
                        Debug.LogException(e);
                  }
            }

            private static void DiagnoseWallPainterComponent(MonoBehaviour wallPainter)
            {
                  try
                  {
                        System.Type wallPainterType = wallPainter.GetType();

                        // Выводим список всех полей компонента
                        Debug.Log("=== Список всех полей компонента WallPainter ===");
                        System.Reflection.FieldInfo[] allFields = wallPainterType.GetFields(System.Reflection.BindingFlags.Public |
                                                                                          System.Reflection.BindingFlags.NonPublic |
                                                                                          System.Reflection.BindingFlags.Instance);

                        foreach (System.Reflection.FieldInfo field in allFields)
                        {
                              try
                              {
                                    object value = field.GetValue(wallPainter);
                                    string valueStr = "null";

                                    if (value != null)
                                    {
                                          if (value is UnityEngine.Object unityObj)
                                          {
                                                valueStr = unityObj != null ? unityObj.name : "null";
                                          }
                                          else if (value is Material[] materials)
                                          {
                                                valueStr = materials.Length > 0 ? $"{materials.Length} материалов" : "пустой массив";
                                          }
                                          else if (value is int intValue)
                                          {
                                                valueStr = intValue.ToString();
                                          }
                                          else
                                          {
                                                valueStr = value.ToString();
                                          }
                                    }

                                    Debug.Log($"Поле: {field.Name}, Тип: {field.FieldType.Name}, Значение: {valueStr}");
                              }
                              catch (System.Exception e)
                              {
                                    Debug.LogWarning($"Не удалось получить значение поля {field.Name}: {e.Message}");
                              }
                        }

                        // Выводим список всех методов компонента
                        Debug.Log("=== Список всех методов компонента WallPainter ===");
                        System.Reflection.MethodInfo[] allMethods = wallPainterType.GetMethods(System.Reflection.BindingFlags.Public |
                                                                                             System.Reflection.BindingFlags.NonPublic |
                                                                                             System.Reflection.BindingFlags.Instance);

                        foreach (System.Reflection.MethodInfo method in allMethods)
                        {
                              if (method.DeclaringType == wallPainterType) // Только методы, объявленные в самом классе, не унаследованные
                              {
                                    Debug.Log($"Метод: {method.Name}");
                              }
                        }

                        // После вывода всех полей, проверим наличие нужных полей с учетом регистра
                        Debug.Log("=== Проверка необходимых полей ===");

                        // Массив имен полей, которые мы ищем
                        string[] requiredFieldNames = new string[] {
                              "mainCamera", "wallLayerMask", "availablePaints", "currentPaintMaterial",
                              "MainCamera", "WallLayerMask", "AvailablePaints", "CurrentPaintMaterial",
                              "_mainCamera", "_wallLayerMask", "_availablePaints", "_currentPaintMaterial",
                              "m_mainCamera", "m_wallLayerMask", "m_availablePaints", "m_currentPaintMaterial"
                        };

                        // Словарь для хранения найденных полей
                        Dictionary<string, System.Reflection.FieldInfo> foundFields = new Dictionary<string, System.Reflection.FieldInfo>();

                        // Проверяем каждое требуемое поле
                        foreach (string fieldName in requiredFieldNames)
                        {
                              System.Reflection.FieldInfo field = wallPainterType.GetField(fieldName,
                                    System.Reflection.BindingFlags.Public |
                                    System.Reflection.BindingFlags.NonPublic |
                                    System.Reflection.BindingFlags.Instance);

                              if (field != null)
                              {
                                    string baseFieldName = fieldName.ToLower();
                                    if (baseFieldName.StartsWith("_") || baseFieldName.StartsWith("m_"))
                                          baseFieldName = baseFieldName.Substring(baseFieldName.IndexOf('_') + 1);

                                    if (char.IsUpper(baseFieldName[0]))
                                          baseFieldName = char.ToLower(baseFieldName[0]) + baseFieldName.Substring(1);

                                    foundFields[baseFieldName] = field;
                                    Debug.Log($"Найдено поле '{fieldName}' (базовое имя: '{baseFieldName}')");
                              }
                        }

                        // Проверяем, какие поля не найдены
                        string[] baseFieldNames = new string[] { "maincamera", "wallLayermask", "availablepaints", "currentpaintmaterial" };
                        foreach (string baseFieldName in baseFieldNames)
                        {
                              if (!foundFields.ContainsKey(baseFieldName))
                              {
                                    Debug.LogWarning($"Не найдено поле '{baseFieldName}' (проверены варианты: {string.Join(", ", requiredFieldNames.Where(n => n.ToLower().Contains(baseFieldName)))})");

                                    // Ищем похожие поля
                                    var similarFields = allFields.Where(f => f.Name.ToLower().Contains(baseFieldName) ||
                                                                         baseFieldName.Contains(f.Name.ToLower())).ToList();

                                    if (similarFields.Count > 0)
                                    {
                                          Debug.Log($"Найдены похожие поля для '{baseFieldName}':");
                                          foreach (var similarField in similarFields)
                                          {
                                                Debug.Log($" - {similarField.Name} (тип: {similarField.FieldType.Name})");
                                          }
                                    }
                              }
                        }

                        // Проверяем метод PaintWallAtPosition и похожие
                        string[] methodNames = new string[] {
                              "PaintWallAtPosition", "paintWallAtPosition", "PaintWall", "paintWall",
                              "Paint", "paint", "ApplyPaint", "applyPaint"
                        };

                        bool paintMethodFound = false;
                        foreach (string methodName in methodNames)
                        {
                              System.Reflection.MethodInfo method = wallPainterType.GetMethod(methodName,
                                    System.Reflection.BindingFlags.Public |
                                    System.Reflection.BindingFlags.NonPublic |
                                    System.Reflection.BindingFlags.Instance);

                              if (method != null)
                              {
                                    paintMethodFound = true;
                                    Debug.Log($"Найден метод '{methodName}' с параметрами: {string.Join(", ", method.GetParameters().Select(p => p.ParameterType.Name))}");
                              }
                        }

                        if (!paintMethodFound)
                        {
                              Debug.LogWarning($"Не найден метод покраски стен (проверены варианты: {string.Join(", ", methodNames)})");

                              // Ищем похожие методы
                              var paintMethods = allMethods.Where(m =>
                                    m.DeclaringType == wallPainterType &&
                                    (m.Name.ToLower().Contains("paint") ||
                                     m.Name.ToLower().Contains("color") ||
                                     m.Name.ToLower().Contains("wall"))).ToList();

                              if (paintMethods.Count > 0)
                              {
                                    Debug.Log("Найдены похожие методы:");
                                    foreach (var method in paintMethods)
                                    {
                                          Debug.Log($" - {method.Name} с параметрами: {string.Join(", ", method.GetParameters().Select(p => p.ParameterType.Name))}");
                                    }
                              }
                        }
                  }
                  catch (System.Exception e)
                  {
                        Debug.LogError($"Ошибка при диагностике компонента WallPainter: {e.Message}");
                        Debug.LogException(e);
                  }
            }

            private static void EnsureWallTags()
            {
                  Debug.Log("=== Проверка тегов стен ===");

                  try
                  {
                        // Проверяем, существует ли слой Wall
                        int wallLayerIndex = LayerMask.NameToLayer("Wall");
                        if (wallLayerIndex == -1)
                        {
                              Debug.LogError("Слой 'Wall' не найден в проекте. Невозможно проверить теги стен.");
                              return;
                        }

                        // Проверяем, существует ли тег Wall
                        bool wallTagExists = false;
                        foreach (string tag in UnityEditorInternal.InternalEditorUtility.tags)
                        {
                              if (tag == "Wall")
                              {
                                    wallTagExists = true;
                                    break;
                              }
                        }

                        if (!wallTagExists)
                        {
                              // Добавляем тег Wall
                              SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
                              SerializedProperty tagsProp = tagManager.FindProperty("tags");

                              bool found = false;
                              for (int i = 0; i < tagsProp.arraySize; i++)
                              {
                                    SerializedProperty t = tagsProp.GetArrayElementAtIndex(i);
                                    if (t.stringValue.Equals("Wall")) { found = true; break; }
                              }

                              if (!found)
                              {
                                    tagsProp.InsertArrayElementAtIndex(0);
                                    SerializedProperty sp = tagsProp.GetArrayElementAtIndex(0);
                                    sp.stringValue = "Wall";
                                    tagManager.ApplyModifiedProperties();
                                    Debug.Log("Добавлен тег 'Wall' в настройки проекта");
                              }
                        }

                        // Находим все объекты на слое Wall
                        GameObject[] allObjects = Object.FindObjectsOfType<GameObject>();
                        int fixedCount = 0;

                        foreach (GameObject obj in allObjects)
                        {
                              if (obj.layer == wallLayerIndex)
                              {
                                    // Если объект на слое Wall, но не имеет тега Wall
                                    if (obj.CompareTag("Wall"))
                                    {
                                          Debug.Log($"Объект '{obj.name}' уже имеет тег 'Wall'");
                                          fixedCount++;
                                    }
                                    else
                                    {
                                          obj.tag = "Wall";
                                          Debug.Log($"Объекту '{obj.name}' установлен тег 'Wall'");
                                          fixedCount++;
                                    }
                              }
                        }

                        Debug.Log($"=== Всего объектов с тегом Wall: {fixedCount} ===");

                        // Сохраняем изменения в сцене
                        if (fixedCount > 0)
                        {
                              EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
                              Debug.Log("Сцена помечена как измененная. Не забудьте сохранить изменения.");
                        }
                  }
                  catch (System.Exception e)
                  {
                        Debug.LogError($"Критическая ошибка при проверке тегов стен: {e.Message}");
                  }
            }

            private static System.Reflection.FieldInfo FindField(System.Type type, string fieldName)
            {
                  System.Reflection.FieldInfo field = null;
                  string[] fieldNames = new string[] {
                        fieldName,
                        fieldName.ToLower(),
                        char.ToUpper(fieldName[0]) + fieldName.Substring(1), // PascalCase
                        "_" + fieldName,
                        "m_" + fieldName,
                        "_" + fieldName.ToLower(),
                        "m_" + fieldName.ToLower()
                  };

                  foreach (string name in fieldNames)
                  {
                        field = type.GetField(name, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        if (field != null)
                        {
                              Debug.Log($"Найдено поле '{name}' для базового имени '{fieldName}'");
                              break;
                        }
                  }

                  return field;
            }

            /// <summary>
            /// Возвращает подходящий шейдер в зависимости от используемого рендер пайплайна
            /// </summary>
            /// <returns>Шейдер, подходящий для текущего рендер пайплайна</returns>
            private static Shader GetAppropriateShader()
            {
                  // Проверяем, какой рендер пайплайн используется
                  if (UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline != null)
                  {
                        // Для URP
                        Debug.Log("Используется URP, выбираем URP шейдер");
                        return Shader.Find("Universal Render Pipeline/Lit");
                  }
                  else
                  {
                        // Для стандартного рендер пайплайна
                        return Shader.Find("Standard");
                  }
            }

            [MenuItem("Tools/Wall Painting/Fix/Check Wall Materials")]
            public static void CheckWallMaterials()
            {
                  Debug.Log("=== Проверка материалов стен ===");

                  try
                  {
                        // Находим все объекты на слое Wall
                        int wallLayerIndex = LayerMask.NameToLayer("Wall");
                        if (wallLayerIndex == -1)
                        {
                              Debug.LogError("Слой 'Wall' не найден в проекте. Создайте слой с именем 'Wall' в настройках проекта.");
                              return;
                        }

                        int wallLayerMask = 1 << wallLayerIndex;
                        GameObject[] allObjects = Object.FindObjectsOfType<GameObject>();
                        List<GameObject> wallObjects = new List<GameObject>();

                        foreach (GameObject obj in allObjects)
                        {
                              if ((((1 << obj.layer) & wallLayerMask) != 0) && obj.CompareTag("Wall"))
                              {
                                    wallObjects.Add(obj);
                                    Debug.Log($"Найден объект на слое Wall: {obj.name}");
                              }
                        }

                        if (wallObjects.Count == 0)
                        {
                              Debug.LogWarning("Не найдено ни одного объекта на слое Wall.");
                              return;
                        }

                        // Проверяем материалы стен
                        foreach (GameObject wallObj in wallObjects)
                        {
                              Renderer renderer = wallObj.GetComponent<Renderer>();
                              if (renderer == null)
                              {
                                    Debug.LogWarning($"Объект {wallObj.name} на слое Wall не имеет компонента Renderer.");
                                    continue;
                              }

                              Material material = renderer.sharedMaterial;
                              if (material == null)
                              {
                                    Debug.LogWarning($"Объект {wallObj.name} на слое Wall не имеет материала.");

                                    // Создаем новый материал, если не найден
                                    Debug.LogWarning($"Материал для стены {wallObj.name} не найден. Создаем новый материал.");

                                    // Создаем материал с подходящим шейдером
                                    Material newMaterial = new Material(GetAppropriateShader());

                                    newMaterial.name = "WallMaterial_" + wallObj.name;
                                    renderer.sharedMaterial = newMaterial;
                                    Debug.Log($"Установлен материал {newMaterial.name} для объекта {wallObj.name}");
                              }
                              else
                              {
                                    Debug.Log($"Объект {wallObj.name} имеет материал: {material.name}");

                                    // Проверяем, правильно ли настроен материал
                                    if (material.color.a < 1.0f)
                                    {
                                          Debug.LogWarning($"Материал {material.name} имеет прозрачность {material.color.a}. Устанавливаем непрозрачность.");
                                          Color color = material.color;
                                          color.a = 1.0f;
                                          material.color = color;
                                    }

                                    // Проверяем режим рендеринга
                                    try
                                    {
                                          // Проверяем, есть ли свойство _Mode в материале
                                          if (material.HasProperty("_Mode"))
                                          {
                                                if (material.GetFloat("_Mode") != 0) // 0 = Opaque
                                                {
                                                      Debug.LogWarning($"Материал {material.name} не в режиме Opaque. Исправляем...");
                                                      material.SetFloat("_Mode", 0);
                                                      material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                                                      material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                                                      material.SetInt("_ZWrite", 1);
                                                      material.DisableKeyword("_ALPHATEST_ON");
                                                      material.DisableKeyword("_ALPHABLEND_ON");
                                                      material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                                                }
                                          }
                                          else if (material.shader.name.Contains("Universal Render Pipeline") ||
                                                  material.shader.name.Contains("URP"))
                                          {
                                                // Для URP шейдеров
                                                Debug.Log($"Материал {material.name} использует URP шейдер. Настраиваем для URP...");

                                                // Настройка для URP материалов
                                                if (material.HasProperty("_Surface"))
                                                {
                                                      // 0 = Opaque в URP
                                                      material.SetFloat("_Surface", 0);
                                                }

                                                if (material.HasProperty("_ZWrite"))
                                                {
                                                      material.SetInt("_ZWrite", 1);
                                                }

                                                // Отключаем прозрачность для URP
                                                material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
                                                material.renderQueue = -1; // Значение по умолчанию для непрозрачных материалов
                                          }
                                    }
                                    catch (System.Exception e)
                                    {
                                          Debug.LogError($"Ошибка при настройке режима рендеринга для материала {material.name}: {e.Message}");
                                    }
                              }

                              // Проверяем компонент WallMaterialInstanceTracker
                              MonoBehaviour tracker = wallObj.GetComponent("WallMaterialInstanceTracker") as MonoBehaviour;
                              if (tracker == null)
                              {
                                    Debug.LogWarning($"Объект {wallObj.name} не имеет компонента WallMaterialInstanceTracker. Добавляем...");
                                    tracker = wallObj.AddComponent(System.Type.GetType("Remalux.AR.WallMaterialInstanceTracker")) as MonoBehaviour;
                                    if (tracker != null)
                                    {
                                          Debug.Log($"Добавлен компонент WallMaterialInstanceTracker к объекту {wallObj.name}");
                                    }
                                    else
                                    {
                                          Debug.LogError($"Не удалось добавить компонент WallMaterialInstanceTracker к объекту {wallObj.name}");
                                    }
                              }
                        }

                        // Сохраняем изменения в сцене
                        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
                        Debug.Log("Сцена помечена как измененная. Не забудьте сохранить изменения.");
                  }
                  catch (System.Exception e)
                  {
                        Debug.LogError($"Ошибка при проверке материалов стен: {e.Message}");
                        Debug.LogException(e);
                  }

                  Debug.Log("=== Проверка материалов стен завершена ===");
            }

            [MenuItem("Tools/Wall Painting/Fix/Fix Wall Material Instances")]
            public static void FixWallMaterialInstances()
            {
                  Debug.Log("=== Исправление экземпляров материалов стен ===");

                  try
                  {
                        // Находим WallPainter в сцене
                        MonoBehaviour wallPainter = FindWallPainterInScene();

                        if (wallPainter == null)
                        {
                              Debug.LogError("WallPainter не найден в сцене. Исправление невозможно.");
                              return;
                        }

                        Debug.Log($"Найден WallPainter на объекте {wallPainter.gameObject.name}");

                        // Получаем доступные материалы
                        System.Reflection.FieldInfo availablePaintsField = FindField(wallPainter.GetType(), "availablePaints");
                        if (availablePaintsField == null)
                        {
                              Debug.LogError("Поле доступных материалов не найдено в WallPainter. Исправление невозможно.");
                              return;
                        }

                        Material[] availablePaints = availablePaintsField.GetValue(wallPainter) as Material[];
                        if (availablePaints == null || availablePaints.Length == 0)
                        {
                              Debug.LogError("WallPainter не имеет доступных материалов. Исправление невозможно.");
                              return;
                        }

                        Debug.Log($"Найдено {availablePaints.Length} доступных материалов для покраски");

                        // Находим все объекты на слое Wall
                        int wallLayerIndex = LayerMask.NameToLayer("Wall");
                        if (wallLayerIndex == -1)
                        {
                              Debug.LogError("Слой 'Wall' не найден в проекте. Создайте слой с именем 'Wall' в настройках проекта.");
                              return;
                        }

                        int wallLayerMask = 1 << wallLayerIndex;
                        GameObject[] allObjects = Object.FindObjectsOfType<GameObject>();
                        List<GameObject> wallObjects = new List<GameObject>();

                        foreach (GameObject obj in allObjects)
                        {
                              if ((((1 << obj.layer) & wallLayerMask) != 0) && obj.CompareTag("Wall"))
                              {
                                    wallObjects.Add(obj);
                                    Debug.Log($"Найден объект на слое Wall: {obj.name}");
                              }
                        }

                        if (wallObjects.Count == 0)
                        {
                              Debug.LogWarning("Не найдено ни одного объекта на слое Wall.");
                              return;
                        }

                        // Проверяем и исправляем материалы экземпляров
                        foreach (GameObject wallObj in wallObjects)
                        {
                              Renderer renderer = wallObj.GetComponent<Renderer>();
                              if (renderer == null)
                              {
                                    Debug.LogWarning($"Объект {wallObj.name} на слое Wall не имеет компонента Renderer.");
                                    continue;
                              }

                              // Проверяем, есть ли у объекта компонент WallMaterialInstanceTracker
                              MonoBehaviour tracker = wallObj.GetComponent("WallMaterialInstanceTracker") as MonoBehaviour;
                              if (tracker == null)
                              {
                                    Debug.LogWarning($"Объект {wallObj.name} не имеет компонента WallMaterialInstanceTracker. Добавляем...");

                                    try
                                    {
                                          // Пытаемся найти тип WallMaterialInstanceTracker
                                          System.Type trackerType = System.Type.GetType("Remalux.AR.WallMaterialInstanceTracker");
                                          if (trackerType == null)
                                          {
                                                // Пытаемся найти тип через рефлексию
                                                foreach (System.Reflection.Assembly assembly in System.AppDomain.CurrentDomain.GetAssemblies())
                                                {
                                                      trackerType = assembly.GetType("Remalux.AR.WallMaterialInstanceTracker");
                                                      if (trackerType != null)
                                                            break;
                                                }

                                                if (trackerType == null)
                                                {
                                                      Debug.LogError($"Не удалось найти тип WallMaterialInstanceTracker. Исправление невозможно для объекта {wallObj.name}");
                                                      continue;
                                                }
                                          }

                                          tracker = wallObj.AddComponent(trackerType) as MonoBehaviour;
                                          if (tracker != null)
                                          {
                                                Debug.Log($"Добавлен компонент WallMaterialInstanceTracker к объекту {wallObj.name}");
                                          }
                                          else
                                          {
                                                Debug.LogError($"Не удалось добавить компонент WallMaterialInstanceTracker к объекту {wallObj.name}");
                                                continue;
                                          }
                                    }
                                    catch (System.Exception e)
                                    {
                                          Debug.LogError($"Ошибка при добавлении компонента WallMaterialInstanceTracker к объекту {wallObj.name}: {e.Message}");
                                          continue;
                                    }
                              }

                              // Проверяем, есть ли у объекта экземпляр материала
                              Material currentMaterial = renderer.sharedMaterial;
                              if (currentMaterial == null)
                              {
                                    Debug.LogWarning($"Объект {wallObj.name} не имеет материала. Устанавливаем материал по умолчанию...");

                                    // Создаем экземпляр материала для стены
                                    Material instanceMaterial;
                                    if (availablePaints != null && availablePaints.Length > 0 && availablePaints[0] != null)
                                    {
                                          instanceMaterial = new Material(availablePaints[0]);
                                          instanceMaterial.name = availablePaints[0].name + "_Instance_" + wallObj.name;
                                          Debug.Log($"Создан экземпляр материала {instanceMaterial.name} для объекта {wallObj.name}");
                                    }
                                    else
                                    {
                                          Debug.LogWarning($"Не найдены доступные материалы для покраски. Создаем стандартный материал.");

                                          // Создаем материал с подходящим шейдером
                                          instanceMaterial = new Material(GetAppropriateShader());

                                          instanceMaterial.name = "DefaultWallMaterial_Instance_" + wallObj.name;
                                          instanceMaterial.color = Color.white;
                                    }

                                    renderer.sharedMaterial = instanceMaterial;

                                    Debug.Log($"Установлен материал {instanceMaterial.name} для объекта {wallObj.name}");

                                    // Устанавливаем оригинальный материал в трекере
                                    System.Reflection.FieldInfo originalMaterialField = FindField(tracker.GetType(), "originalMaterial");
                                    if (originalMaterialField != null)
                                    {
                                          originalMaterialField.SetValue(tracker, instanceMaterial);
                                          Debug.Log($"Установлен оригинальный материал {instanceMaterial.name} в трекере для объекта {wallObj.name}");
                                    }
                              }
                              else
                              {
                                    // Проверяем, является ли материал экземпляром
                                    bool isInstance = false;
                                    foreach (Material paint in availablePaints)
                                    {
                                          if (currentMaterial.name.Contains(paint.name))
                                          {
                                                isInstance = true;
                                                break;
                                          }
                                    }

                                    if (!isInstance)
                                    {
                                          Debug.LogWarning($"Материал {currentMaterial.name} объекта {wallObj.name} не является экземпляром доступных материалов. Создаем экземпляр...");

                                          // Создаем экземпляр материала для стены
                                          Material instanceMaterial;
                                          if (availablePaints != null && availablePaints.Length > 0 && availablePaints[0] != null)
                                          {
                                                instanceMaterial = new Material(availablePaints[0]);
                                                instanceMaterial.name = availablePaints[0].name + "_Instance_" + wallObj.name;
                                                Debug.Log($"Создан экземпляр материала {instanceMaterial.name} для объекта {wallObj.name}");
                                          }
                                          else
                                          {
                                                Debug.LogWarning($"Не найдены доступные материалы для покраски. Создаем стандартный материал.");

                                                // Создаем материал с подходящим шейдером
                                                instanceMaterial = new Material(GetAppropriateShader());

                                                instanceMaterial.name = "DefaultWallMaterial_Instance_" + wallObj.name;
                                                instanceMaterial.color = Color.white;
                                          }

                                          renderer.sharedMaterial = instanceMaterial;

                                          Debug.Log($"Установлен материал {instanceMaterial.name} для объекта {wallObj.name}");

                                          // Устанавливаем оригинальный материал в трекере
                                          System.Reflection.FieldInfo originalMaterialField = FindField(tracker.GetType(), "originalMaterial");
                                          if (originalMaterialField != null)
                                          {
                                                originalMaterialField.SetValue(tracker, instanceMaterial);
                                                Debug.Log($"Установлен оригинальный материал {instanceMaterial.name} в трекере для объекта {wallObj.name}");
                                          }
                                    }
                              }
                        }

                        // Сохраняем изменения в сцене
                        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
                        Debug.Log("Сцена помечена как измененная. Не забудьте сохранить изменения.");
                  }
                  catch (System.Exception e)
                  {
                        Debug.LogError($"Ошибка при исправлении экземпляров материалов стен: {e.Message}");
                        Debug.LogException(e);
                  }

                  Debug.Log("=== Исправление экземпляров материалов стен завершено ===");
            }

            private static void EnsureWallColliders()
            {
                  Debug.Log("=== Проверка коллайдеров стен ===");

                  try
                  {
                        // Проверяем, существует ли слой Wall
                        int wallLayerIndex = LayerMask.NameToLayer("Wall");
                        if (wallLayerIndex == -1)
                        {
                              Debug.LogError("Слой 'Wall' не найден в проекте. Невозможно проверить коллайдеры стен.");
                              return;
                        }

                        // Находим все объекты на слое Wall
                        GameObject[] allObjects = Object.FindObjectsOfType<GameObject>();
                        int fixedCount = 0;

                        foreach (GameObject obj in allObjects)
                        {
                              if (obj.layer == wallLayerIndex)
                              {
                                    // Проверяем наличие коллайдера
                                    Collider collider = obj.GetComponent<Collider>();
                                    if (collider == null)
                                    {
                                          // Проверяем форму объекта
                                          MeshFilter meshFilter = obj.GetComponent<MeshFilter>();
                                          if (meshFilter != null)
                                          {
                                                // Если есть меш, добавляем MeshCollider
                                                MeshCollider meshCollider = obj.AddComponent<MeshCollider>();
                                                fixedCount++;
                                                Debug.Log($"Добавлен MeshCollider к объекту '{obj.name}'");
                                          }
                                          else
                                          {
                                                // Если нет меша, добавляем BoxCollider
                                                BoxCollider boxCollider = obj.AddComponent<BoxCollider>();
                                                fixedCount++;
                                                Debug.Log($"Добавлен BoxCollider к объекту '{obj.name}'");
                                          }
                                    }
                              }
                        }

                        Debug.Log($"=== Всего добавлено коллайдеров: {fixedCount} ===");

                        // Сохраняем изменения в сцене
                        if (fixedCount > 0)
                        {
                              EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
                              Debug.Log("Сцена помечена как измененная. Не забудьте сохранить изменения.");
                        }
                  }
                  catch (System.Exception e)
                  {
                        Debug.LogError($"Критическая ошибка при проверке коллайдеров стен: {e.Message}");
                  }
            }

            [MenuItem("Tools/Wall Painting/Fix/Complete Wall Painting System Fix")]
            public static void CompleteWallPaintingSystemFix()
            {
                  Debug.Log("=== НАЧАЛО ПОЛНОГО ИСПРАВЛЕНИЯ СИСТЕМЫ ПОКРАСКИ СТЕН ===");

                  try
                  {
                        // Шаг 1: Удаляем отсутствующие скрипты
                        Debug.Log("Шаг 1: Удаление отсутствующих скриптов...");
                        RemoveMissingScripts();

                        // Шаг 2: Проверяем и исправляем слои стен
                        Debug.Log("Шаг 2: Проверка и исправление слоев стен...");
                        CheckWallLayers();

                        // Шаг 3: Убеждаемся, что у стен есть тег "Wall"
                        Debug.Log("Шаг 3: Проверка и установка тегов стен...");
                        EnsureWallTags();

                        // Шаг 4: Проверяем и добавляем коллайдеры к стенам
                        Debug.Log("Шаг 4: Проверка и добавление коллайдеров к стенам...");
                        EnsureWallColliders();

                        // Шаг 5: Проверяем и исправляем материалы стен
                        Debug.Log("Шаг 5: Проверка и исправление материалов стен...");
                        CheckWallMaterials();

                        // Шаг 6: Проверяем и исправляем WallMaterialInstanceTracker на всех стенах
                        Debug.Log("Шаг 6: Проверка и исправление трекеров материалов стен...");
                        FixWallMaterialTrackers();

                        // Шаг 7: Проверяем и исправляем экземпляры материалов стен
                        Debug.Log("Шаг 7: Проверка и исправление экземпляров материалов стен...");
                        FixWallMaterialInstances();

                        // Шаг 8: Исправляем систему покраски стен
                        Debug.Log("Шаг 8: Исправление системы покраски стен...");
                        FixWallPaintingSystem();

                        // Сохраняем изменения в сцене
                        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
                        Debug.Log("Сцена помечена как измененная. Не забудьте сохранить изменения.");

                        // Спрашиваем пользователя о тестировании
                        if (EditorUtility.DisplayDialog("Тестирование системы покраски",
                              "Комплексное исправление системы покраски стен завершено. Хотите протестировать систему сейчас?",
                              "Да, протестировать", "Нет, позже"))
                        {
                              TestWallPainting();
                        }
                  }
                  catch (System.Exception e)
                  {
                        Debug.LogError($"Критическая ошибка при комплексном исправлении системы покраски стен: {e.Message}");
                        Debug.LogException(e);
                  }

                  Debug.Log("=== ПОЛНОЕ ИСПРАВЛЕНИЕ СИСТЕМЫ ПОКРАСКИ СТЕН ЗАВЕРШЕНО ===");
            }

            [MenuItem("Tools/Wall Painting/Fix/Fix Wall Painting System (Complete)")]
            public static void FixWallPaintingSystemComplete()
            {
                  Debug.Log("=== НАЧАЛО ПОЛНОГО ИСПРАВЛЕНИЯ СИСТЕМЫ ПОКРАСКИ СТЕН ===");

                  try
                  {
                        // Шаг 1: Проверяем и исправляем слои
                        int wallLayerIndex = LayerMask.NameToLayer("Wall");
                        if (wallLayerIndex == -1)
                        {
                              Debug.LogError("Слой 'Wall' не найден! Создайте слой 'Wall' в настройках проекта.");
                              return;
                        }

                        // Шаг 2: Находим все стены
                        GameObject[] allObjects = Object.FindObjectsOfType<GameObject>();
                        List<GameObject> wallObjects = new List<GameObject>();

                        foreach (GameObject obj in allObjects)
                        {
                              if (obj.layer == wallLayerIndex)
                              {
                                    wallObjects.Add(obj);
                              }
                        }

                        if (wallObjects.Count == 0)
                        {
                              Debug.LogError("Не найдено объектов на слое Wall!");
                              return;
                        }

                        Debug.Log($"Найдено {wallObjects.Count} объектов на слое Wall");

                        // Шаг 3: Проверяем WallPainter
                        MonoBehaviour wallPainter = FindWallPainterInScene();
                        if (wallPainter == null)
                        {
                              Debug.LogError("WallPainter не найден в сцене!");
                              return;
                        }

                        // Шаг 4: Проверяем и исправляем материалы на каждой стене
                        foreach (GameObject wallObj in wallObjects)
                        {
                              try
                              {
                                    // Проверяем Renderer
                                    Renderer renderer = wallObj.GetComponent<Renderer>();
                                    if (renderer == null)
                                    {
                                          Debug.LogError($"На объекте {wallObj.name} отсутствует Renderer!");
                                          continue;
                                    }

                                    // Проверяем WallMaterialInstanceTracker
                                    WallMaterialInstanceTracker tracker = wallObj.GetComponent<WallMaterialInstanceTracker>();
                                    if (tracker == null)
                                    {
                                          tracker = wallObj.AddComponent<WallMaterialInstanceTracker>();
                                          Debug.Log($"Добавлен WallMaterialInstanceTracker к {wallObj.name}");
                                    }

                                    // Сохраняем оригинальный материал
                                    if (tracker.OriginalSharedMaterial == null)
                                    {
                                          tracker.OriginalSharedMaterial = renderer.sharedMaterial;
                                          Debug.Log($"Сохранен оригинальный материал для {wallObj.name}");
                                    }

                                    // Проверяем текущий материал
                                    if (renderer.sharedMaterial == null)
                                    {
                                          Debug.LogError($"На объекте {wallObj.name} отсутствует материал!");
                                          continue;
                                    }

                                    // Создаем уникальный экземпляр материала, если его еще нет
                                    if (!renderer.sharedMaterial.name.Contains("_Instance_"))
                                    {
                                          Material instanceMaterial = new Material(renderer.sharedMaterial);
                                          instanceMaterial.name = $"{renderer.sharedMaterial.name}_Instance_{wallObj.name}";
                                          renderer.sharedMaterial = instanceMaterial;
                                          tracker.SetInstancedMaterial(instanceMaterial, true);
                                          Debug.Log($"Создан уникальный экземпляр материала для {wallObj.name}");
                                    }

                                    // Проверяем коллайдер
                                    Collider collider = wallObj.GetComponent<Collider>();
                                    if (collider == null)
                                    {
                                          // Добавляем подходящий коллайдер
                                          MeshFilter meshFilter = wallObj.GetComponent<MeshFilter>();
                                          if (meshFilter != null && meshFilter.sharedMesh != null)
                                          {
                                                MeshCollider meshCollider = wallObj.AddComponent<MeshCollider>();
                                                Debug.Log($"Добавлен MeshCollider к {wallObj.name}");
                                          }
                                          else
                                          {
                                                BoxCollider boxCollider = wallObj.AddComponent<BoxCollider>();
                                                Debug.Log($"Добавлен BoxCollider к {wallObj.name}");
                                          }
                                    }

                                    // Помечаем объекты как измененные
                                    EditorUtility.SetDirty(wallObj);
                                    EditorUtility.SetDirty(renderer);
                                    EditorUtility.SetDirty(tracker);
                              }
                              catch (System.Exception e)
                              {
                                    Debug.LogError($"Ошибка при обработке объекта {wallObj.name}: {e.Message}");
                              }
                        }

                        // Шаг 5: Проверяем настройки WallPainter
                        System.Type wallPainterType = wallPainter.GetType();

                        // Проверяем камеру
                        FieldInfo mainCameraField = FindField(wallPainterType, "mainCamera");
                        if (mainCameraField != null)
                        {
                              Camera mainCamera = mainCameraField.GetValue(wallPainter) as Camera;
                              if (mainCamera == null)
                              {
                                    mainCamera = Camera.main;
                                    if (mainCamera != null)
                                    {
                                          mainCameraField.SetValue(wallPainter, mainCamera);
                                          Debug.Log("Установлена основная камера для WallPainter");
                                    }
                              }
                        }

                        // Проверяем маску слоя
                        FieldInfo wallLayerMaskField = FindField(wallPainterType, "wallLayerMask");
                        if (wallLayerMaskField != null)
                        {
                              int wallLayerMask = 1 << wallLayerIndex;
                              // Convert to LayerMask before setting
                              LayerMask layerMask = new LayerMask();
                              layerMask.value = wallLayerMask;
                              wallLayerMaskField.SetValue(wallPainter, layerMask);
                              Debug.Log($"Установлена маска слоя Wall: {layerMask.value}");
                        }

                        // Проверяем материалы
                        FieldInfo availablePaintsField = FindField(wallPainterType, "availablePaints");
                        if (availablePaintsField != null)
                        {
                              Material[] availablePaints = availablePaintsField.GetValue(wallPainter) as Material[];
                              if (availablePaints == null || availablePaints.Length == 0)
                              {
                                    Debug.LogWarning("WallPainter не имеет доступных материалов для покраски!");
                              }
                              else
                              {
                                    Debug.Log($"WallPainter имеет {availablePaints.Length} доступных материалов");

                                    // Проверяем текущий материал
                                    FieldInfo currentPaintMaterialField = FindField(wallPainterType, "currentPaintMaterial");
                                    if (currentPaintMaterialField != null)
                                    {
                                          Material currentMaterial = currentPaintMaterialField.GetValue(wallPainter) as Material;
                                          if (currentMaterial == null && availablePaints.Length > 0)
                                          {
                                                currentPaintMaterialField.SetValue(wallPainter, availablePaints[0]);
                                                Debug.Log($"Установлен текущий материал: {availablePaints[0].name}");
                                          }
                                    }
                              }
                        }

                        // Сохраняем изменения в сцене
                        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
                        Debug.Log("Сцена помечена как измененная. Не забудьте сохранить изменения.");

                        Debug.Log("=== ПОЛНОЕ ИСПРАВЛЕНИЕ СИСТЕМЫ ПОКРАСКИ СТЕН ЗАВЕРШЕНО ===");

                        // Спрашиваем пользователя о тестировании
                        if (EditorUtility.DisplayDialog("Тестирование системы покраски",
                              "Комплексное исправление системы покраски стен завершено. Хотите протестировать систему сейчас?",
                              "Да, протестировать", "Нет, позже"))
                        {
                              TestWallPainting();
                        }
                  }
                  catch (System.Exception e)
                  {
                        Debug.LogError($"Критическая ошибка при исправлении системы покраски стен: {e.Message}");
                        Debug.LogException(e);
                  }
            }
      }
}
#endif