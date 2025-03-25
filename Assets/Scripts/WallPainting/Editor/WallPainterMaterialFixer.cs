#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.Reflection;

namespace Remalux.AR
{
      public static class WallPainterMaterialFixer
      {
            [MenuItem("Tools/Wall Painting/Fix/Fix Material Application")]
            public static void FixMaterialApplication()
            {
                  Debug.Log("=== Исправление проблем с применением материалов в WallPainter ===");

                  // Находим все WallPainter в сцене
                  MonoBehaviour[] allComponents = Object.FindObjectsOfType<MonoBehaviour>();
                  int fixedCount = 0;

                  foreach (MonoBehaviour component in allComponents)
                  {
                        if (component.GetType().Name == "WallPainter")
                        {
                              bool wasFixed = FixWallPainterMaterials(component);
                              if (wasFixed)
                              {
                                    fixedCount++;
                              }
                        }
                  }

                  Debug.Log($"=== Исправлено {fixedCount} компонентов WallPainter ===");
            }

            private static bool FixWallPainterMaterials(MonoBehaviour wallPainter)
            {
                  Debug.Log($"Проверка материалов в WallPainter на объекте {wallPainter.gameObject.name}...");
                  bool anyChanges = false;

                  // Проверяем доступные материалы
                  FieldInfo paintMaterialsField = wallPainter.GetType().GetField("paintMaterials");
                  if (paintMaterialsField != null)
                  {
                        Material[] materials = (Material[])paintMaterialsField.GetValue(wallPainter);
                        if (materials == null || materials.Length == 0)
                        {
                              Debug.LogError($"  - Нет доступных материалов для покраски в {wallPainter.gameObject.name}");
                        }
                        else
                        {
                              Debug.Log($"  - Доступно {materials.Length} материалов для покраски");

                              // Проверяем текущий материал
                              FieldInfo currentPaintMaterialField = wallPainter.GetType().GetField("currentPaintMaterial");
                              if (currentPaintMaterialField != null)
                              {
                                    Material currentMaterial = (Material)currentPaintMaterialField.GetValue(wallPainter);
                                    if (currentMaterial == null)
                                    {
                                          // Устанавливаем первый доступный материал как текущий
                                          currentPaintMaterialField.SetValue(wallPainter, materials[0]);
                                          anyChanges = true;
                                          Debug.Log($"  - Установлен материал по умолчанию: {materials[0].name}");
                                    }
                                    else
                                    {
                                          Debug.Log($"  - Текущий материал: {currentMaterial.name}");
                                    }
                              }
                        }
                  }

                  // Добавляем компонент для улучшенного применения материалов
                  WallPainterMaterialEnhancer enhancer = wallPainter.gameObject.GetComponent<WallPainterMaterialEnhancer>();
                  if (enhancer == null)
                  {
                        enhancer = wallPainter.gameObject.AddComponent<WallPainterMaterialEnhancer>();
                        enhancer.wallPainter = wallPainter;
                        anyChanges = true;
                        Debug.Log($"  - Добавлен компонент WallPainterMaterialEnhancer для улучшения применения материалов");
                  }

                  // Тестируем покраску стены
                  TestPaintWall(wallPainter);

                  return anyChanges;
            }

            [MenuItem("Tools/Wall Painting/Debug/Test Material Application")]
            public static void TestMaterialApplication()
            {
                  Debug.Log("=== Тестирование применения материалов ===");

                  // Находим все WallPainter в сцене
                  MonoBehaviour[] allComponents = Object.FindObjectsOfType<MonoBehaviour>();

                  foreach (MonoBehaviour component in allComponents)
                  {
                        if (component.GetType().Name == "WallPainter")
                        {
                              TestPaintWall(component);
                        }
                  }

                  Debug.Log("=== Завершение тестирования применения материалов ===");
            }

