using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using System.Text.RegularExpressions;

namespace Remalux.AR
{
      public static class WallPainterFixAll
      {
            [MenuItem("Tools/Wall Painting/Fix/Fix All Wall Painting Issues")]
            public static void FixAllWallPaintingIssues()
            {
                  Debug.Log("=== Начало комплексного исправления системы покраски стен ===");

                  // 1. Патчим скрипт WallPainter
                  PatchWallPainterScript();

                  // 2. Проверяем и исправляем слои стен
                  FixWallLayers();

                  // 3. Добавляем компоненты WallMaterialInstanceTracker ко всем стенам
                  AddTrackerComponentsToWalls();

                  // 4. Исправляем общие материалы
                  FixSharedMaterials();

                  // 5. Применяем текущий материал покраски ко всем стенам
                  ApplyCurrentPaintMaterial();

                  Debug.Log("=== Комплексное исправление системы покраски стен завершено ===");
            }

            private static void PatchWallPainterScript()
            {
                  Debug.Log("--- Патчинг скрипта WallPainter ---");

                  // Находим скрипт WallPainter
                  string[] guids = AssetDatabase.FindAssets("WallPainter t:MonoScript");
                  if (guids.Length == 0)
                  {
                        Debug.LogError("Не удалось найти скрипт WallPainter");
                        return;
                  }

                  string assetPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                  Debug.Log($"Найден скрипт WallPainter по пути: {assetPath}");

                  // Создаем резервную копию
                  string backupPath = assetPath + ".backup";
                  if (!File.Exists(backupPath))
                  {
                        File.Copy(assetPath, backupPath);
                        Debug.Log($"Создана резервная копия: {backupPath}");
                  }
                  else
                  {
                        Debug.Log("Резервная копия уже существует, пропускаем создание");
                  }

                  // Читаем содержимое файла
                  string scriptContent = File.ReadAllText(assetPath);
                  string originalContent = scriptContent;

                  // Патчим метод HandleWallHit
                  bool handleWallHitPatched = PatchHandleWallHit(ref scriptContent);

                  // Патчим метод PaintWallAtPosition
                  bool paintWallAtPositionPatched = PatchPaintWallAtPosition(ref scriptContent);

                  // Добавляем новый метод для прямого применения материала
                  bool applyMethodAdded = AddApplyMaterialDirectlyMethod(ref scriptContent);

                  // Сохраняем изменения, если были внесены изменения
                  if (scriptContent != originalContent)
                  {
                        File.WriteAllText(assetPath, scriptContent);
                        AssetDatabase.Refresh();
                        Debug.Log("Скрипт WallPainter успешно обновлен");
                  }
                  else
                  {
                        Debug.Log("Скрипт WallPainter не требует изменений");
                  }
            }

            private static bool PatchHandleWallHit(ref string scriptContent)
            {
                  // Ищем метод HandleWallHit
                  string handleWallHitPattern = @"private\s+void\s+HandleWallHit\s*\(\s*RaycastHit\s+hit\s*\)\s*\{[^}]*wallRenderer\.material\s*=\s*currentPaintMaterial;[^}]*\}";
                  Match match = Regex.Match(scriptContent, handleWallHitPattern, RegexOptions.Singleline);

                  if (match.Success)
                  {
                        // Проверяем, не содержит ли уже метод вызов ApplyMaterialDirectly
                        if (!match.Value.Contains("ApplyMaterialDirectly"))
                        {
                              // Заменяем прямое присваивание материала на вызов метода ApplyMaterialDirectly
                              string patchedMethod = match.Value.Replace(
                                  "wallRenderer.material = currentPaintMaterial;",
                                  "ApplyMaterialDirectly(wallObject, currentPaintMaterial);"
                              );

                              scriptContent = scriptContent.Replace(match.Value, patchedMethod);
                              Debug.Log("Метод HandleWallHit успешно исправлен");
                              return true;
                        }
                        else
                        {
                              Debug.Log("Метод HandleWallHit уже содержит вызов ApplyMaterialDirectly");
                        }
                  }
                  else
                  {
                        Debug.LogWarning("Не удалось найти метод HandleWallHit или шаблон не соответствует");
                  }

                  return false;
            }

