#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace Remalux.AR
{
      public static class WallPaintingMenu
      {
            /// <summary>
            /// Возвращает подходящий шейдер в зависимости от используемого рендер пайплайна
            /// </summary>
            private static Shader GetAppropriateShader()
            {
                  if (UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline != null)
                  {
                        // Для URP
                        Debug.Log("Используется URP, возвращаем URP шейдер");
                        return Shader.Find("Universal Render Pipeline/Lit");
                  }
                  else
                  {
                        // Для стандартного рендер пайплайна
                        return Shader.Find("Standard");
                  }
            }

            [MenuItem("Tools/Setup Wall Painting System")]
            public static void SetupWallPaintingSystem()
            {
                  Debug.Log("Настройка системы покраски стен...");

                  // Создаем папку для материалов
                  string folderPath = "Assets/Materials/PaintMaterials";
                  if (!Directory.Exists(Application.dataPath + "/Materials"))
                  {
                        Directory.CreateDirectory(Application.dataPath + "/Materials");
                  }

                  if (!Directory.Exists(Application.dataPath + "/Materials/PaintMaterials"))
                  {
                        Directory.CreateDirectory(Application.dataPath + "/Materials/PaintMaterials");
                        AssetDatabase.Refresh();
                  }

                  // Создаем базовые цвета
                  Color[] colors = new Color[]
                  {
                        Color.red,
                        Color.green,
                        Color.blue,
                        Color.yellow,
                        Color.cyan,
                        Color.magenta
                  };

                  string[] names = new string[]
                  {
                        "RedPaint",
                        "GreenPaint",
                        "BluePaint",
                        "YellowPaint",
                        "CyanPaint",
                        "MagentaPaint"
                  };

                  List<Material> materials = new List<Material>();

                  for (int i = 0; i < colors.Length; i++)
                  {
                        string materialName = names[i];
                        string materialPath = $"{folderPath}/{materialName}.mat";

                        // Проверяем, существует ли уже такой материал
                        Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                        if (material == null)
                        {
                              // Создаем новый материал
                              material = new Material(GetAppropriateShader());
                              material.color = colors[i];
                              AssetDatabase.CreateAsset(material, materialPath);
                        }
                        else
                        {
                              // Обновляем существующий материал
                              material.color = colors[i];
                              EditorUtility.SetDirty(material);
                        }

                        materials.Add(material);
                  }

                  AssetDatabase.SaveAssets();
                  Debug.Log($"Создано {materials.Count} материалов для покраски");

                  // Находим WallPaintingManager в сцене
                  var managers = Object.FindObjectsOfType<MonoBehaviour>();
                  int count = 0;

                  foreach (var component in managers)
                  {
                        if (component.GetType().Name == "WallPaintingManager")
                        {
                              // Используем рефлексию для установки материалов
                              var paintMaterialsField = component.GetType().GetField("paintMaterials");
                              if (paintMaterialsField != null)
                              {
                                    paintMaterialsField.SetValue(component, materials.ToArray());

                                    // Устанавливаем материал по умолчанию
                                    var defaultMaterialField = component.GetType().GetField("defaultWallMaterial");
                                    if (defaultMaterialField != null && materials.Count > 0)
                                    {
                                          defaultMaterialField.SetValue(component, materials[0]);
                                    }

                                    EditorUtility.SetDirty(component);
                                    count++;
                              }
                        }
                  }

                  if (count > 0)
                  {
                        Debug.Log($"Материалы установлены для {count} WallPaintingManager");
                  }
                  else
                  {
                        Debug.LogWarning("WallPaintingManager не найден в сцене");
                  }
            }
      }
}
#endif