using UnityEngine;
using System.Collections.Generic;

namespace Remalux.AR
{
      public class MeshOptimizer : MonoBehaviour
      {
            [Header("Настройки оптимизации")]
            [SerializeField] private bool optimizeOnStart = true;
            [SerializeField] private int maxPolygons = 256;
            [SerializeField] private bool recalculateNormals = true;
            [SerializeField] private bool recalculateBounds = true;
            [SerializeField] private bool optimizeCollider = true;

            private Mesh originalMesh;
            private Mesh optimizedMesh;

            private void Start()
            {
                  if (optimizeOnStart)
                  {
                        OptimizeMesh();
                  }
            }

            public void OptimizeMesh()
            {
                  MeshFilter meshFilter = GetComponent<MeshFilter>();
                  MeshCollider meshCollider = GetComponent<MeshCollider>();

                  if (meshFilter == null)
                  {
                        Debug.LogError("MeshFilter not found on " + gameObject.name);
                        return;
                  }

                  originalMesh = meshFilter.sharedMesh;
                  if (originalMesh == null)
                  {
                        Debug.LogError("No mesh found on " + gameObject.name);
                        return;
                  }

                  // Create optimized mesh
                  optimizedMesh = new Mesh();
                  optimizedMesh.name = originalMesh.name + "_Optimized";

                  // Copy mesh data
                  CopyMeshData(originalMesh, optimizedMesh);

                  // Simplify mesh if needed
                  if (originalMesh.triangles.Length / 3 > maxPolygons)
                  {
                        SimplifyMesh(optimizedMesh);
                  }

                  // Apply optimizations
                  if (recalculateNormals)
                        optimizedMesh.RecalculateNormals();
                  if (recalculateBounds)
                        optimizedMesh.RecalculateBounds();

                  // Apply optimized mesh
                  meshFilter.sharedMesh = optimizedMesh;

                  // Update collider if needed
                  if (optimizeCollider && meshCollider != null)
                  {
                        meshCollider.sharedMesh = optimizedMesh;
                  }

                  Debug.Log($"Mesh optimized for {gameObject.name}. Original triangles: {originalMesh.triangles.Length / 3}, Optimized triangles: {optimizedMesh.triangles.Length / 3}");
            }

            private void CopyMeshData(Mesh source, Mesh destination)
            {
                  destination.vertices = source.vertices;
                  destination.triangles = source.triangles;
                  destination.normals = source.normals;
                  destination.tangents = source.tangents;
                  destination.uv = source.uv;
                  destination.uv2 = source.uv2;
                  destination.uv3 = source.uv3;
                  destination.uv4 = source.uv4;
                  destination.colors = source.colors;
                  destination.bindposes = source.bindposes;
                  destination.boneWeights = source.boneWeights;
            }

            private void SimplifyMesh(Mesh mesh)
            {
                  // For cylinder meshes, we'll use a different optimization approach
                  if (IsCylinderMesh(mesh))
                  {
                        OptimizeCylinderMesh(mesh);
                  }
                  else
                  {
                        OptimizeGeneralMesh(mesh);
                  }
            }

            private bool IsCylinderMesh(Mesh mesh)
            {
                  // Check if the mesh is likely a cylinder by analyzing its vertices
                  Vector3[] vertices = mesh.vertices;
                  if (vertices.Length < 3) return false;

                  // Check if vertices form circular patterns
                  Vector3 center = Vector3.zero;
                  foreach (Vector3 vertex in vertices)
                  {
                        center += vertex;
                  }
                  center /= vertices.Length;

                  float radius = Vector3.Distance(vertices[0], center);
                  int circularPatterns = 0;

                  for (int i = 0; i < vertices.Length; i++)
                  {
                        float currentRadius = Vector3.Distance(vertices[i], center);
                        if (Mathf.Abs(currentRadius - radius) < 0.01f)
                        {
                              circularPatterns++;
                        }
                  }

                  return circularPatterns > vertices.Length * 0.8f;
            }

            private void OptimizeCylinderMesh(Mesh mesh)
            {
                  Vector3[] vertices = mesh.vertices;
                  int[] triangles = mesh.triangles;
                  List<Vector3> newVertices = new List<Vector3>();
                  List<int> newTriangles = new List<int>();

                  // Find the center and axis of the cylinder
                  Vector3 center = Vector3.zero;
                  foreach (Vector3 vertex in vertices)
                  {
                        center += vertex;
                  }
                  center /= vertices.Length;

                  // Group vertices by height
                  Dictionary<float, List<Vector3>> heightGroups = new Dictionary<float, List<Vector3>>();
                  foreach (Vector3 vertex in vertices)
                  {
                        float height = vertex.y;
                        if (!heightGroups.ContainsKey(height))
                        {
                              heightGroups[height] = new List<Vector3>();
                        }
                        heightGroups[height].Add(vertex);
                  }

                  // Create optimized vertices
                  foreach (var group in heightGroups)
                  {
                        List<Vector3> levelVertices = group.Value;
                        int vertexCount = Mathf.Min(8, levelVertices.Count); // Use 8 vertices per level for cylinders

                        for (int i = 0; i < vertexCount; i++)
                        {
                              float angle = (2 * Mathf.PI * i) / vertexCount;
                              Vector3 newVertex = center + new Vector3(
                                    Mathf.Cos(angle) * Vector3.Distance(levelVertices[0], center),
                                    group.Key,
                                    Mathf.Sin(angle) * Vector3.Distance(levelVertices[0], center)
                              );
                              newVertices.Add(newVertex);
                        }
                  }

                  // Create optimized triangles
                  int levels = heightGroups.Count;
                  for (int level = 0; level < levels - 1; level++)
                  {
                        int baseIndex = level * 8;
                        int nextBaseIndex = (level + 1) * 8;

                        for (int i = 0; i < 8; i++)
                        {
                              int nextI = (i + 1) % 8;
                              newTriangles.Add(baseIndex + i);
                              newTriangles.Add(nextBaseIndex + i);
                              newTriangles.Add(baseIndex + nextI);

                              newTriangles.Add(nextBaseIndex + i);
                              newTriangles.Add(nextBaseIndex + nextI);
                              newTriangles.Add(baseIndex + nextI);
                        }
                  }

                  mesh.vertices = newVertices.ToArray();
                  mesh.triangles = newTriangles.ToArray();
            }

            private void OptimizeGeneralMesh(Mesh mesh)
            {
                  // Basic vertex reduction for non-cylinder meshes
                  Vector3[] vertices = mesh.vertices;
                  int[] triangles = mesh.triangles;
                  List<Vector3> newVertices = new List<Vector3>();
                  List<int> newTriangles = new List<int>();
                  Dictionary<Vector3, int> vertexMap = new Dictionary<Vector3, int>();

                  for (int i = 0; i < triangles.Length; i += 3)
                  {
                        for (int j = 0; j < 3; j++)
                        {
                              Vector3 vertex = vertices[triangles[i + j]];
                              if (!vertexMap.ContainsKey(vertex))
                              {
                                    vertexMap[vertex] = newVertices.Count;
                                    newVertices.Add(vertex);
                              }
                              newTriangles.Add(vertexMap[vertex]);
                        }
                  }

                  mesh.vertices = newVertices.ToArray();
                  mesh.triangles = newTriangles.ToArray();
            }

            private void OnDestroy()
            {
                  if (optimizedMesh != null)
                  {
                        MemoryManager.Instance.ReleaseMeshInstance(optimizedMesh);
                  }
            }
      }
}