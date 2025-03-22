#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace Remalux.AR
{
      public static class WallPaintingMaterialFixer
      {
            private const string PAINT_MATERIALS_PATH = "Assets/Materials/PaintMaterials";

            /// <summary>
            /// Returns the appropriate shader based on the render pipeline
            /// </summary>
            private static Shader GetAppropriateShader()
            {
                  if (UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline != null)
                  {
                        Debug.Log("Using URP, returning URP shader");
                        return Shader.Find("Universal Render Pipeline/Lit");
                  }
                  else
                  {
                        return Shader.Find("Standard");
                  }
            }

            [MenuItem("Tools/Wall Painting/Fix/Update Paint Materials")]
            public static void UpdatePaintMaterials()
            {
                  var paintMaterialsDir = new DirectoryInfo(PAINT_MATERIALS_PATH);
                  if (!paintMaterialsDir.Exists)
                  {
                        Debug.LogError($"Paint materials directory not found at {PAINT_MATERIALS_PATH}");
                        return;
                  }

                  var materialFiles = paintMaterialsDir.GetFiles("*.mat");
                  var shader = GetAppropriateShader();
                  if (shader == null)
                  {
                        Debug.LogError("Could not find appropriate shader!");
                        return;
                  }

                  int updatedCount = 0;
                  foreach (var matFile in materialFiles)
                  {
                        string assetPath = PAINT_MATERIALS_PATH + "/" + matFile.Name;
                        var material = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
                        if (material != null)
                        {
                              if (material.shader != shader)
                              {
                                    Debug.Log($"Updating shader for material: {material.name}");
                                    material.shader = shader;
                                    EditorUtility.SetDirty(material);
                                    updatedCount++;
                              }
                        }
                  }

                  if (updatedCount > 0)
                  {
                        AssetDatabase.SaveAssets();
                        Debug.Log($"Updated {updatedCount} materials to use {shader.name}");
                  }
                  else
                  {
                        Debug.Log("No materials needed updating");
                  }
            }
      }
}
#endif