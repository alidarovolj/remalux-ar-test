using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace Remalux.AR
{
      public static class WallPaintingSystemFixer
      {
            [MenuItem("Tools/Wall Painting/Fix/Complete System Fix")]
            public static void FixEntireSystem()
            {
                  Debug.Log("=== Starting Complete Wall Painting System Fix ===");

                  // Step 1: Fix missing scripts
                  RemoveMissingScripts();

                  // Step 2: Update materials to use URP shader
                  UpdateMaterials();

                  // Step 3: Fix material instances
                  FixMaterialInstances();

                  // Step 4: Reinitialize wall painting components
                  ReinitializeComponents();

                  Debug.Log("=== Wall Painting System Fix Complete ===");
                  EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            }

            private static void RemoveMissingScripts()
            {
                  Debug.Log("Removing missing script references...");
                  GameObject[] allObjects = Object.FindObjectsOfType<GameObject>();
                  int removedCount = 0;

                  foreach (GameObject obj in allObjects)
                  {
                        int count = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(obj);
                        if (count > 0)
                        {
                              removedCount += count;
                              Debug.Log($"Removed {count} missing scripts from {obj.name}");
                        }
                  }

                  Debug.Log($"Removed {removedCount} missing script references");
            }

            private static void UpdateMaterials()
            {
                  Debug.Log("Updating materials to use URP shader...");

                  // Get the URP shader
                  Shader urpShader = Shader.Find("Universal Render Pipeline/Lit");
                  if (urpShader == null)
                  {
                        Debug.LogError("Could not find URP shader!");
                        return;
                  }

                  // Update all materials in the scene
                  foreach (Material material in Resources.FindObjectsOfTypeAll<Material>())
                  {
                        if (material.shader != urpShader)
                        {
                              Debug.Log($"Updating shader for material: {material.name}");
                              material.shader = urpShader;
                              EditorUtility.SetDirty(material);
                        }
                  }

                  AssetDatabase.SaveAssets();
            }

            private static void FixMaterialInstances()
            {
                  Debug.Log("Fixing material instances...");

                  // Find all objects with Renderer component
                  foreach (Renderer renderer in Object.FindObjectsOfType<Renderer>())
                  {
                        if (renderer.gameObject.layer == LayerMask.NameToLayer("Wall"))
                        {
                              // Check for WallMaterialInstanceTracker
                              WallMaterialInstanceTracker tracker = renderer.GetComponent<WallMaterialInstanceTracker>();
                              if (tracker == null)
                              {
                                    tracker = renderer.gameObject.AddComponent<WallMaterialInstanceTracker>();
                              }

                              // Store original material if not already stored
                              if (tracker.OriginalSharedMaterial == null)
                              {
                                    tracker.OriginalSharedMaterial = renderer.sharedMaterial;
                              }

                              // Create unique material instance if needed
                              if (!renderer.sharedMaterial.name.Contains("_Instance_"))
                              {
                                    Material instanceMaterial = new Material(renderer.sharedMaterial);
                                    instanceMaterial.name = $"{renderer.sharedMaterial.name}_Instance_{renderer.gameObject.name}";
                                    renderer.sharedMaterial = instanceMaterial;
                                    tracker.instancedMaterial = instanceMaterial;
                                    Debug.Log($"Created unique material instance for {renderer.gameObject.name}");
                              }

                              EditorUtility.SetDirty(renderer.gameObject);
                        }
                  }
            }

            private static void ReinitializeComponents()
            {
                  Debug.Log("Reinitializing wall painting components...");

                  // Find WallPaintingManager
                  WallPaintingManager manager = Object.FindObjectOfType<WallPaintingManager>();
                  if (manager != null)
                  {
                        // Force reinitialize
                        System.Type managerType = manager.GetType();
                        var initMethod = managerType.GetMethod("InitializeComponents",
                            System.Reflection.BindingFlags.NonPublic |
                            System.Reflection.BindingFlags.Instance);

                        if (initMethod != null)
                        {
                              initMethod.Invoke(manager, null);
                              Debug.Log("Reinitialized WallPaintingManager");
                        }
                  }

                  // Find WallPainter
                  WallPainter painter = Object.FindObjectOfType<WallPainter>();
                  if (painter != null)
                  {
                        // Force reinitialize
                        System.Type painterType = painter.GetType();
                        var initMethod = painterType.GetMethod("Initialize",
                            System.Reflection.BindingFlags.NonPublic |
                            System.Reflection.BindingFlags.Instance);

                        if (initMethod != null)
                        {
                              initMethod.Invoke(painter, null);
                              Debug.Log("Reinitialized WallPainter");
                        }
                  }
            }
      }
}