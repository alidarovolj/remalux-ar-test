using UnityEngine;
using UnityEditor;

namespace Remalux.AR
{
      public static class TestWallCreator
      {
            private const string WALL_TAG = "Wall";
            private const int WALL_LAYER = 8; // Слой "Wall" имеет индекс 8

            [MenuItem("Tools/Wall Painting/Create Test Wall")]
            public static void CreateTestWall()
            {
                  // Проверяем, настроен ли слой "Wall" в проекте
                  string layerName = LayerMask.LayerToName(WALL_LAYER);
                  if (string.IsNullOrEmpty(layerName) || layerName != "Wall")
                  {
                        Debug.LogError($"Слой с индексом {WALL_LAYER} не настроен как 'Wall'. Пожалуйста, настройте слои в Project Settings -> Tags and Layers.");
                        return;
                  }

                  // Создаем объект стены
                  GameObject wallObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                  wallObject.name = "TestWall";
                  wallObject.tag = WALL_TAG;
                  wallObject.layer = WALL_LAYER;

                  // Настраиваем размеры и положение стены
                  wallObject.transform.position = new Vector3(0, 0.5f, 2);
                  wallObject.transform.localScale = new Vector3(3, 1, 0.1f);

                  // Создаем материал для стены
                  Material wallMaterial = new Material(Shader.Find("Standard"));
                  wallMaterial.name = "WallMaterial";
                  wallMaterial.color = Color.white;

                  // Применяем материал к стене
                  MeshRenderer renderer = wallObject.GetComponent<MeshRenderer>();
                  if (renderer != null)
                  {
                        renderer.material = wallMaterial;
                  }

                  // Выбираем созданную стену
                  Selection.activeGameObject = wallObject;

                  Debug.Log("Создана тестовая стена. Тег: 'Wall', слой: 'Wall'");
            }

            [MenuItem("Tools/Wall Painting/Create Test Room")]
            public static void CreateTestRoom()
            {
                  // Проверяем, настроен ли слой "Wall" в проекте
                  string layerName = LayerMask.LayerToName(WALL_LAYER);
                  if (string.IsNullOrEmpty(layerName) || layerName != "Wall")
                  {
                        Debug.LogError($"Слой с индексом {WALL_LAYER} не настроен как 'Wall'. Пожалуйста, настройте слои в Project Settings -> Tags and Layers.");
                        return;
                  }

                  // Создаем родительский объект для комнаты
                  GameObject roomObject = new GameObject("TestRoom");

                  // Создаем материал для стен
                  Material wallMaterial = new Material(Shader.Find("Standard"));
                  wallMaterial.name = "WallMaterial";
                  wallMaterial.color = Color.white;

                  // Создаем пол
                  GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
                  floor.name = "Floor";
                  floor.transform.SetParent(roomObject.transform);
                  floor.transform.localPosition = new Vector3(0, -0.05f, 0);
                  floor.transform.localScale = new Vector3(5, 0.1f, 5);

                  // Создаем стены
                  CreateWall("WallNorth", roomObject.transform, new Vector3(0, 1, 2.5f), new Vector3(5, 2, 0.1f), wallMaterial);
                  CreateWall("WallSouth", roomObject.transform, new Vector3(0, 1, -2.5f), new Vector3(5, 2, 0.1f), wallMaterial);
                  CreateWall("WallEast", roomObject.transform, new Vector3(2.5f, 1, 0), new Vector3(0.1f, 2, 5), wallMaterial);
                  CreateWall("WallWest", roomObject.transform, new Vector3(-2.5f, 1, 0), new Vector3(0.1f, 2, 5), wallMaterial);

                  // Выбираем созданную комнату
                  Selection.activeGameObject = roomObject;

                  Debug.Log("Создана тестовая комната с 4 стенами. Тег стен: 'Wall', слой: 'Wall'");
            }

            private static GameObject CreateWall(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
            {
                  GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                  wall.name = name;
                  wall.tag = WALL_TAG;
                  wall.layer = WALL_LAYER;
                  wall.transform.SetParent(parent);
                  wall.transform.localPosition = position;
                  wall.transform.localScale = scale;

                  MeshRenderer renderer = wall.GetComponent<MeshRenderer>();
                  if (renderer != null && material != null)
                  {
                        renderer.material = material;
                  }

                  return wall;
            }
      }
}