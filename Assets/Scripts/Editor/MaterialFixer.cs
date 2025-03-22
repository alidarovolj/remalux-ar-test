#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace Remalux.AR
{
      public class MaterialFixer : EditorWindow
      {
            private const string PAINT_MATERIALS_PATH = "Assets/Materials/PaintMaterials";

            [MenuItem("Tools/Wall Painting/Fix/Fix Materials")]
            public static void FixMaterials()
            {
                  // Получаем URP шейдер
                  var urpShader = Shader.Find("Universal Render Pipeline/Lit");
                  if (urpShader == null)
                  {
                        Debug.LogError("URP Shader not found! Make sure URP is installed and configured.");
                        return;
                  }

                  // Исправляем все материалы в папке PaintMaterials
                  if (Directory.Exists(PAINT_MATERIALS_PATH))
                  {
                        var materialGuids = AssetDatabase.FindAssets("t:Material", new[] { PAINT_MATERIALS_PATH });
                        foreach (var guid in materialGuids)
                        {
                              string path = AssetDatabase.GUIDToAssetPath(guid);
                              Material material = AssetDatabase.LoadAssetAtPath<Material>(path);

                              if (material != null)
                              {
                                    // Сохраняем цвет
                                    Color originalColor = material.color;

                                    // Устанавливаем правильный шейдер
                                    material.shader = urpShader;

                                    // Восстанавливаем цвет
                                    material.SetColor("_BaseColor", originalColor);
                                    material.SetColor("_Color", originalColor);

                                    // Устанавливаем базовые параметры
                                    material.SetFloat("_Metallic", 0);
                                    material.SetFloat("_Smoothness", 0);
                                    material.SetFloat("_BumpScale", 1);

                                    // Отключаем прозрачность
                                    material.SetFloat("_Surface", 0); // 0 = Opaque
                                    material.SetFloat("_AlphaClip", 0);
                                    material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Geometry;

                                    EditorUtility.SetDirty(material);
                              }
                        }

                        // Сохраняем все изменения
                        AssetDatabase.SaveAssets();
                        AssetDatabase.Refresh();

                        Debug.Log("All paint materials have been updated to use URP shader with correct settings.");
                  }
                  else
                  {
                        Debug.LogError($"Paint materials folder not found at {PAINT_MATERIALS_PATH}");
                  }

                  // Обновляем все экземпляры материалов в сцене
                  var renderers = Object.FindObjectsOfType<Renderer>();
                  foreach (var renderer in renderers)
                  {
                        var tracker = renderer.GetComponent<WallMaterialInstanceTracker>();
                        if (tracker != null)
                        {
                              tracker.ResetToOriginal();
                        }
                  }
            }
      }
}
#endif