            private static bool PatchPaintWallAtPosition(ref string scriptContent)
            {
                  // Ищем метод PaintWallAtPosition
                  string paintWallAtPositionPattern = @"public\s+void\s+PaintWallAtPosition\s*\(\s*Vector2\s+screenPosition\s*\)\s*\{[^}]*\}";
                  Match match = Regex.Match(scriptContent, paintWallAtPositionPattern, RegexOptions.Singleline);

                  if (match.Success)
                  {
                        string methodContent = match.Value;

                        // Проверяем, содержит ли метод проверку на null для mainCamera
                        if (!methodContent.Contains("if (mainCamera == null)"))
                        {
                              // Добавляем проверку на null для mainCamera
                              string patchedMethod = methodContent.Replace(
                                  "public void PaintWallAtPosition(Vector2 screenPosition)\n    {",
                                  "public void PaintWallAtPosition(Vector2 screenPosition)\n    {\n        if (mainCamera == null)\n            return;"
                              );

                              scriptContent = scriptContent.Replace(methodContent, patchedMethod);
                              Debug.Log("Метод PaintWallAtPosition успешно исправлен (добавлена проверка на null для mainCamera)");
                              return true;
                        }
                        else
                        {
                              Debug.Log("Метод PaintWallAtPosition уже содержит проверку на null для mainCamera");
                        }

                        // Проверяем, не содержит ли уже метод вызов ApplyMaterialDirectly
                        if (!methodContent.Contains("ApplyMaterialDirectly") && methodContent.Contains("wallRenderer.material = currentPaintMaterial"))
                        {
                              // Заменяем прямое присваивание материала на вызов метода ApplyMaterialDirectly
                              string patchedMethod = methodContent.Replace(
                                  "wallRenderer.material = currentPaintMaterial;",
                                  "ApplyMaterialDirectly(wallObject, currentPaintMaterial);"
                              );

                              scriptContent = scriptContent.Replace(methodContent, patchedMethod);
                              Debug.Log("Метод PaintWallAtPosition успешно исправлен (добавлен вызов ApplyMaterialDirectly)");
                              return true;
                        }
                        else if (methodContent.Contains("ApplyMaterialDirectly"))
                        {
                              Debug.Log("Метод PaintWallAtPosition уже содержит вызов ApplyMaterialDirectly");
                        }
                  }
                  else
                  {
                        Debug.LogWarning("Не удалось найти метод PaintWallAtPosition или шаблон не соответствует");
                  }

                  return false;
            }

            private static bool AddApplyMaterialDirectlyMethod(ref string scriptContent)
            {
                  // Проверяем, содержит ли скрипт уже метод ApplyMaterialDirectly
                  if (scriptContent.Contains("private void ApplyMaterialDirectly(GameObject wallObject, Material material)"))
                  {
                        Debug.Log("Метод ApplyMaterialDirectly уже существует в скрипте");
                        return false;
                  }

                  // Ищем закрывающую скобку класса WallPainter
                  int lastBraceIndex = scriptContent.LastIndexOf('}');
                  if (lastBraceIndex > 0)
                  {
                        // Находим предпоследнюю закрывающую скобку (конец последнего метода)
                        int secondLastBraceIndex = scriptContent.LastIndexOf('}', lastBraceIndex - 1);
                        if (secondLastBraceIndex > 0)
                        {
                              // Добавляем новый метод перед закрывающей скобкой класса
                              string applyMaterialDirectlyMethod = @"
      /// <summary>
      /// Безопасно применяет материал к объекту, создавая экземпляр материала
      /// </summary>
      private void ApplyMaterialDirectly(GameObject wallObject, Material material)
      {
            if (wallObject == null || material == null)
                  return;

            // Проверяем наличие компонента WallMaterialInstanceTracker
            WallMaterialInstanceTracker tracker = wallObject.GetComponent<WallMaterialInstanceTracker>();
            if (tracker == null)
            {
                  // Если компонента нет, добавляем его
                  tracker = wallObject.AddComponent<WallMaterialInstanceTracker>();
                  Debug.Log($""Добавлен компонент WallMaterialInstanceTracker к объекту {wallObject.name}"");
            }

            // Применяем материал через трекер
            tracker.ApplyMaterial(material);
      }";

                              scriptContent = scriptContent.Insert(secondLastBraceIndex + 1, applyMaterialDirectlyMethod);
                              Debug.Log("Метод ApplyMaterialDirectly успешно добавлен в скрипт");
                              return true;
                        }
                  }

                  Debug.LogWarning("Не удалось добавить метод ApplyMaterialDirectly в скрипт");
                  return false;
            }

