#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;

namespace Remalux.AR
{
      public class WallDetector : EditorWindow
      {
            [MenuItem("Tools/Wall Painting/Fix/Setup Real Walls")]
            public static void SetupRealWalls()
            {
                  Debug.Log("Setting up real walls...");

                  // Создаем слой Wall если его нет
                  int wallLayer = LayerMask.NameToLayer("Wall");
                  if (wallLayer == -1)
                  {
                        Debug.LogError("Layer 'Wall' does not exist! Please create it first.");
                        return;
                  }

                  var allObjects = Object.FindObjectsOfType<GameObject>();
                  int wallCount = 0;
                  List<GameObject> wallsToSetup = new List<GameObject>();

                  // Сначала находим все потенциальные стены
                  foreach (var obj in allObjects)
                  {
                        if (IsWallLike(obj))
                        {
                              wallsToSetup.Add(obj);
                              wallCount++;
                        }
                  }

                  if (wallCount == 0)
                  {
                        // Если стен нет, создаем базовые стены
                        CreateBaseWalls();
                        return;
                  }

                  // Настраиваем найденные стены
                  foreach (var wall in wallsToSetup)
                  {
                        SetupWall(wall);
                  }

                  Debug.Log($"Setup complete: {wallCount} walls configured");
                  EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            }

            private static bool IsWallLike(GameObject obj)
            {
                  if (obj == null) return false;

                  MeshFilter meshFilter = obj.GetComponent<MeshFilter>();
                  if (meshFilter == null || meshFilter.sharedMesh == null) return false;

                  // Проверяем геометрию объекта
                  Mesh mesh = meshFilter.sharedMesh;

                  // Считаем объект стеной, если:
                  // 1. Это вертикальная плоскость (один из размеров намного меньше других)
                  // 2. Имеет название, указывающее на стену
                  // 3. Имеет правильную ориентацию для стены

                  Bounds bounds = mesh.bounds;
                  float minDimension = Mathf.Min(bounds.size.x, Mathf.Min(bounds.size.y, bounds.size.z));
                  float maxDimension = Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));

                  bool isPlanelike = minDimension / maxDimension < 0.1f; // Один размер намного меньше других
                  bool hasWallName = obj.name.ToLower().Contains("wall") ||
                                   obj.name.ToLower().Contains("plane") ||
                                   obj.name.ToLower().Contains("surface");

                  // Проверяем ориентацию - стены обычно вертикальные
                  bool isVertical = Mathf.Abs(Vector3.Dot(obj.transform.up, Vector3.up)) < 0.1f;

                  return (isPlanelike || hasWallName) && isVertical;
            }

            private static void SetupWall(GameObject wall)
            {
                  // Устанавливаем слой
                  wall.layer = LayerMask.NameToLayer("Wall");

                  // Проверяем и добавляем необходимые компоненты
                  if (!wall.GetComponent<MeshCollider>())
                  {
                        MeshCollider collider = wall.AddComponent<MeshCollider>();
                        collider.convex = false;
                        Debug.Log($"Added MeshCollider to {wall.name}");
                  }

                  var renderer = wall.GetComponent<MeshRenderer>();
                  if (renderer == null)
                  {
                        Debug.LogError($"Wall {wall.name} has no MeshRenderer!");
                        return;
                  }

                  // Проверяем WallMaterialInstanceTracker
                  var tracker = wall.GetComponent<WallMaterialInstanceTracker>();
                  if (tracker == null)
                  {
                        tracker = wall.AddComponent<WallMaterialInstanceTracker>();
                        Debug.Log($"Added WallMaterialInstanceTracker to {wall.name}");
                  }

                  // Настраиваем материал
                  if (renderer.sharedMaterial != null)
                  {
                        if (!renderer.sharedMaterial.name.Contains("_Instance_"))
                        {
                              Material instanceMaterial = new Material(renderer.sharedMaterial);
                              instanceMaterial.name = $"{renderer.sharedMaterial.name}_Instance_{wall.name}";
                              renderer.sharedMaterial = instanceMaterial;

                              if (tracker != null)
                              {
                                    tracker.OriginalSharedMaterial = instanceMaterial;
                              }

                              Debug.Log($"Created unique material instance for {wall.name}");
                        }
                  }
                  else
                  {
                        // Создаем базовый материал для стены
                        var defaultMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                        defaultMaterial.name = $"DefaultWall_Instance_{wall.name}";
                        defaultMaterial.color = Color.white;
                        renderer.sharedMaterial = defaultMaterial;

                        if (tracker != null)
                        {
                              tracker.OriginalSharedMaterial = defaultMaterial;
                        }

                        Debug.Log($"Created default material for {wall.name}");
                  }
            }

            private static void CreateBaseWalls()
            {
                  Debug.Log("Creating base walls...");

                  // Создаем родительский объект для стен
                  GameObject wallsParent = new GameObject("Walls");

                  // Создаем четыре стены вокруг сцены
                  CreateWall("Wall_North", wallsParent.transform, new Vector3(0, 2.5f, 5), new Vector3(90, 0, 0), new Vector3(10, 0.1f, 5));
                  CreateWall("Wall_South", wallsParent.transform, new Vector3(0, 2.5f, -5), new Vector3(90, 180, 0), new Vector3(10, 0.1f, 5));
                  CreateWall("Wall_East", wallsParent.transform, new Vector3(5, 2.5f, 0), new Vector3(90, 90, 0), new Vector3(10, 0.1f, 5));
                  CreateWall("Wall_West", wallsParent.transform, new Vector3(-5, 2.5f, 0), new Vector3(90, 270, 0), new Vector3(10, 0.1f, 5));

                  Debug.Log("Created base walls");
                  EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            }

            private static void CreateWall(string name, Transform parent, Vector3 position, Vector3 rotation, Vector3 scale)
            {
                  GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Plane);
                  wall.name = name;
                  wall.transform.parent = parent;
                  wall.transform.position = position;
                  wall.transform.eulerAngles = rotation;
                  wall.transform.localScale = scale;

                  SetupWall(wall);
            }
      }
}
#endif