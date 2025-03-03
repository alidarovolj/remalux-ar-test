using UnityEngine;
using System.Collections.Generic;

namespace Remalux.AR
{
      /// <summary>
      /// Компонент для отслеживания экземпляров материалов на стенах.
      /// Добавляется к объектам стен для управления материалами.
      /// </summary>
      public class WallMaterialInstanceTracker : MonoBehaviour
      {
            [SerializeField]
            private Material originalSharedMaterial;
            private Dictionary<Material, Material> materialInstances = new Dictionary<Material, Material>();
            private Renderer wallRenderer;
            private bool initialized = false;

            // Public property for getting and setting the original material
            public Material OriginalSharedMaterial
            {
                  get => originalSharedMaterial;
                  set
                  {
                        originalSharedMaterial = value;
#if UNITY_EDITOR
                        if (!Application.isPlaying)
                        {
                              UnityEditor.EditorUtility.SetDirty(this);
                        }
#endif
                  }
            }

            // Property for editor compatibility
            public Material instancedMaterial
            {
                  get
                  {
                        if (wallRenderer != null)
                        {
                              return wallRenderer.sharedMaterial;
                        }
                        return null;
                  }
                  set
                  {
                        if (wallRenderer != null && value != null)
                        {
                              // Add material to dictionary if not already present
                              if (!materialInstances.ContainsKey(value))
                              {
                                    Material instance = new Material(value);
                                    instance.CopyPropertiesFromMaterial(value);
                                    instance.name = $"{value.name}_Instance_{gameObject.name}";
                                    materialInstances[value] = instance;
                              }
                              wallRenderer.sharedMaterial = materialInstances[value];
#if UNITY_EDITOR
                              if (!Application.isPlaying)
                              {
                                    UnityEditor.EditorUtility.SetDirty(wallRenderer);
                                    UnityEditor.EditorUtility.SetDirty(this);
                              }
#endif
                        }
                  }
            }

            private void Awake()
            {
                  Initialize();
            }

            private void OnEnable()
            {
                  if (!initialized)
                  {
                        Initialize();
                  }
            }

            private void Initialize()
            {
                  if (initialized) return;

                  wallRenderer = GetComponent<Renderer>();
                  if (wallRenderer != null)
                  {
                        if (originalSharedMaterial == null)
                        {
                              OriginalSharedMaterial = wallRenderer.sharedMaterial;
                              Debug.Log($"WallMaterialInstanceTracker: Saved original material for {gameObject.name}: {originalSharedMaterial?.name ?? "null"}");
                        }

                        // Create instance of current material if needed
                        Material currentMaterial = wallRenderer.sharedMaterial;
                        if (currentMaterial != null && !currentMaterial.name.Contains("_Instance_"))
                        {
                              Material instance = new Material(currentMaterial);
                              instance.CopyPropertiesFromMaterial(currentMaterial);
                              instance.name = $"{currentMaterial.name}_Instance_{gameObject.name}";
                              materialInstances[currentMaterial] = instance;
                              wallRenderer.sharedMaterial = instance;
                              Debug.Log($"WallMaterialInstanceTracker: Created instance of current material for {gameObject.name}");
                        }
                  }
                  else
                  {
                        Debug.LogError($"WallMaterialInstanceTracker: No Renderer found on {gameObject.name}");
                  }

                  initialized = true;
            }

