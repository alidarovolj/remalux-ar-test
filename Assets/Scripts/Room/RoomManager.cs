using UnityEngine;
using System.Collections.Generic;

namespace Remalux.WallPainting
{
      public class RoomManager : MonoBehaviour
      {
            [Header("Wall Settings")]
            [SerializeField] private Material defaultWallMaterial;
            [SerializeField] private float wallHeight = 3f;
            [SerializeField] private float wallThickness = 0.2f;

            private List<GameObject> walls = new List<GameObject>();
            private Dictionary<GameObject, Material> wallMaterials = new Dictionary<GameObject, Material>();

            public void CreateWall(Vector3 start, Vector3 end)
            {
                  Vector3 direction = end - start;
                  float length = direction.magnitude;
                  Vector3 center = (start + end) / 2f;

                  GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                  wall.name = $"Wall_{walls.Count}";
                  wall.transform.parent = transform;

                  // Устанавливаем размеры стены
                  wall.transform.localScale = new Vector3(length, wallHeight, wallThickness);

                  // Устанавливаем позицию
                  wall.transform.position = new Vector3(center.x, wallHeight / 2f, center.z);

                  // Поворачиваем стену
                  float angle = Mathf.Atan2(direction.z, direction.x) * Mathf.Rad2Deg;
                  wall.transform.rotation = Quaternion.Euler(0, angle, 0);

                  // Применяем материал
                  var renderer = wall.GetComponent<Renderer>();
                  if (defaultWallMaterial != null)
                  {
                        Material wallMaterial = new Material(defaultWallMaterial);
                        renderer.material = wallMaterial;
                        wallMaterials[wall] = wallMaterial;
                  }

                  walls.Add(wall);
            }

            public void SetDefaultMaterial(Material material)
            {
                  defaultWallMaterial = material;
                  foreach (var wall in walls)
                  {
                        if (wall != null)
                        {
                              Material wallMaterial;
                              if (!wallMaterials.TryGetValue(wall, out wallMaterial))
                              {
                                    wallMaterial = new Material(material);
                                    wallMaterials[wall] = wallMaterial;
                              }
                              else
                              {
                                    wallMaterial.CopyPropertiesFromMaterial(material);
                              }

                              var renderer = wall.GetComponent<Renderer>();
                              if (renderer != null)
                              {
                                    renderer.material = wallMaterial;
                              }
                        }
                  }
            }

            public void ClearWalls()
            {
                  foreach (var wall in walls)
                  {
                        if (wall != null)
                        {
                              if (Application.isPlaying)
                                    Destroy(wall);
                              else
                                    DestroyImmediate(wall);
                        }
                  }

                  walls.Clear();
                  wallMaterials.Clear();
            }

            public void CreateRoomFromPoints(Vector3[] points)
            {
                  if (points == null || points.Length < 3)
                  {
                        Debug.LogError("RoomManager: Недостаточно точек для создания комнаты!");
                        return;
                  }

                  ClearWalls();

                  for (int i = 0; i < points.Length; i++)
                  {
                        Vector3 start = points[i];
                        Vector3 end = points[(i + 1) % points.Length];
                        CreateWall(start, end);
                  }
            }

            public void CreateDefaultRoom()
            {
                  Vector3[] points = new Vector3[]
                  {
                new Vector3(-5, 0, -5),
                new Vector3(5, 0, -5),
                new Vector3(5, 0, 5),
                new Vector3(-5, 0, 5)
                  };

                  CreateRoomFromPoints(points);
            }

            public void CreateRoom(Vector3[] points)
            {
                  if (points == null || points.Length < 3)
                  {
                        Debug.LogError("RoomManager: Недостаточно точек для создания комнаты!");
                        return;
                  }

                  ClearWalls();
                  CreateRoomFromPoints(points);
            }

            public void CreateRoom()
            {
                  CreateDefaultRoom();
            }

            public void ApplyMaterialToWall(GameObject wall, Material material)
            {
                  if (wall == null || material == null) return;

                  var renderer = wall.GetComponent<Renderer>();
                  if (renderer != null)
                  {
                        Material wallMaterial;
                        if (!wallMaterials.TryGetValue(wall, out wallMaterial))
                        {
                              wallMaterial = new Material(material);
                              wallMaterials[wall] = wallMaterial;
                        }
                        else
                        {
                              wallMaterial.CopyPropertiesFromMaterial(material);
                        }
                        renderer.material = wallMaterial;
                  }
            }

            private void OnValidate()
            {
                  // Обновляем высоту и толщину стен при изменении в инспекторе
                  foreach (var wall in walls)
                  {
                        if (wall != null)
                        {
                              Vector3 scale = wall.transform.localScale;
                              wall.transform.localScale = new Vector3(scale.x, wallHeight, wallThickness);
                              wall.transform.position = new Vector3(
                                  wall.transform.position.x,
                                  wallHeight / 2f,
                                  wall.transform.position.z
                              );
                        }
                  }
            }
      }
}