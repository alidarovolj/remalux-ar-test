using UnityEngine;
using System.Collections.Generic;

namespace Remalux.AR.Vision
{
    /// <summary>
    /// Компонент для создания 3D-моделей стен на основе обнаруженных данных
    /// </summary>
    public class WallMeshBuilder : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] public WallDetector wallDetector;
        [SerializeField] public Camera mainCamera;
        [SerializeField] public Material defaultWallMaterial;

        [Header("Wall Settings")]
        [SerializeField] private float wallDistance = 2.0f; // Расстояние от камеры до стены
        [SerializeField] private float wallDepth = 0.05f; // Толщина стены
        [SerializeField] private float wallExtensionFactor = 1.2f; // Коэффициент расширения стены

        // Список созданных стен
        private List<GameObject> createdWalls = new List<GameObject>();
        private Dictionary<string, GameObject> wallObjects = new Dictionary<string, GameObject>();

        private void Start()
        {
            if (mainCamera == null)
                mainCamera = Camera.main;

            if (wallDetector == null)
                wallDetector = GetComponent<WallDetector>();

            if (wallDetector != null)
                wallDetector.OnWallsDetected += OnWallsDetected;
            else
                Debug.LogError("WallMeshBuilder: WallDetector component not found!");

            if (defaultWallMaterial == null)
                defaultWallMaterial = new Material(Shader.Find("Standard"));
        }

        private void OnDestroy()
        {
            if (wallDetector != null)
                wallDetector.OnWallsDetected -= OnWallsDetected;
        }

        private void OnWallsDetected(List<WallDetector.WallData> walls)
        {
            UpdateWallMeshes(walls);
        }

        private void UpdateWallMeshes(List<WallDetector.WallData> walls)
        {
            // Mark all existing walls as unused
            HashSet<string> usedWallIds = new HashSet<string>();

            // Update or create walls
            for (int i = 0; i < walls.Count; i++)
            {
                WallDetector.WallData wallData = walls[i];
                
                // Используем id из структуры WallData
                string wallId = wallData.id;
                usedWallIds.Add(wallId);

                if (wallObjects.ContainsKey(wallId))
                {
                    // Update existing wall
                    UpdateWallMesh(wallObjects[wallId], wallData);
                }
                else
                {
                    // Create new wall
                    GameObject wallObject = CreateWallMesh(wallData, $"Wall_{wallId}");
                    wallObjects[wallId] = wallObject;
                    createdWalls.Add(wallObject);
                }
            }

            // Remove unused walls
            List<string> wallsToRemove = new List<string>();
            foreach (var kvp in wallObjects)
            {
                if (!usedWallIds.Contains(kvp.Key))
                {
                    wallsToRemove.Add(kvp.Key);
                    createdWalls.Remove(kvp.Value);
                    Destroy(kvp.Value);
                }
            }

            foreach (string id in wallsToRemove)
            {
                wallObjects.Remove(id);
            }
        }

        private GameObject CreateWallMesh(WallDetector.WallData wallData, string name)
        {
            GameObject wallObject = new GameObject(name);
            wallObject.transform.parent = transform;

            // Add components
            MeshFilter meshFilter = wallObject.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = wallObject.AddComponent<MeshRenderer>();
            BoxCollider boxCollider = wallObject.AddComponent<BoxCollider>();
            WallMaterialInstanceTracker materialTracker = wallObject.AddComponent<WallMaterialInstanceTracker>();

            // Set material
            meshRenderer.material = defaultWallMaterial;
            materialTracker.OriginalSharedMaterial = defaultWallMaterial;

            // Create mesh
            Mesh mesh = CreateWallMeshGeometry(wallData);
            meshFilter.mesh = mesh;

            // Position the wall
            Vector3 worldPos = GetWorldPosition(wallData);
            wallObject.transform.position = worldPos;

            // Look at camera
            wallObject.transform.LookAt(mainCamera.transform);
            wallObject.transform.Rotate(Vector3.up, 180f); // Flip to face camera

            // Set collider size
            Vector2 worldSize = GetWorldSize(wallData);
            boxCollider.size = new Vector3(worldSize.x * wallExtensionFactor, worldSize.y * wallExtensionFactor, wallDepth);

            return wallObject;
        }

        private void UpdateWallMesh(GameObject wallObject, WallDetector.WallData wallData)
        {
            // Update position
            Vector3 worldPos = GetWorldPosition(wallData);
            wallObject.transform.position = worldPos;

            // Update rotation to face camera
            wallObject.transform.LookAt(mainCamera.transform);
            wallObject.transform.Rotate(Vector3.up, 180f); // Flip to face camera

            // Update collider size
            Vector2 worldSize = GetWorldSize(wallData);
            BoxCollider boxCollider = wallObject.GetComponent<BoxCollider>();
            if (boxCollider != null)
            {
                boxCollider.size = new Vector3(worldSize.x * wallExtensionFactor, worldSize.y * wallExtensionFactor, wallDepth);
            }

            // Update mesh
            MeshFilter meshFilter = wallObject.GetComponent<MeshFilter>();
            if (meshFilter != null)
            {
                Mesh mesh = CreateWallMeshGeometry(wallData);
                meshFilter.mesh = mesh;
            }
        }

        private Mesh CreateWallMeshGeometry(WallDetector.WallData wallData)
        {
            Mesh mesh = new Mesh();

            // Get world size
            Vector2 worldSize = GetWorldSize(wallData);
            float width = worldSize.x * wallExtensionFactor;
            float height = worldSize.y * wallExtensionFactor;

            // Create vertices
            Vector3[] vertices = new Vector3[8];
            
            // Front face (facing camera)
            vertices[0] = new Vector3(-width / 2, -height / 2, 0);
            vertices[1] = new Vector3(width / 2, -height / 2, 0);
            vertices[2] = new Vector3(width / 2, height / 2, 0);
            vertices[3] = new Vector3(-width / 2, height / 2, 0);
            
            // Back face
            vertices[4] = new Vector3(-width / 2, -height / 2, -wallDepth);
            vertices[5] = new Vector3(width / 2, -height / 2, -wallDepth);
            vertices[6] = new Vector3(width / 2, height / 2, -wallDepth);
            vertices[7] = new Vector3(-width / 2, height / 2, -wallDepth);

            // Create triangles
            int[] triangles = new int[36];
            
            // Front face
            triangles[0] = 0; triangles[1] = 2; triangles[2] = 1;
            triangles[3] = 0; triangles[4] = 3; triangles[5] = 2;
            
            // Back face
            triangles[6] = 5; triangles[7] = 6; triangles[8] = 4;
            triangles[9] = 4; triangles[10] = 6; triangles[11] = 7;
            
            // Top face
            triangles[12] = 3; triangles[13] = 7; triangles[14] = 6;
            triangles[15] = 3; triangles[16] = 6; triangles[17] = 2;
            
            // Bottom face
            triangles[18] = 0; triangles[19] = 1; triangles[20] = 5;
            triangles[21] = 0; triangles[22] = 5; triangles[23] = 4;
            
            // Left face
            triangles[24] = 0; triangles[25] = 4; triangles[26] = 7;
            triangles[27] = 0; triangles[28] = 7; triangles[29] = 3;
            
            // Right face
            triangles[30] = 1; triangles[31] = 2; triangles[32] = 6;
            triangles[33] = 1; triangles[34] = 6; triangles[35] = 5;

            // Create UVs
            Vector2[] uvs = new Vector2[8];
            uvs[0] = new Vector2(0, 0);
            uvs[1] = new Vector2(1, 0);
            uvs[2] = new Vector2(1, 1);
            uvs[3] = new Vector2(0, 1);
            uvs[4] = new Vector2(0, 0);
            uvs[5] = new Vector2(1, 0);
            uvs[6] = new Vector2(1, 1);
            uvs[7] = new Vector2(0, 1);

            // Assign to mesh
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.uv = uvs;
            
            // Recalculate normals and bounds
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return mesh;
        }

        /// <summary>
        /// Получает позицию стены в мировых координатах
        /// </summary>
        private Vector3 GetWorldPosition(WallDetector.WallData wallData)
        {
            // Вычисляем центр стены
            Vector2 center = new Vector2(
                (wallData.topLeft.x + wallData.topRight.x + wallData.bottomLeft.x + wallData.bottomRight.x) / 4,
                (wallData.topLeft.y + wallData.topRight.y + wallData.bottomLeft.y + wallData.bottomRight.y) / 4
            );
            
            // Преобразуем в мировые координаты
            Vector3 screenPos = new Vector3(center.x, center.y, wallDistance);
            return mainCamera.ScreenToWorldPoint(screenPos);
        }

        /// <summary>
        /// Получает размер стены в мировых координатах
        /// </summary>
        private Vector2 GetWorldSize(WallDetector.WallData wallData)
        {
            // Вычисляем ширину и высоту стены
            float width = wallData.width;
            float height = wallData.height;
            
            // Преобразуем в мировые координаты
            Vector3 bottomLeft = mainCamera.ScreenToWorldPoint(new Vector3(0, 0, wallDistance));
            Vector3 topRight = mainCamera.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height, wallDistance));
            
            float worldWidth = (width / Screen.width) * Vector3.Distance(bottomLeft, new Vector3(topRight.x, bottomLeft.y, bottomLeft.z));
            float worldHeight = (height / Screen.height) * Vector3.Distance(bottomLeft, new Vector3(bottomLeft.x, topRight.y, bottomLeft.z));
            
            return new Vector2(worldWidth, worldHeight);
        }

        /// <summary>
        /// Сбросить все стены к исходному материалу
        /// </summary>
        public void ResetAllWalls()
        {
            foreach (GameObject wall in createdWalls)
            {
                WallMaterialInstanceTracker tracker = wall.GetComponent<WallMaterialInstanceTracker>();
                if (tracker != null)
                {
                    tracker.ResetToOriginal();
                }
            }
        }

        /// <summary>
        /// Получить список всех созданных стен
        /// </summary>
        public List<GameObject> GetWalls()
        {
            return createdWalls;
        }

        /// <summary>
        /// Устанавливает материал по умолчанию для стен
        /// </summary>
        public void SetDefaultWallMaterial(Material material)
        {
            defaultWallMaterial = material;
        }

        /// <summary>
        /// Устанавливает основную камеру
        /// </summary>
        public void SetMainCamera(Camera camera)
        {
            mainCamera = camera;
        }

        /// <summary>
        /// Устанавливает детектор стен
        /// </summary>
        public void SetWallDetector(WallDetector detector)
        {
            if (wallDetector != null)
                wallDetector.OnWallsDetected -= OnWallsDetected;
                
            wallDetector = detector;
            
            if (wallDetector != null)
                wallDetector.OnWallsDetected += OnWallsDetected;
        }
    }
} 