            private static void FixWallLayers()
            {
                  Debug.Log("--- Исправление слоев стен ---");

                  // Проверяем, существует ли слой "Wall"
                  int wallLayerIndex = LayerMask.NameToLayer("Wall");
                  if (wallLayerIndex == -1)
                  {
                        Debug.LogError("Слой 'Wall' не найден в проекте. Пожалуйста, создайте слой 'Wall' в настройках проекта (Edit > Project Settings > Tags and Layers)");
                        return;
                  }

                  Debug.Log($"Слой 'Wall' найден с индексом {wallLayerIndex}");

                  // Находим все объекты с тегом "Wall"
                  GameObject[] wallsWithTag = GameObject.FindGameObjectsWithTag("Wall");
                  Debug.Log($"Найдено {wallsWithTag.Length} объектов с тегом 'Wall'");

                  // Находим все объекты, которые могут быть стенами по имени
                  List<GameObject> potentialWalls = new List<GameObject>();
                  foreach (GameObject obj in Object.FindObjectsOfType<GameObject>())
                  {
                        if (obj.name.ToLower().Contains("wall") || obj.name.ToLower().Contains("стена"))
                        {
                              potentialWalls.Add(obj);
                        }
                  }

                  Debug.Log($"Найдено {potentialWalls.Count} потенциальных объектов стен по имени");

                  // Устанавливаем слой "Wall" для всех найденных объектов
                  int wallsFixed = 0;

                  // Сначала для объектов с тегом "Wall"
                  foreach (GameObject wall in wallsWithTag)
                  {
                        if (wall.layer != wallLayerIndex)
                        {
                              wall.layer = wallLayerIndex;
                              wallsFixed++;
                              Debug.Log($"Установлен слой 'Wall' для объекта {wall.name}");
                        }
                  }

                  // Затем для потенциальных стен
                  foreach (GameObject wall in potentialWalls)
                  {
                        if (wall.layer != wallLayerIndex)
                        {
                              wall.layer = wallLayerIndex;
                              wallsFixed++;
                              Debug.Log($"Установлен слой 'Wall' для потенциальной стены {wall.name}");
                        }
                  }

                  Debug.Log($"Исправлено {wallsFixed} объектов стен");

                  // Проверяем настройки WallPainter
                  MonoBehaviour[] allComponents = Object.FindObjectsOfType<MonoBehaviour>();
                  List<MonoBehaviour> wallPainters = new List<MonoBehaviour>();

                  foreach (MonoBehaviour component in allComponents)
                  {
                        if (component.GetType().Name == "WallPainter")
                        {
                              wallPainters.Add(component);
                        }
                  }

                  if (wallPainters.Count == 0)
                  {
                        Debug.LogWarning("Не найден компонент WallPainter в сцене.");
                        return;
                  }

                  int count = 0;
                  foreach (MonoBehaviour wallPainter in wallPainters)
                  {
                        FieldInfo wallLayerMaskField = wallPainter.GetType().GetField("wallLayerMask", BindingFlags.Public | BindingFlags.Instance);
                        if (wallLayerMaskField != null)
                        {
                              // Создаем маску, которая включает только слой "Wall"
                              LayerMask mask = 1 << wallLayerIndex;
                              wallLayerMaskField.SetValue(wallPainter, mask);

                              // Помечаем объект как измененный для сохранения изменений
                              EditorUtility.SetDirty(wallPainter);

                              count++;
                              Debug.Log($"Исправлена маска слоя для WallPainter на объекте {wallPainter.gameObject.name}. Новое значение: {mask.value}");
                        }
                  }

                  Debug.Log($"Исправлена маска слоя для {count} компонентов WallPainter");
            }

            private static void AddTrackerComponentsToWalls()
            {
                  Debug.Log("--- Добавление компонентов WallMaterialInstanceTracker ---");

                  // Находим все объекты на слое "Wall"
                  int wallLayerIndex = LayerMask.NameToLayer("Wall");
                  if (wallLayerIndex == -1)
                  {
                        Debug.LogError("Слой 'Wall' не найден в проекте");
                        return;
                  }

                  List<GameObject> wallObjects = new List<GameObject>();
                  foreach (GameObject obj in Object.FindObjectsOfType<GameObject>())
                  {
                        if (obj.layer == wallLayerIndex)
                        {
                              wallObjects.Add(obj);
                        }
                  }

                  Debug.Log($"Найдено {wallObjects.Count} объектов на слое 'Wall'");

                  int trackersAdded = 0;
                  foreach (GameObject wall in wallObjects)
                  {
                        // Проверяем наличие Renderer
                        Renderer renderer = wall.GetComponent<Renderer>();
                        if (renderer == null)
                        {
                              Debug.LogWarning($"Объект {wall.name} не имеет компонента Renderer и не может быть использован как стена");
                              continue;
                        }

                        // Проверяем наличие компонента WallMaterialInstanceTracker
                        WallMaterialInstanceTracker tracker = wall.GetComponent<WallMaterialInstanceTracker>();
                        if (tracker == null)
                        {
                              // Добавляем компонент
                              tracker = wall.AddComponent<WallMaterialInstanceTracker>();
                              trackersAdded++;
                              Debug.Log($"Добавлен компонент WallMaterialInstanceTracker к объекту {wall.name}");
                        }
                  }

                  Debug.Log($"Добавлено {trackersAdded} компонентов WallMaterialInstanceTracker");
            }

