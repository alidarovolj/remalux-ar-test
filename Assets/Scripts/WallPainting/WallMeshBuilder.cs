using UnityEngine;
using System.Collections.Generic;
using Remalux.WallPainting;

namespace Remalux.WallPainting
{
      /// <summary>
      /// Компонент для создания мешей стен на основе данных обнаружения
      /// </summary>
      public class WallMeshBuilder : MonoBehaviour
      {
            [Header("Wall Settings")]
            [SerializeField] private float wallHeight = 2.5f;
            [SerializeField] private float wallThickness = 0.1f;
            [SerializeField] private Material wallMaterial;

            [Header("References")]
            [SerializeField] private WallDetector wallDetector;

            private Dictionary<string, GameObject> wallObjects = new Dictionary<string, GameObject>();

            private void Start()
            {
                  if (wallDetector == null)
                  {
                        wallDetector = FindObjectOfType<WallDetector>();
                        if (wallDetector == null)
                        {
                              Debug.LogError("WallMeshBuilder: WallDetector not found!");
                              enabled = false;
                              return;
                        }
                  }

                  wallDetector.OnWallsDetected += OnWallsDetected;
            }

            private void OnWallsDetected(List<WallData> walls)
            {
                  // Удаляем стены, которых больше нет в списке
                  List<string> wallsToRemove = new List<string>();
                  foreach (var wallObj in wallObjects)
                  {
                        if (!walls.Exists(w => w.id == wallObj.Key))
                        {
                              wallsToRemove.Add(wallObj.Key);
                        }
                  }

                  foreach (var wallId in wallsToRemove)
                  {
                        if (wallObjects.TryGetValue(wallId, out GameObject wallObj))
                        {
                              Destroy(wallObj);
                              wallObjects.Remove(wallId);
                        }
                  }

                  // Обновляем или создаем новые стены
                  foreach (var wall in walls)
                  {
                        if (wallObjects.TryGetValue(wall.id, out GameObject existingWall))
                        {
                              UpdateWallMesh(existingWall, wall);
                        }
                        else
                        {
                              CreateWallMesh(wall);
                        }
                  }
            }

            private void CreateWallMesh(WallData wall)
            {
                  GameObject wallObj = new GameObject($"Wall_{wall.id}");
                  wallObj.transform.SetParent(transform);
                  wallObj.transform.localPosition = Vector3.zero;

                  MeshFilter meshFilter = wallObj.AddComponent<MeshFilter>();
                  MeshRenderer meshRenderer = wallObj.AddComponent<MeshRenderer>();
                  meshRenderer.material = wallMaterial;

                  UpdateWallMesh(wallObj, wall);
                  wallObjects.Add(wall.id, wallObj);
            }

            private void UpdateWallMesh(GameObject wallObj, WallData wall)
            {
                  MeshFilter meshFilter = wallObj.GetComponent<MeshFilter>();
                  if (meshFilter == null) return;

                  Mesh mesh = new Mesh();
                  mesh.name = $"WallMesh_{wall.id}";

                  // Создаем вершины для стены
                  Vector3[] vertices = new Vector3[]
                  {
                        wall.bottomLeft,
                        wall.bottomRight,
                        wall.topLeft,
                        wall.topRight
                  };

                  // Создаем треугольники
                  int[] triangles = new int[]
                  {
                        0, 1, 2,
                        1, 3, 2
                  };

                  // Создаем UV координаты
                  Vector2[] uvs = new Vector2[]
                  {
                        new Vector2(0, 0),
                        new Vector2(1, 0),
                        new Vector2(0, 1),
                        new Vector2(1, 1)
                  };

                  // Создаем нормали
                  Vector3 normal = Vector3.Cross(wall.topRight - wall.topLeft, wall.bottomLeft - wall.topLeft).normalized;
                  Vector3[] normals = new Vector3[]
                  {
                        normal,
                        normal,
                        normal,
                        normal
                  };

                  mesh.vertices = vertices;
                  mesh.triangles = triangles;
                  mesh.uv = uvs;
                  mesh.normals = normals;
                  mesh.RecalculateBounds();
                  mesh.RecalculateTangents();

                  meshFilter.mesh = mesh;
            }

            private void OnDestroy()
            {
                  if (wallDetector != null)
                  {
                        wallDetector.OnWallsDetected -= OnWallsDetected;
                  }
            }
      }
}