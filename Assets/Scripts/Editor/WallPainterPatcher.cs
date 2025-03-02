using UnityEngine;
using UnityEditor;
using System.Reflection;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace Remalux.AR
{
      public static class WallPainterPatcher
      {
            [MenuItem("Tools/Wall Painting/Fix/Patch WallPainter Script")]
            public static void PatchWallPainterScript()
            {
                  Debug.Log("=== Патчинг скрипта WallPainter ===");

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
                  bool applyMaterialDirectlyAdded = AddApplyMaterialDirectlyMethod(ref scriptContent);

                  // Сохраняем изменения, если были внесены патчи
                  if (handleWallHitPatched || paintWallAtPositionPatched || applyMaterialDirectlyAdded)
                  {
                        File.WriteAllText(assetPath, scriptContent);
                        AssetDatabase.Refresh();
                        Debug.Log("Скрипт WallPainter успешно обновлен");
                  }
                  else
                  {
                        Debug.Log("Изменения не требуются или не удалось применить патчи");
                  }
            }

            private static bool PatchHandleWallHit(ref string scriptContent)
            {
                  // Ищем метод HandleWallHit
                  string handleWallHitPattern = @"(private|protected)\s+void\s+HandleWallHit\s*\(\s*RaycastHit\s+hit\s*\)\s*\{[^}]*\}";
                  Match match = Regex.Match(scriptContent, handleWallHitPattern, RegexOptions.Singleline);

                  if (match.Success)
                  {
                        string originalMethod = match.Value;
                        Debug.Log("Найден метод HandleWallHit");

                        // Проверяем, содержит ли метод уже вызов ApplyMaterialDirectly
                        if (originalMethod.Contains("ApplyMaterialDirectly"))
                        {
                              Debug.Log("Метод HandleWallHit уже содержит вызов ApplyMaterialDirectly, пропускаем патч");
                              return false;
                        }

                        // Создаем новую версию метода
                        string patchedMethod = originalMethod;

                        // Ищем строку с применением материала (renderer.material = currentPaintMaterial)
                        string materialAssignmentPattern = @"(renderer\.material\s*=\s*currentPaintMaterial)";
                        Match materialAssignmentMatch = Regex.Match(originalMethod, materialAssignmentPattern);

                        if (materialAssignmentMatch.Success)
                        {
                              // Заменяем прямое присваивание на вызов метода ApplyMaterialDirectly
                              patchedMethod = patchedMethod.Replace(
                                  materialAssignmentMatch.Value,
                                  "ApplyMaterialDirectly(hit.collider.gameObject, currentPaintMaterial)"
                              );

                              // Заменяем оригинальный метод на патченный
                              scriptContent = scriptContent.Replace(originalMethod, patchedMethod);
                              Debug.Log("Метод HandleWallHit успешно обновлен");
                              return true;
                        }
                        else
                        {
                              Debug.LogWarning("Не удалось найти строку с присваиванием материала в методе HandleWallHit");
                        }
                  }
                  else
                  {
                        Debug.LogWarning("Не удалось найти метод HandleWallHit");
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
                        string originalMethod = match.Value;
                        Debug.Log("Найден метод PaintWallAtPosition");

                        // Проверяем, содержит ли метод уже проверку на null
                        if (originalMethod.Contains("if (mainCamera == null)"))
                        {
                              Debug.Log("Метод PaintWallAtPosition уже содержит проверку на null, пропускаем патч");
                        }
                        else
                        {
                              // Добавляем проверку на null для mainCamera
                              string patchedMethod = originalMethod.Replace(
                                  "public void PaintWallAtPosition(Vector2 screenPosition)",
                                  "public void PaintWallAtPosition(Vector2 screenPosition)\n    {\n        if (mainCamera == null)\n        {\n            Debug.LogError(\"WallPainter: mainCamera is null\");\n            return;\n        }"
                              );

                              // Заменяем оригинальный метод на патченный
                              scriptContent = scriptContent.Replace(originalMethod, patchedMethod);
                              Debug.Log("Метод PaintWallAtPosition успешно обновлен с проверкой на null");
                              return true;
                        }
                  }
                  else
                  {
                        Debug.LogWarning("Не удалось найти метод PaintWallAtPosition");
                  }

                  return false;
            }

            private static bool AddApplyMaterialDirectlyMethod(ref string scriptContent)
            {
                  // Проверяем, существует ли уже метод ApplyMaterialDirectly
                  if (scriptContent.Contains("ApplyMaterialDirectly"))
                  {
                        Debug.Log("Метод ApplyMaterialDirectly уже существует, пропускаем добавление");
                        return false;
                  }

                  // Ищем закрывающую скобку класса
                  int lastClosingBrace = scriptContent.LastIndexOf('}');
                  if (lastClosingBrace == -1)
                  {
                        Debug.LogError("Не удалось найти закрывающую скобку класса");
                        return false;
                  }

                  // Добавляем новый метод перед закрывающей скобкой класса
                  string newMethod = @"
    // Метод для безопасного применения материала к объекту
    private void ApplyMaterialDirectly(GameObject targetObject, Material materialToApply)
    {
        if (targetObject == null || materialToApply == null)
        {
            Debug.LogWarning(""WallPainter: Невозможно применить материал - объект или материал равны null"");
            return;
        }

        Renderer renderer = targetObject.GetComponent<Renderer>();
        if (renderer == null)
        {
            Debug.LogWarning(""WallPainter: Объект "" + targetObject.name + "" не имеет компонента Renderer"");
            return;
        }

        // Проверяем, есть ли компонент для отслеживания материалов
        WallMaterialInstanceTracker tracker = targetObject.GetComponent<WallMaterialInstanceTracker>();
        if (tracker != null)
        {
            // Используем метод компонента для применения материала
            tracker.ApplyMaterial(materialToApply);
            Debug.Log(""WallPainter: Материал "" + materialToApply.name + "" применен к объекту "" + targetObject.name + "" через WallMaterialInstanceTracker"");
        }
        else
        {
            // Создаем экземпляр материала
            Material instancedMaterial = new Material(materialToApply);
            instancedMaterial.name = materialToApply.name + ""_Instance_"" + targetObject.name;
            
            // Применяем экземпляр материала
            renderer.material = instancedMaterial;
            
            // Добавляем компонент для отслеживания
            tracker = targetObject.AddComponent<WallMaterialInstanceTracker>();
            tracker.originalSharedMaterial = renderer.sharedMaterial;
            tracker.instancedMaterial = instancedMaterial;
            
            Debug.Log(""WallPainter: Создан экземпляр материала "" + instancedMaterial.name + "" и применен к объекту "" + targetObject.name);
        }
    }
";

                  // Вставляем новый метод
                  scriptContent = scriptContent.Insert(lastClosingBrace, newMethod);
                  Debug.Log("Метод ApplyMaterialDirectly успешно добавлен");
                  return true;
            }

            [MenuItem("Tools/Wall Painting/Fix/Restore WallPainter Script")]
            public static void RestoreWallPainterScript()
            {
                  Debug.Log("=== Восстановление скрипта WallPainter из резервной копии ===");

                  // Находим скрипт WallPainter
                  string[] guids = AssetDatabase.FindAssets("WallPainter t:MonoScript");
                  if (guids.Length == 0)
                  {
                        Debug.LogError("Не удалось найти скрипт WallPainter");
                        return;
                  }

                  string assetPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                  string backupPath = assetPath + ".backup";

                  if (File.Exists(backupPath))
                  {
                        // Копируем резервную копию обратно
                        File.Copy(backupPath, assetPath, true);
                        Debug.Log($"Скрипт WallPainter восстановлен из резервной копии: {backupPath}");
                        AssetDatabase.Refresh();
                  }
                  else
                  {
                        Debug.LogError($"Резервная копия не найдена: {backupPath}");
                  }
            }
      }
}