            private static void FixSharedMaterials()
            {
                  Debug.Log("--- Исправление общих материалов ---");

                  // Находим все объекты с компонентом WallMaterialInstanceTracker
                  WallMaterialInstanceTracker[] trackers = Object.FindObjectsOfType<WallMaterialInstanceTracker>();
                  Debug.Log($"Найдено {trackers.Length} объектов с компонентом WallMaterialInstanceTracker");

                  int materialsFixed = 0;
                  foreach (WallMaterialInstanceTracker tracker in trackers)
                  {
                        // Обновляем экземпляр материала
                        tracker.UpdateMaterialInstance();
                        materialsFixed++;
                  }

                  Debug.Log($"Обновлено {materialsFixed} экземпляров материалов");
            }

            private static void ApplyCurrentPaintMaterial()
            {
                  Debug.Log("--- Применение текущего материала покраски ---");

                  // Находим компонент WallPainter
                  MonoBehaviour[] allComponents = Object.FindObjectsOfType<MonoBehaviour>();
                  MonoBehaviour wallPainter = null;

                  foreach (MonoBehaviour component in allComponents)
                  {
                        if (component.GetType().Name == "WallPainter")
                        {
                              wallPainter = component;
                              break;
                        }
                  }

                  if (wallPainter == null)
                  {
                        Debug.LogError("Не найден компонент WallPainter в сцене");
                        return;
                  }

                  // Получаем текущий материал для покраски
                  FieldInfo currentPaintMaterialField = wallPainter.GetType().GetField("currentPaintMaterial", BindingFlags.NonPublic | BindingFlags.Instance);
                  if (currentPaintMaterialField == null)
                  {
                        Debug.LogError("Не удалось найти поле currentPaintMaterial в компоненте WallPainter");
                        return;
                  }

                  Material currentPaintMaterial = currentPaintMaterialField.GetValue(wallPainter) as Material;
                  if (currentPaintMaterial == null)
                  {
                        Debug.LogError("Текущий материал для покраски не установлен");

                        // Пытаемся получить материал из availablePaints
                        FieldInfo availablePaintsField = wallPainter.GetType().GetField("availablePaints", BindingFlags.Public | BindingFlags.Instance);
                        if (availablePaintsField != null)
                        {
                              Material[] availablePaints = availablePaintsField.GetValue(wallPainter) as Material[];
                              if (availablePaints != null && availablePaints.Length > 0)
                              {
                                    currentPaintMaterial = availablePaints[0];
                                    Debug.Log($"Использую первый доступный материал: {currentPaintMaterial.name}");

                                    // Устанавливаем текущий материал
                                    currentPaintMaterialField.SetValue(wallPainter, currentPaintMaterial);
                              }
                        }

                        if (currentPaintMaterial == null)
                        {
                              Debug.LogError("Не удалось найти материал для покраски");
                              return;
                        }
                  }

                  Debug.Log($"Текущий материал для покраски: {currentPaintMaterial.name}");

                  // Находим все объекты на слое "Wall"
                  int wallLayerIndex = LayerMask.NameToLayer("Wall");
                  if (wallLayerIndex == -1)
                  {
                        Debug.LogError("Слой 'Wall' не найден в проекте");
                        return;
                  }

                  List<GameObject> wallObjects = new List<GameObject>();
                  foreach (GameObject obj in Object.FindObjectsOfType<GameObject>())
                  {
                        if (obj.layer == wallLayerIndex)
                        {
                              wallObjects.Add(obj);
                        }
                  }

                  Debug.Log($"Найдено {wallObjects.Count} объектов на слое 'Wall'");

                  // Применяем материал ко всем стенам
                  int wallsPainted = 0;
                  foreach (GameObject wall in wallObjects)
                  {
                        // Проверяем наличие Renderer
                        Renderer renderer = wall.GetComponent<Renderer>();
                        if (renderer == null)
                        {
                              continue;
                        }

                        // Проверяем наличие компонента WallMaterialInstanceTracker
                        WallMaterialInstanceTracker tracker = wall.GetComponent<WallMaterialInstanceTracker>();
                        if (tracker == null)
                        {
                              // Добавляем компонент
                              tracker = wall.AddComponent<WallMaterialInstanceTracker>();
                        }

                        // Применяем материал
                        tracker.ApplyMaterial(currentPaintMaterial);
                        wallsPainted++;
                  }

                  Debug.Log($"Покрашено {wallsPainted} объектов стен");
            }
      }
}