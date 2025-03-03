using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace Remalux.AR
{
      public static class WallColliderChecker
      {
            private const string WALL_TAG = "Wall";
            private const int WALL_LAYER = 8; // Слой "Wall" имеет индекс 8

            /// <summary>
            /// Возвращает подходящий шейдер в зависимости от используемого рендер пайплайна
            /// </summary>
            private static Shader GetAppropriateShader()
            {
                  if (UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline != null)
                  {
                        // Для URP
                        Debug.Log("Используется URP, возвращаем URP шейдер");
                        return Shader.Find("Universal Render Pipeline/Lit");
                  }
                  else
                  {
                        // Для стандартного рендер пайплайна
                        return Shader.Find("Standard");
                  }
            }

            [MenuItem("Tools/Wall Painting/Debug/Check Wall Colliders")]
            public static void CheckWallColliders()
            {
                  Debug.Log("=== Проверка коллайдеров стен ===");

                  // Находим все объекты с тегом "Wall"
                  GameObject[] wallTaggedObjects = GameObject.FindGameObjectsWithTag(WALL_TAG);

                  if (wallTaggedObjects.Length == 0)
                  {
                        Debug.LogWarning("Не найдено объектов с тегом 'Wall'.");
                  }
                  else
                  {
                        Debug.Log($"Найдено {wallTaggedObjects.Length} объектов с тегом 'Wall':");

                        int objectsWithoutColliders = 0;
                        int objectsWithWrongLayer = 0;

                        foreach (GameObject obj in wallTaggedObjects)
                        {
                              Debug.Log($"- {obj.name} (слой: {LayerMask.LayerToName(obj.layer)})");

                              // Проверяем слой
                              if (obj.layer != WALL_LAYER)
                              {
                                    Debug.LogWarning($"  - Объект {obj.name} имеет тег 'Wall', но находится на слое '{LayerMask.LayerToName(obj.layer)}' вместо 'Wall'.");
                                    objectsWithWrongLayer++;
                              }

                              // Проверяем наличие коллайдера
                              Collider collider = obj.GetComponent<Collider>();
                              if (collider == null)
                              {
                                    Debug.LogError($"  - Объект {obj.name} не имеет коллайдера. Рейкасты не будут его обнаруживать.");
                                    objectsWithoutColliders++;
                              }
                              else
                              {
                                    Debug.Log($"  - Коллайдер: {collider.GetType().Name}, Enabled: {collider.enabled}, Trigger: {collider.isTrigger}");

                                    // Предупреждение, если коллайдер является триггером
                                    if (collider.isTrigger)
                                    {
                                          Debug.LogWarning($"  - Коллайдер объекта {obj.name} настроен как триггер. Это может помешать рейкастам.");
                                    }
                              }

                              // Проверяем наличие MeshRenderer
                              MeshRenderer renderer = obj.GetComponent<MeshRenderer>();
                              if (renderer == null)
                              {
                                    Debug.LogWarning($"  - Объект {obj.name} не имеет MeshRenderer. Это может помешать визуализации покраски.");
                              }
                              else
                              {
                                    Debug.Log($"  - MeshRenderer: Enabled: {renderer.enabled}, Materials: {renderer.sharedMaterials.Length}");
                              }
                        }

                        // Итоговая статистика
                        if (objectsWithoutColliders > 0)
                        {
                              Debug.LogError($"Обнаружено {objectsWithoutColliders} объектов без коллайдеров. Система покраски стен не будет работать с этими объектами.");
                        }

                        if (objectsWithWrongLayer > 0)
                        {
                              Debug.LogWarning($"Обнаружено {objectsWithWrongLayer} объектов на неправильном слое. Система покраски стен может не работать с этими объектами.");
                        }
                  }

                  // Находим все объекты на слое "Wall"
                  List<GameObject> wallLayerObjects = new List<GameObject>();
                  GameObject[] allObjects = Object.FindObjectsOfType<GameObject>();

                  foreach (GameObject obj in allObjects)
                  {
                        if (obj.layer == WALL_LAYER)
                        {
                              wallLayerObjects.Add(obj);
                        }
                  }

                  if (wallLayerObjects.Count == 0)
                  {
                        Debug.LogError("Не найдено объектов на слое 'Wall'. Система покраски стен не будет работать.");
                  }
                  else
                  {
                        Debug.Log($"Найдено {wallLayerObjects.Count} объектов на слое 'Wall':");

                        int objectsWithoutColliders = 0;
                        int objectsWithWrongTag = 0;

                        foreach (GameObject obj in wallLayerObjects)
                        {
                              Debug.Log($"- {obj.name} (тег: {obj.tag})");

                              // Проверяем тег
                              if (obj.tag != WALL_TAG)
                              {
                                    Debug.LogWarning($"  - Объект {obj.name} находится на слое 'Wall', но имеет тег '{obj.tag}' вместо 'Wall'.");
                                    objectsWithWrongTag++;
                              }

                              // Проверяем наличие коллайдера
                              Collider collider = obj.GetComponent<Collider>();
                              if (collider == null)
                              {
                                    Debug.LogError($"  - Объект {obj.name} не имеет коллайдера. Рейкасты не будут его обнаруживать.");
                                    objectsWithoutColliders++;
                              }
                        }

                        // Итоговая статистика
                        if (objectsWithoutColliders > 0)
                        {
                              Debug.LogError($"Обнаружено {objectsWithoutColliders} объектов без коллайдеров. Система покраски стен не будет работать с этими объектами.");
                        }

                        if (objectsWithWrongTag > 0)
                        {
                              Debug.LogWarning($"Обнаружено {objectsWithWrongTag} объектов с неправильным тегом. Это может вызвать проблемы с системой покраски стен.");
                        }
                  }

                  Debug.Log("=== Завершение проверки коллайдеров стен ===");
            }

            [MenuItem("Tools/Wall Painting/Fix/Fix Wall Layers and Tags")]
            public static void FixWallLayersAndTags()
            {
                  Debug.Log("=== Исправление слоев и тегов стен ===");

                  // Находим все объекты с тегом "Wall" и устанавливаем им слой "Wall"
                  GameObject[] wallTaggedObjects = GameObject.FindGameObjectsWithTag(WALL_TAG);
                  int fixedLayers = 0;

                  foreach (GameObject obj in wallTaggedObjects)
                  {
                        if (obj.layer != WALL_LAYER)
                        {
                              obj.layer = WALL_LAYER;
                              EditorUtility.SetDirty(obj);
                              fixedLayers++;
                              Debug.Log($"Установлен слой 'Wall' для объекта {obj.name}");
                        }
                  }

                  // Находим все объекты на слое "Wall" и устанавливаем им тег "Wall"
                  GameObject[] allObjects = Object.FindObjectsOfType<GameObject>();
                  int fixedTags = 0;

                  foreach (GameObject obj in allObjects)
                  {
                        if (obj.layer == WALL_LAYER && obj.tag != WALL_TAG)
                        {
                              obj.tag = WALL_TAG;
                              EditorUtility.SetDirty(obj);
                              fixedTags++;
                              Debug.Log($"Установлен тег 'Wall' для объекта {obj.name}");
                        }
                  }

                  Debug.Log($"=== Исправлено {fixedLayers} слоев и {fixedTags} тегов ===");
            }

            [MenuItem("Tools/Wall Painting/Fix/Add Colliders to Walls")]
            public static void AddCollidersToWalls()
            {
                  Debug.Log("=== Добавление коллайдеров к стенам ===");

                  // Находим все объекты на слое "Wall" без коллайдеров
                  GameObject[] allObjects = Object.FindObjectsOfType<GameObject>();
                  int addedColliders = 0;

                  foreach (GameObject obj in allObjects)
                  {
                        if (obj.layer == WALL_LAYER)
                        {
                              Collider collider = obj.GetComponent<Collider>();
                              if (collider == null)
                              {
                                    // Проверяем наличие MeshFilter
                                    MeshFilter meshFilter = obj.GetComponent<MeshFilter>();
                                    if (meshFilter != null && meshFilter.sharedMesh != null)
                                    {
                                          // Добавляем MeshCollider
                                          MeshCollider meshCollider = obj.AddComponent<MeshCollider>();
                                          meshCollider.sharedMesh = meshFilter.sharedMesh;
                                          meshCollider.convex = false;
                                          meshCollider.isTrigger = false;

                                          EditorUtility.SetDirty(obj);
                                          addedColliders++;
                                          Debug.Log($"Добавлен MeshCollider к объекту {obj.name}");
                                    }
                                    else
                                    {
                                          // Если нет MeshFilter, добавляем BoxCollider
                                          BoxCollider boxCollider = obj.AddComponent<BoxCollider>();
                                          EditorUtility.SetDirty(obj);
                                          addedColliders++;
                                          Debug.Log($"Добавлен BoxCollider к объекту {obj.name}");
                                    }
                              }
                              else if (collider.isTrigger)
                              {
                                    // Исправляем триггер
                                    collider.isTrigger = false;
                                    EditorUtility.SetDirty(collider);
                                    addedColliders++;
                                    Debug.Log($"Исправлен триггер коллайдера на объекте {obj.name}");
                              }
                        }
                  }

                  Debug.Log($"=== Добавлено/исправлено {addedColliders} коллайдеров ===");
            }

            [MenuItem("Tools/Wall Painting/Debug/Create Test Wall")]
            public static void CreateTestWall()
            {
                  // Создаем тестовую стену
                  GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                  wall.name = "TestWall";
                  wall.tag = WALL_TAG;
                  wall.layer = WALL_LAYER;

                  // Устанавливаем размеры и позицию
                  wall.transform.position = new Vector3(0, 1, 2);
                  wall.transform.localScale = new Vector3(2, 2, 0.1f);

                  // Создаем материал
                  Material wallMaterial = new Material(GetAppropriateShader());
                  wallMaterial.color = Color.white;

                  // Применяем материал
                  MeshRenderer renderer = wall.GetComponent<MeshRenderer>();
                  renderer.material = wallMaterial;

                  // Выбираем созданную стену
                  Selection.activeGameObject = wall;

                  Debug.Log("Создана тестовая стена. Убедитесь, что она находится в поле зрения камеры.");
            }
      }
}