using UnityEngine;

namespace Remalux.AR
{
    public class MeshBuilder : MonoBehaviour
    {
        private float wallHeight;
        private Material wallMaterial;

        public void Initialize(float height, Material material)
        {
            this.wallHeight = height;
            this.wallMaterial = material;
        }

        public GameObject CreateWallMesh(WallSegment wall)
        {
            GameObject wallObject = new GameObject($"Wall_{wall.id}");
            wallObject.transform.position = wall.worldCenter;
            
            // Создаем базовый меш для стены
            MeshFilter meshFilter = wallObject.AddComponent<MeshFilter>();
            MeshRenderer renderer = wallObject.AddComponent<MeshRenderer>();
            
            // Создаем простой прямоугольный меш
            Mesh mesh = new Mesh();
            float halfLength = Vector3.Distance(wall.worldStart, wall.worldEnd) * 0.5f;
            
            Vector3[] vertices = new Vector3[]
            {
                new Vector3(-halfLength, 0, 0),
                new Vector3(halfLength, 0, 0),
                new Vector3(halfLength, wallHeight, 0),
                new Vector3(-halfLength, wallHeight, 0)
            };
            
            int[] triangles = new int[] { 0, 1, 2, 0, 2, 3 };
            Vector2[] uvs = new Vector2[]
            {
                new Vector2(0, 0),
                new Vector2(1, 0),
                new Vector2(1, 1),
                new Vector2(0, 1)
            };
            
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.uv = uvs;
            mesh.RecalculateNormals();
            
            meshFilter.mesh = mesh;
            renderer.material = wallMaterial;
            
            // Поворачиваем стену в соответствии с её нормалью
            wallObject.transform.forward = wall.normal;
            
            return wallObject;
        }

        public void UpdateWallMesh(GameObject wallObject, WallSegment newWall)
        {
            wallObject.transform.position = newWall.worldCenter;
            wallObject.transform.forward = newWall.normal;
            
            // Обновляем размеры меша если необходимо
            float newLength = Vector3.Distance(newWall.worldStart, newWall.worldEnd);
            wallObject.transform.localScale = new Vector3(newLength, 1, 1);
        }
    }
} 