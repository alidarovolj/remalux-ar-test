using UnityEngine;
using UnityEditor;
using System.Reflection;

namespace Remalux.AR
{
      public static class WallPainterDirectInputHandler
      {
            [MenuItem("Tools/Wall Painting/Fix/Add Direct Input Handler")]
            public static void AddDirectInputHandler()
            {
                  Debug.Log("=== Добавление компонента прямой обработки ввода для WallPainter ===");

                  // Проверяем, существует ли уже объект с обработчиком ввода
                  GameObject existingHandler = GameObject.Find("WallPainterInputHandler");
                  if (existingHandler != null)
                  {
                        Debug.Log("Объект WallPainterInputHandler уже существует в сцене.");

                        // Проверяем, есть ли на нем компонент DirectInputHandler
                        DirectInputHandler existingComponent = existingHandler.GetComponent<DirectInputHandler>();
                        if (existingComponent != null)
                        {
                              Debug.Log("Компонент DirectInputHandler уже присутствует на объекте.");
                              return;
                        }

                        // Добавляем компонент
                        existingHandler.AddComponent<DirectInputHandler>();
                        Debug.Log("Добавлен компонент DirectInputHandler к существующему объекту.");
                        return;
                  }

                  // Создаем новый объект для обработки ввода
                  GameObject inputHandler = new GameObject("WallPainterInputHandler");
                  inputHandler.AddComponent<DirectInputHandler>();

                  Debug.Log("Создан новый объект WallPainterInputHandler с компонентом DirectInputHandler.");
            }

            [MenuItem("Tools/Wall Painting/Debug/Test Direct Input")]
            public static void TestDirectInput()
            {
                  Debug.Log("=== Тестирование прямой обработки ввода для WallPainter ===");

                  // Находим все WallPainter в сцене
                  MonoBehaviour[] allComponents = Object.FindObjectsOfType<MonoBehaviour>();

                  foreach (MonoBehaviour component in allComponents)
                  {
                        if (component.GetType().Name == "WallPainter")
                        {
                              Debug.Log($"Найден WallPainter на объекте {component.gameObject.name}");

                              // Получаем метод PaintWallAtPosition через рефлексию
                              MethodInfo paintMethod = component.GetType().GetMethod("PaintWallAtPosition");
                              if (paintMethod != null)
                              {
                                    // Вызываем метод с координатами центра экрана
                                    Vector2 screenCenter = new Vector2(Screen.width / 2, Screen.height / 2);
                                    Debug.Log($"Вызов метода PaintWallAtPosition с координатами {screenCenter}");
                                    paintMethod.Invoke(component, new object[] { screenCenter });
                              }
                              else
                              {
                                    Debug.LogError("Не удалось найти метод PaintWallAtPosition в компоненте WallPainter");
                              }
                        }
                  }

                  Debug.Log("=== Завершение тестирования прямой обработки ввода ===");
            }

            [MenuItem("Tools/Wall Painting/Fix/Fix Input Handling")]
            public static void FixInputHandling()
            {
                  Debug.Log("=== Исправление обработки ввода в WallPainter ===");

                  // Находим все WallPainter в сцене
                  MonoBehaviour[] allComponents = Object.FindObjectsOfType<MonoBehaviour>();
                  int fixedCount = 0;

                  foreach (MonoBehaviour component in allComponents)
                  {
                        if (component.GetType().Name == "WallPainter")
                        {
                              Debug.Log($"Исправление обработки ввода в WallPainter на объекте {component.gameObject.name}...");

                              // Проверяем, есть ли уже компонент InputHandler
                              InputHandler existingHandler = component.gameObject.GetComponent<InputHandler>();
                              if (existingHandler == null)
                              {
                                    // Добавляем компонент InputHandler
                                    InputHandler handler = component.gameObject.AddComponent<InputHandler>();
                                    handler.wallPainter = component;
                                    fixedCount++;
                                    Debug.Log($"  - Добавлен компонент InputHandler к объекту {component.gameObject.name}");
                              }
                              else
                              {
                                    Debug.Log($"  - Компонент InputHandler уже присутствует на объекте {component.gameObject.name}");
                              }
                        }
                  }

                  // Добавляем глобальный обработчик ввода, если его еще нет
                  GameObject existingGlobalHandler = GameObject.Find("WallPainterInputHandler");
                  if (existingGlobalHandler == null)
                  {
                        GameObject inputHandler = new GameObject("WallPainterInputHandler");
                        inputHandler.AddComponent<DirectInputHandler>();
                        Debug.Log("  - Создан глобальный обработчик ввода WallPainterInputHandler");
                        fixedCount++;
                  }

                  Debug.Log($"=== Исправлено {fixedCount} компонентов обработки ввода ===");
            }
      }

      // Компонент для прямой обработки ввода
      public class DirectInputHandler : MonoBehaviour
      {
            [Header("Настройки обработки ввода")]
            public bool enableMouseInput = true;
            public bool enableTouchInput = true;
            public bool logInputEvents = true;

            private MonoBehaviour[] wallPainters;
            private MethodInfo[] paintMethods;

