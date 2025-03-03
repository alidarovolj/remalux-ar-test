using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace Remalux.AR
{
      public class WallPaintingFixer : EditorWindow
      {
            [MenuItem("Tools/Wall Painting/Fix Wall Painting Issues")]
            public static void ShowWindow()
            {
                  GetWindow<WallPaintingFixer>("Wall Painting Fixer");
            }

            private void OnGUI()
            {
                  GUILayout.Label("Wall Painting System Fixer", EditorStyles.boldLabel);

                  if (GUILayout.Button("Fix All Issues"))
                  {
                        FixAllIssues();
                  }

                  EditorGUILayout.Space();

                  if (GUILayout.Button("Fix Layer Settings"))
                  {
                        FixLayerSettings();
                  }

                  if (GUILayout.Button("Fix Wall Components"))
                  {
                        FixWallComponents();
                  }

                  if (GUILayout.Button("Fix Material Instances"))
                  {
                        FixMaterialInstances();
                  }
            }

            private void FixAllIssues()
            {
                  Debug.Log("=== Starting comprehensive wall painting system fix ===");

                  FixLayerSettings();
                  FixWallComponents();
                  FixMaterialInstances();

                  Debug.Log("=== Completed comprehensive wall painting system fix ===");
            }

            private void FixLayerSettings()
            {
                  Debug.Log("Fixing layer settings...");

                  // Ensure "Wall" layer exists (layer 8)
                  if (LayerMask.NameToLayer("Wall") == -1)
                  {
                        Debug.LogError("Layer 'Wall' does not exist! Please create it in Edit > Project Settings > Tags and Layers");
                        return;
                  }

                  // Fix WallPainter layer mask
                  var wallPainters = Object.FindObjectsOfType<MonoBehaviour>();
                  foreach (var component in wallPainters)
                  {
                        if (component.GetType().Name == "WallPainter")
                        {
                              var layerMaskField = component.GetType().GetField("wallLayerMask");
                              if (layerMaskField != null)
                              {
                                    LayerMask currentMask = (LayerMask)layerMaskField.GetValue(component);
                                    LayerMask newMask = 1 << 8; // Layer 8 ("Wall")
                                    if (currentMask.value != newMask.value)
                                    {
                                          layerMaskField.SetValue(component, newMask);
                                          EditorUtility.SetDirty(component);
                                          Debug.Log($"Fixed layer mask for WallPainter on {component.gameObject.name}");
                                    }
                              }
                        }
                  }
            }

            private void FixWallComponents()
            {
                  Debug.Log("WallPaintingFixer: Starting wall components fix...");

                  // Find all objects on the "Wall" layer
                  var wallObjects = new List<GameObject>();
                  var allObjects = Object.FindObjectsOfType<GameObject>();
                  foreach (var obj in allObjects)
                  {
                        if (obj.layer == 8) // "Wall" layer
                        {
                              wallObjects.Add(obj);
                        }
                  }

                  Debug.Log($"WallPaintingFixer: Found {wallObjects.Count} wall objects");

                  foreach (var wallObj in wallObjects)
                  {
                        // Ensure Renderer component exists
                        var renderer = wallObj.GetComponent<Renderer>();
                        if (renderer == null)
                        {
                              Debug.LogWarning($"WallPaintingFixer: Wall object {wallObj.name} is missing Renderer component!");
                              continue;
                        }

                        // Ensure WallMaterialInstanceTracker exists
                        var tracker = wallObj.GetComponent<WallMaterialInstanceTracker>();
                        if (tracker == null)
                        {
                              tracker = wallObj.AddComponent<WallMaterialInstanceTracker>();
                              Debug.Log($"WallPaintingFixer: Added WallMaterialInstanceTracker to {wallObj.name}");
                        }

                        // Save original material if not already saved
                        if (tracker.OriginalSharedMaterial == null)
                        {
                              // Use sharedMaterial to prevent material leaks
                              Material originalMaterial = renderer.sharedMaterial;
                              if (originalMaterial != null)
                              {
                                    // Create a unique instance for this wall
                                    Material instanceMaterial = new Material(originalMaterial);
                                    instanceMaterial.name = $"{originalMaterial.name}_Instance_{wallObj.name}";

                                    // Apply the instance material using sharedMaterial
                                    renderer.sharedMaterial = instanceMaterial;

                                    // Apply through tracker to ensure proper setup
                                    tracker.ApplyMaterial(originalMaterial);

                                    Debug.Log($"WallPaintingFixer: Created unique material instance for {wallObj.name}");
                              }
                        }

                        // Mark objects as dirty
                        UnityEditor.EditorUtility.SetDirty(wallObj);
                        UnityEditor.EditorUtility.SetDirty(renderer);
                        UnityEditor.EditorUtility.SetDirty(tracker);
                  }

                  // Save the scene
                  UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                        UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene()
                  );

                  Debug.Log("WallPaintingFixer: Completed wall components fix");
            }

            private void FixMaterialInstances()
            {
                  Debug.Log("Fixing material instances...");

                  var trackers = Object.FindObjectsOfType<WallMaterialInstanceTracker>();
                  foreach (var tracker in trackers)
                  {
                        var renderer = tracker.GetComponent<Renderer>();
                        if (renderer != null)
                        {
                              // Get the current material (use sharedMaterial to prevent leaks)
                              Material currentMaterial = renderer.sharedMaterial;

                              // Skip if the material is already an instance
                              if (currentMaterial != null && !currentMaterial.name.Contains("_Instance_"))
                              {
                                    // Save the original material if not already saved
                                    if (tracker.OriginalSharedMaterial == null)
                                    {
                                          tracker.OriginalSharedMaterial = currentMaterial;
                                          Debug.Log($"Saved original material for {tracker.gameObject.name}: {currentMaterial.name}");
                                    }

                                    // Create a new instance with a unique name
                                    Material instanceMaterial = new Material(currentMaterial);
                                    instanceMaterial.name = $"{currentMaterial.name}_Instance_{tracker.gameObject.name}";

                                    // Apply the instance material using sharedMaterial in edit mode
                                    renderer.sharedMaterial = instanceMaterial;
                                    Debug.Log($"Created and applied new material instance for {tracker.gameObject.name}");

                                    // Update the tracker's instanced material reference
                                    tracker.instancedMaterial = instanceMaterial;
                              }

                              // Mark objects as dirty to ensure changes are saved
                              EditorUtility.SetDirty(renderer);
                              EditorUtility.SetDirty(tracker);
                              EditorUtility.SetDirty(tracker.gameObject);
                        }
                  }

                  // Save the scene after making changes
                  UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            }
      }
}