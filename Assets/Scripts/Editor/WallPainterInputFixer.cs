using UnityEngine;
using UnityEditor;

namespace Remalux.AR
{
      public static class WallPainterInputFixer
      {
            [MenuItem("Tools/Wall Painting/Fix/Fix Input Handling")]
            public static void FixInputHandling()
            {
                  Debug.Log("=== Исправление обработки ввода для WallPainter ===");

                  // Находим все WallPainter в сцене
                  MonoBehaviour[] allComponents = Object.FindObjectsOfType<MonoBehaviour>();
                  int fixedCount = 0;

                  foreach (MonoBehaviour component in allComponents)
                  {
                        if (component.GetType().Name == "WallPainter")
                        {
                              // Добавляем дополнительный обработчик ввода
                              GameObject gameObject = component.gameObject;

                              // Проверяем, есть ли уже компонент EnhancedWallPainterInput
                              EnhancedWallPainterInput existingInput = gameObject.GetComponent<EnhancedWallPainterInput>();
                              if (existingInput == null)
                              {
                                    EnhancedWallPainterInput enhancedInput = gameObject.AddComponent<EnhancedWallPainterInput>();
                                    enhancedInput.wallPainter = component;
                                    fixedCount++;
                                    Debug.Log($"Добавлен EnhancedWallPainterInput к объекту {gameObject.name}");
                              }
                              else
                              {
                                    Debug.Log($"EnhancedWallPainterInput уже присутствует на объекте {gameObject.name}");
                              }
                        }
                  }

                  Debug.Log($"=== Исправлено {fixedCount} компонентов WallPainter ===");
            }

            [MenuItem("Tools/Wall Painting/Debug/Test Wall Painting")]
            public static void TestWallPainting()
            {
                  Debug.Log("=== Тестирование покраски стен ===");

                  // Находим все WallPainter в сцене
                  MonoBehaviour[] allComponents = Object.FindObjectsOfType<MonoBehaviour>();

                  foreach (MonoBehaviour component in allComponents)
                  {
                        if (component.GetType().Name == "WallPainter")
                        {
                              // Получаем метод PaintWallAtPosition через рефлексию
                              System.Reflection.MethodInfo paintMethod = component.GetType().GetMethod("PaintWallAtPosition");
                              if (paintMethod != null)
                              {
                                    // Вызываем метод с координатами центра экрана
                                    Vector2 screenCenter = new Vector2(Screen.width / 2, Screen.height / 2);
                                    paintMethod.Invoke(component, new object[] { screenCenter });
                                    Debug.Log($"Вызван метод PaintWallAtPosition для {component.gameObject.name} с координатами {screenCenter}");
                              }
                              else
                              {
                                    Debug.LogError($"Метод PaintWallAtPosition не найден в {component.gameObject.name}");
                              }
                        }
                  }

                  Debug.Log("=== Завершение тестирования покраски стен ===");
            }

            [MenuItem("Tools/Wall Painting/Fix/Add Direct Input Handlers")]
            public static void AddDirectInputHandlers()
            {
                  Debug.Log("=== Добавление прямых обработчиков ввода ===");

                  // Создаем объект для обработки ввода
                  GameObject inputHandlerObject = GameObject.Find("WallPaintingInputHandler");
                  if (inputHandlerObject == null)
                  {
                        inputHandlerObject = new GameObject("WallPaintingInputHandler");
                        DirectWallPaintingInput handler = inputHandlerObject.AddComponent<DirectWallPaintingInput>();
                        Debug.Log("Создан объект DirectWallPaintingInput для обработки ввода");
                  }
                  else
                  {
                        Debug.Log("Объект WallPaintingInputHandler уже существует");
                  }

                  Debug.Log("=== Завершение добавления прямых обработчиков ввода ===");
            }
      }

      // Компонент для улучшенной обработки ввода, добавляемый к объекту WallPainter
      public class EnhancedWallPainterInput : MonoBehaviour
      {
            [HideInInspector]
            public MonoBehaviour wallPainter;
            private System.Reflection.MethodInfo paintWallAtPositionMethod;

