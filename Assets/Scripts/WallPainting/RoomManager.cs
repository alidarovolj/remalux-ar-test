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
                  GameObject wall = new GameObject("Wall");
                  wall.transform.SetParent(transform);

                  Vector3 direction = end - start;
                  float length = direction.magnitude;
                  direction.Normalize();

                  wall.transform.position = (start + end) / 2f;
                  wall.transform.rotation = Quaternion.LookRotation(direction);
                  wall.transform.localScale = new Vector3(wallThickness, wallHeight, length);

                  MeshFilter meshFilter = wall.AddComponent<MeshFilter>();
                  MeshRenderer meshRenderer = wall.AddComponent<MeshRenderer>();

                  Mesh mesh = new Mesh();
                  Vector3[] vertices = new Vector3[8];
                  int[] triangles = new int[36];

                  // Front face
                  vertices[0] = new Vector3(-wallThickness / 2, -wallHeight / 2, -length / 2);
                  vertices[1] = new Vector3(wallThickness / 2, -wallHeight / 2, -length / 2);
                  vertices[2] = new Vector3(wallThickness / 2, wallHeight / 2, -length / 2);
                  vertices[3] = new Vector3(-wallThickness / 2, wallHeight / 2, -length / 2);

                  // Back face
                  vertices[4] = new Vector3(-wallThickness / 2, -wallHeight / 2, length / 2);
                  vertices[5] = new Vector3(wallThickness / 2, -wallHeight / 2, length / 2);
                  vertices[6] = new Vector3(wallThickness / 2, wallHeight / 2, length / 2);
                  vertices[7] = new Vector3(-wallThickness / 2, wallHeight / 2, length / 2);

                  // Front face triangles
                  triangles[0] = 0; triangles[1] = 2; triangles[2] = 1;
                  triangles[3] = 0; triangles[4] = 3; triangles[5] = 2;

                  // Back face triangles
                  triangles[6] = 5; triangles[7] = 6; triangles[8] = 4;
                  triangles[9] = 4; triangles[10] = 6; triangles[11] = 7;

                  // Top face triangles
                  triangles[12] = 3; triangles[13] = 7; triangles[14] = 6;
                  triangles[15] = 3; triangles[16] = 6; triangles[17] = 2;

                  // Bottom face triangles
                  triangles[18] = 1; triangles[19] = 5; triangles[20] = 0;
                  triangles[21] = 0; triangles[22] = 5; triangles[23] = 4;

                  // Left face triangles
                  triangles[24] = 0; triangles[25] = 7; triangles[26] = 3;
                  triangles[27] = 0; triangles[28] = 4; triangles[29] = 7;

                  // Right face triangles
                  triangles[30] = 1; triangles[31] = 2; triangles[32] = 6;
                  triangles[33] = 1; triangles[34] = 6; triangles[35] = 5;

                  mesh.vertices = vertices;
                  mesh.triangles = triangles;
                  mesh.RecalculateNormals();

                  meshFilter.mesh = mesh;
                  meshRenderer.material = defaultWallMaterial;

                  walls.Add(wall);
                  wallMaterials[wall] = defaultWallMaterial;
            }

            public void SetDefaultMaterial(Material material)
            {
                  defaultWallMaterial = material;
                  foreach (GameObject wall in walls)
                  {
                        if (wallMaterials.ContainsKey(wall))
                        {
                              wallMaterials[wall] = material;
                              wall.GetComponent<MeshRenderer>().material = material;
                        }
                  }
            }

            public void ClearWalls()
            {
                  foreach (GameObject wall in walls)
                  {
                        Destroy(wall);
                  }
                  walls.Clear();
                  wallMaterials.Clear();
            }

            public void CreateRoomFromPoints(Vector3[] points)
            {
                  if (points == null || points.Length < 3)
                  {
                        Debug.LogError("Not enough points to create a room!");
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
                  CreateRoomFromPoints(points);
            }

            public void CreateRoom()
            {
                  CreateDefaultRoom();
            }

            public void ApplyMaterialToWall(GameObject wall, Material material)
            {
                  if (wall == null || material == null) return;

                  if (walls.Contains(wall))
                  {
                        wall.GetComponent<MeshRenderer>().material = material;
                        wallMaterials[wall] = material;
                  }
            }

            private void OnValidate()
            {
                  if (defaultWallMaterial == null)
                  {
                        defaultWallMaterial = new Material(Shader.Find("Standard"));
                        defaultWallMaterial.color = Color.white;
                  }
            }

            public void ApplyTextureToAllWalls(Material material)
            {
                  foreach (GameObject wall in walls)
                  {
                        ApplyMaterialToWall(wall, material);
                  }
            }

            public void UpdateWallColors(Color color)
            {
                  foreach (GameObject wall in walls)
                  {
                        if (wallMaterials.ContainsKey(wall))
                        {
                              Material material = wallMaterials[wall];
                              if (material != null)
                              {
                                    material.color = color;
                              }
                        }
                  }
            }
      }
}