            /// <summary>
            /// Применяет материал к объекту, создавая его экземпляр
            /// </summary>
            public void ApplyMaterial(Material newMaterial)
            {
                  if (!initialized)
                  {
                        Initialize();
                  }

                  if (wallRenderer == null)
                  {
                        Debug.LogError($"WallMaterialInstanceTracker: No Renderer found on {gameObject.name}");
                        return;
                  }

                  if (newMaterial == null)
                  {
                        Debug.LogError($"WallMaterialInstanceTracker: Attempted to apply null material to {gameObject.name}");
                        return;
                  }

                  try
                  {
                        // Check if we already have an instance for this material
                        Material instanceMaterial;
                        if (!materialInstances.TryGetValue(newMaterial, out instanceMaterial))
                        {
                              // Create new instance
                              instanceMaterial = new Material(newMaterial);
                              instanceMaterial.CopyPropertiesFromMaterial(newMaterial);
                              instanceMaterial.name = $"{newMaterial.name}_Instance_{gameObject.name}";
                              materialInstances[newMaterial] = instanceMaterial;
                              Debug.Log($"WallMaterialInstanceTracker: Created new material instance {instanceMaterial.name}");
                        }

                        // Apply the material
                        wallRenderer.sharedMaterial = instanceMaterial;
                        Debug.Log($"WallMaterialInstanceTracker: Applied material {instanceMaterial.name} to {gameObject.name}");

#if UNITY_EDITOR
                        if (!Application.isPlaying)
                        {
                              UnityEditor.EditorUtility.SetDirty(gameObject);
                              UnityEditor.EditorUtility.SetDirty(wallRenderer);
                              UnityEditor.EditorUtility.SetDirty(this);
                              UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                                    UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene()
                              );
                        }
#endif
                  }
                  catch (System.Exception e)
                  {
                        Debug.LogError($"WallMaterialInstanceTracker: Error applying material to {gameObject.name}: {e.Message}");
                  }
            }

            /// <summary>
            /// Updates the material instance for editor compatibility
            /// </summary>
            public void UpdateMaterialInstance()
            {
                  if (!initialized)
                  {
                        Initialize();
                  }

                  if (wallRenderer != null)
                  {
                        Material currentMaterial = wallRenderer.sharedMaterial;
                        if (currentMaterial != null && !currentMaterial.name.Contains("_Instance_"))
                        {
                              Material newInstance = new Material(currentMaterial);
                              newInstance.CopyPropertiesFromMaterial(currentMaterial);
                              newInstance.name = $"{currentMaterial.name}_Instance_{gameObject.name}";
                              materialInstances[currentMaterial] = newInstance;
                              wallRenderer.sharedMaterial = newInstance;
                              Debug.Log($"WallMaterialInstanceTracker: Updated material instance for {gameObject.name}");

#if UNITY_EDITOR
                              if (!Application.isPlaying)
                              {
                                    UnityEditor.EditorUtility.SetDirty(gameObject);
                                    UnityEditor.EditorUtility.SetDirty(wallRenderer);
                                    UnityEditor.EditorUtility.SetDirty(this);
                              }
#endif
                        }
                  }
            }

            /// <summary>
            /// Восстанавливает оригинальный материал
            /// </summary>
            public void ResetToOriginal()
            {
                  if (!initialized)
                  {
                        Initialize();
                  }

                  if (wallRenderer != null && originalSharedMaterial != null)
                  {
                        // Create instance of original material if needed
                        Material instanceMaterial;
                        if (!materialInstances.TryGetValue(originalSharedMaterial, out instanceMaterial))
                        {
                              instanceMaterial = new Material(originalSharedMaterial);
                              instanceMaterial.CopyPropertiesFromMaterial(originalSharedMaterial);
                              instanceMaterial.name = $"{originalSharedMaterial.name}_Instance_{gameObject.name}";
                              materialInstances[originalSharedMaterial] = instanceMaterial;
                        }

                        wallRenderer.sharedMaterial = instanceMaterial;
                        Debug.Log($"WallMaterialInstanceTracker: Reset to original material for {gameObject.name}");

#if UNITY_EDITOR
                        if (!Application.isPlaying)
                        {
                              UnityEditor.EditorUtility.SetDirty(gameObject);
                              UnityEditor.EditorUtility.SetDirty(wallRenderer);
                              UnityEditor.EditorUtility.SetDirty(this);
                        }
#endif
                  }
            }

            private void OnDestroy()
            {
                  // Clean up material instances
                  foreach (var material in materialInstances.Values)
                  {
                        if (material != null)
                        {
                              if (Application.isPlaying)
                              {
                                    Destroy(material);
                              }
                              else
                              {
                                    DestroyImmediate(material);
                              }
                        }
                  }
                  materialInstances.Clear();
            }

#if UNITY_EDITOR
            private void OnValidate()
            {
                  if (!initialized && !Application.isPlaying)
                  {
                        Initialize();
                  }
            }
#endif
      }
}