            private void Start()
            {
                  if (wallPainter != null)
                  {
                        // Получаем метод PaintWallAtPosition через рефлексию
                        paintWallAtPositionMethod = wallPainter.GetType().GetMethod("PaintWallAtPosition");
                        if (paintWallAtPositionMethod == null)
                        {
                              Debug.LogError("Метод PaintWallAtPosition не найден в компоненте WallPainter");
                              enabled = false;
                        }
                        else
                        {
                              Debug.Log("EnhancedWallPainterInput: инициализация завершена успешно");
                        }
                  }
                  else
                  {
                        Debug.LogError("Не задана ссылка на компонент WallPainter");
                        enabled = false;
                  }
            }

            private void Update()
            {
                  if (wallPainter == null || paintWallAtPositionMethod == null)
                        return;

                  // Обработка нажатия мыши
                  if (Input.GetMouseButtonDown(0))
                  {
                        // Исправление: конвертируем Vector3 в Vector2
                        Vector2 mousePos = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
                        Debug.Log($"EnhancedWallPainterInput: обработка нажатия мыши в позиции {mousePos}");
                        paintWallAtPositionMethod.Invoke(wallPainter, new object[] { mousePos });
                  }

                  // Обработка касаний (для мобильных устройств)
                  if (Input.touchCount > 0)
                  {
                        Touch touch = Input.GetTouch(0);
                        if (touch.phase == TouchPhase.Began)
                        {
                              Debug.Log($"EnhancedWallPainterInput: обработка касания в позиции {touch.position}");
                              paintWallAtPositionMethod.Invoke(wallPainter, new object[] { touch.position });
                        }
                  }
            }
      }

      // Компонент для прямой обработки ввода, независимый от WallPainter
      public class DirectWallPaintingInput : MonoBehaviour
      {
            private MonoBehaviour[] wallPainters;
            private System.Collections.Generic.Dictionary<MonoBehaviour, System.Reflection.MethodInfo> paintMethods =
                new System.Collections.Generic.Dictionary<MonoBehaviour, System.Reflection.MethodInfo>();

            private void Start()
            {
                  // Находим все WallPainter в сцене
                  MonoBehaviour[] allComponents = FindObjectsOfType<MonoBehaviour>();
                  System.Collections.Generic.List<MonoBehaviour> painters = new System.Collections.Generic.List<MonoBehaviour>();

                  foreach (MonoBehaviour component in allComponents)
                  {
                        if (component.GetType().Name == "WallPainter")
                        {
                              painters.Add(component);

                              // Получаем метод PaintWallAtPosition через рефлексию
                              System.Reflection.MethodInfo paintMethod = component.GetType().GetMethod("PaintWallAtPosition");
                              if (paintMethod != null)
                              {
                                    paintMethods[component] = paintMethod;
                              }
                        }
                  }

                  wallPainters = painters.ToArray();
                  Debug.Log($"DirectWallPaintingInput: найдено {wallPainters.Length} компонентов WallPainter");
            }

            private void Update()
            {
                  if (wallPainters == null || wallPainters.Length == 0)
                        return;

                  // Обработка нажатия мыши
                  if (Input.GetMouseButtonDown(0))
                  {
                        Debug.Log($"DirectWallPaintingInput: обработка нажатия мыши в позиции {Input.mousePosition}");

                        foreach (MonoBehaviour painter in wallPainters)
                        {
                              if (painter != null && paintMethods.ContainsKey(painter))
                              {
                                    paintMethods[painter].Invoke(painter, new object[] { Input.mousePosition });
                              }
                        }
                  }

                  // Обработка касаний (для мобильных устройств)
                  if (Input.touchCount > 0)
                  {
                        Touch touch = Input.GetTouch(0);
                        if (touch.phase == TouchPhase.Began)
                        {
                              Debug.Log($"DirectWallPaintingInput: обработка касания в позиции {touch.position}");

                              foreach (MonoBehaviour painter in wallPainters)
                              {
                                    if (painter != null && paintMethods.ContainsKey(painter))
                                    {
                                          paintMethods[painter].Invoke(painter, new object[] { touch.position });
                                    }
                              }
                        }
                  }
            }
      }
}