            private void Start()
            {
                  // Находим все WallPainter в сцене
                  MonoBehaviour[] allComponents = FindObjectsOfType<MonoBehaviour>();
                  System.Collections.Generic.List<MonoBehaviour> painters = new System.Collections.Generic.List<MonoBehaviour>();
                  System.Collections.Generic.List<MethodInfo> methods = new System.Collections.Generic.List<MethodInfo>();

                  foreach (MonoBehaviour component in allComponents)
                  {
                        if (component.GetType().Name == "WallPainter")
                        {
                              painters.Add(component);

                              // Получаем метод PaintWallAtPosition через рефлексию
                              MethodInfo paintMethod = component.GetType().GetMethod("PaintWallAtPosition");
                              if (paintMethod != null)
                              {
                                    methods.Add(paintMethod);
                              }
                              else
                              {
                                    Debug.LogError($"Не удалось найти метод PaintWallAtPosition в компоненте WallPainter на объекте {component.gameObject.name}");
                              }
                        }
                  }

                  wallPainters = painters.ToArray();
                  paintMethods = methods.ToArray();

                  Debug.Log($"DirectInputHandler: инициализация завершена. Найдено {wallPainters.Length} компонентов WallPainter.");
            }

            private void Update()
            {
                  if (wallPainters == null || wallPainters.Length == 0)
                        return;

                  // Обработка нажатия мыши
                  if (enableMouseInput && Input.GetMouseButtonDown(0))
                  {
                        // Исправление: конвертируем Vector3 в Vector2
                        Vector2 mousePos = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
                        if (logInputEvents)
                        {
                              Debug.Log($"DirectInputHandler: обнаружено нажатие мыши в позиции {mousePos}");
                        }

                        PaintAtPosition(mousePos);
                  }

                  // Обработка сенсорного ввода
                  if (enableTouchInput && Input.touchCount > 0)
                  {
                        Touch touch = Input.GetTouch(0);
                        if (touch.phase == TouchPhase.Began)
                        {
                              if (logInputEvents)
                              {
                                    Debug.Log($"DirectInputHandler: обнаружено касание в позиции {touch.position}");
                              }

                              PaintAtPosition(touch.position);
                        }
                  }
            }

            private void PaintAtPosition(Vector2 position)
            {
                  if (wallPainters == null || paintMethods == null || wallPainters.Length == 0)
                  {
                        Debug.LogWarning("DirectInputHandler: не найдены компоненты WallPainter для обработки ввода.");
                        return;
                  }

                  for (int i = 0; i < wallPainters.Length; i++)
                  {
                        if (wallPainters[i] != null && paintMethods[i] != null)
                        {
                              if (logInputEvents)
                              {
                                    Debug.Log($"DirectInputHandler: вызов метода PaintWallAtPosition на объекте {wallPainters[i].gameObject.name} с позицией {position}");
                              }

                              paintMethods[i].Invoke(wallPainters[i], new object[] { position });
                        }
                  }
            }
      }

      // Компонент для обработки ввода, прикрепляемый к объекту с WallPainter
      public class InputHandler : MonoBehaviour
      {
            [HideInInspector]
            public MonoBehaviour wallPainter;

            [Header("Настройки обработки ввода")]
            public bool enableMouseInput = true;
            public bool enableTouchInput = true;
            public bool logInputEvents = true;

            private MethodInfo paintWallAtPositionMethod;

            private void Start()
            {
                  if (wallPainter != null)
                  {
                        // Получаем метод PaintWallAtPosition через рефлексию
                        paintWallAtPositionMethod = wallPainter.GetType().GetMethod("PaintWallAtPosition");

                        if (paintWallAtPositionMethod == null)
                        {
                              Debug.LogError("InputHandler: не удалось найти метод PaintWallAtPosition в компоненте WallPainter");
                              enabled = false;
                        }
                        else
                        {
                              Debug.Log($"InputHandler: инициализация завершена для объекта {gameObject.name}");
                        }
                  }
                  else
                  {
                        Debug.LogError("InputHandler: не задана ссылка на компонент WallPainter");
                        enabled = false;
                  }
            }

            private void Update()
            {
                  if (wallPainter == null || paintWallAtPositionMethod == null)
                        return;

                  // Обработка ввода мыши
                  if (enableMouseInput && Input.GetMouseButtonDown(0))
                  {
                        // Исправление: конвертируем Vector3 в Vector2
                        Vector2 mousePos = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
                        if (logInputEvents)
                        {
                              Debug.Log($"InputHandler: обнаружено нажатие мыши в позиции {mousePos}");
                        }

                        paintWallAtPositionMethod.Invoke(wallPainter, new object[] { mousePos });
                  }

                  // Обработка сенсорного ввода
                  if (enableTouchInput && Input.touchCount > 0)
                  {
                        Touch touch = Input.GetTouch(0);
                        if (touch.phase == TouchPhase.Began)
                        {
                              if (logInputEvents)
                              {
                                    Debug.Log($"InputHandler: обнаружено касание в позиции {touch.position}");
                              }

                              paintWallAtPositionMethod.Invoke(wallPainter, new object[] { touch.position });
                        }
                  }
            }
      }
}