            private static void TestPaintWall(MonoBehaviour wallPainter)
            {
                  Debug.Log($"Тестирование покраски стены для {wallPainter.gameObject.name}...");

                  // Получаем текущий материал
                  FieldInfo currentPaintMaterialField = wallPainter.GetType().GetField("currentPaintMaterial");
                  Material currentMaterial = null;
                  if (currentPaintMaterialField != null)
                  {
                        currentMaterial = (Material)currentPaintMaterialField.GetValue(wallPainter);
                        Debug.Log($"  - Текущий материал для покраски: {(currentMaterial != null ? currentMaterial.name : "Не задан")}");
                  }

                  // Выполняем рейкаст для поиска стены
                  Camera mainCamera = Camera.main;
                  if (mainCamera != null)
                  {
                        Ray ray = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
                        RaycastHit hit;
                        int wallLayer = 8; // Слой "Wall"
                        bool didHit = Physics.Raycast(ray, out hit, 100f, 1 << wallLayer);

                        if (didHit)
                        {
                              Debug.Log($"  - Рейкаст попал в объект: {hit.collider.gameObject.name}");

                              // Проверяем наличие рендерера
                              Renderer renderer = hit.collider.GetComponent<Renderer>();
                              if (renderer != null)
                              {
                                    Debug.Log($"  - Текущий материал объекта: {renderer.material.name}");

                                    // Напрямую применяем материал
                                    if (currentMaterial != null)
                                    {
                                          Material originalMaterial = renderer.material;
                                          renderer.material = currentMaterial;
                                          Debug.Log($"  - Материал напрямую изменен на: {currentMaterial.name}");

                                          // Добавляем компонент для отслеживания изменений материала
                                          WallMaterialTracker tracker = hit.collider.gameObject.GetComponent<WallMaterialTracker>();
                                          if (tracker == null)
                                          {
                                                tracker = hit.collider.gameObject.AddComponent<WallMaterialTracker>();
                                                tracker.originalMaterial = originalMaterial;
                                                Debug.Log("  - Добавлен компонент WallMaterialTracker для отслеживания изменений материала");
                                          }
                                    }
                                    else
                                    {
                                          Debug.LogError("  - Не удалось применить материал: текущий материал не задан");
                                    }
                              }
                              else
                              {
                                    Debug.LogError($"  - Объект {hit.collider.gameObject.name} не имеет компонента Renderer");
                              }
                        }
                        else
                        {
                              Debug.LogWarning("  - Рейкаст не попал ни в один объект на слое Wall");
                        }
                  }
                  else
                  {
                        Debug.LogError("  - Не найдена основная камера для тестирования");
                  }
            }
      }

      // Компонент для улучшенного применения материалов
      public class WallPainterMaterialEnhancer : MonoBehaviour
      {
            [HideInInspector]
            public MonoBehaviour wallPainter;

            private FieldInfo currentPaintMaterialField;
            private MethodInfo paintWallAtPositionMethod;

            [Header("Настройки применения материалов")]
            public bool enableDirectMaterialApplication = true;
            public bool showDebugInfo = true;

            private void Start()
            {
                  if (wallPainter != null)
                  {
                        // Получаем доступ к полям и методам через рефлексию
                        currentPaintMaterialField = wallPainter.GetType().GetField("currentPaintMaterial");
                        paintWallAtPositionMethod = wallPainter.GetType().GetMethod("PaintWallAtPosition");

                        Debug.Log($"WallPainterMaterialEnhancer: инициализация завершена для {gameObject.name}");
                  }
                  else
                  {
                        Debug.LogError("Не задана ссылка на компонент WallPainter");
                        enabled = false;
                  }
            }

            private void Update()
            {
                  if (wallPainter == null || paintWallAtPositionMethod == null) return;

                  // Use UnityEngine.Input directly
                  if (UnityEngine.Input.GetMouseButtonDown(0))
                  {
                        Vector2 mousePos = UnityEngine.Input.mousePosition;
                        paintWallAtPositionMethod.Invoke(wallPainter, new object[] { mousePos });
                  }
            }
      }

      // Компонент для отслеживания изменений материала стены
      public class WallMaterialTracker : MonoBehaviour
      {
            public Material originalMaterial;
            private Material currentMaterial;

            private void Start()
            {
                  Renderer renderer = GetComponent<Renderer>();
                  if (renderer != null)
                  {
                        if (originalMaterial == null)
                        {
                              originalMaterial = renderer.material;
                        }
                        currentMaterial = renderer.material;
                        Debug.Log($"WallMaterialTracker: инициализация для {gameObject.name}. Оригинальный материал: {originalMaterial.name}, Текущий материал: {currentMaterial.name}");
                  }
            }

            private void OnGUI()
            {
                  Renderer renderer = GetComponent<Renderer>();
                  if (renderer != null && renderer.material != currentMaterial)
                  {
                        currentMaterial = renderer.material;
                        Debug.Log($"WallMaterialTracker: материал объекта {gameObject.name} изменен на {currentMaterial.name}");
                  }
            }
      }
}
#endif