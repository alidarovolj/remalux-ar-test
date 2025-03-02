using UnityEngine;
using System.Reflection;

namespace Remalux.AR
{
      /// <summary>
      /// Компонент для автоматической инициализации WallPainter во время выполнения.
      /// Добавьте этот компонент к тому же объекту, что и WallPainter.
      /// </summary>
      public class WallPainterAutoInitializer : MonoBehaviour
      {
            private MonoBehaviour wallPainter;

            private void Awake()
            {
                  // Находим компонент WallPainter на этом же объекте
                  MonoBehaviour[] components = GetComponents<MonoBehaviour>();
                  foreach (MonoBehaviour component in components)
                  {
                        if (component.GetType().Name == "WallPainter")
                        {
                              wallPainter = component;
                              break;
                        }
                  }

                  if (wallPainter == null)
                  {
                        Debug.LogError("WallPainterAutoInitializer: Не найден компонент WallPainter на этом объекте");
                        return;
                  }

                  // Инициализируем WallPainter
                  InitializeWallPainter();
            }

            private void InitializeWallPainter()
            {
                  Debug.Log($"Автоматическая инициализация WallPainter на объекте {gameObject.name}...");

                  // Получаем тип компонента
                  System.Type wallPainterType = wallPainter.GetType();

                  // Проверяем и инициализируем currentPaintMaterial
                  FieldInfo currentPaintMaterialField = wallPainterType.GetField("currentPaintMaterial");
                  if (currentPaintMaterialField != null)
                  {
                        Material currentMaterial = (Material)currentPaintMaterialField.GetValue(wallPainter);
                        if (currentMaterial == null)
                        {
                              // Пробуем получить материалы из availablePaints
                              FieldInfo availablePaintsField = wallPainterType.GetField("availablePaints");
                              if (availablePaintsField != null)
                              {
                                    Material[] availablePaints = (Material[])availablePaintsField.GetValue(wallPainter);
                                    if (availablePaints != null && availablePaints.Length > 0)
                                    {
                                          // Используем первый доступный материал
                                          currentPaintMaterialField.SetValue(wallPainter, availablePaints[0]);
                                          Debug.Log($"  - Установлен currentPaintMaterial = {availablePaints[0].name}");
                                    }
                              }
                        }
                  }

                  // Проверяем и инициализируем mainCamera
                  FieldInfo mainCameraField = wallPainterType.GetField("mainCamera");
                  if (mainCameraField != null)
                  {
                        Camera mainCamera = (Camera)mainCameraField.GetValue(wallPainter);
                        if (mainCamera == null)
                        {
                              // Ищем главную камеру
                              Camera camera = Camera.main;
                              if (camera != null)
                              {
                                    mainCameraField.SetValue(wallPainter, camera);
                                    Debug.Log($"  - Установлена mainCamera = {camera.name}");
                              }
                        }
                  }

                  // Проверяем и инициализируем wallLayerMask
                  FieldInfo wallLayerMaskField = wallPainterType.GetField("wallLayerMask");
                  if (wallLayerMaskField != null)
                  {
                        try
                        {
                              // Проверяем тип поля
                              if (wallLayerMaskField.FieldType == typeof(int))
                              {
                                    int wallLayerMask = (int)wallLayerMaskField.GetValue(wallPainter);
                                    if (wallLayerMask == 0)
                                    {
                                          // Устанавливаем маску для слоя Wall (8)
                                          int newLayerMask = 1 << 8;
                                          wallLayerMaskField.SetValue(wallPainter, newLayerMask);
                                          Debug.Log($"  - Установлен wallLayerMask (int) для слоя Wall (8)");
                                    }
                              }
                              else if (wallLayerMaskField.FieldType == typeof(LayerMask))
                              {
                                    LayerMask layerMask = (LayerMask)wallLayerMaskField.GetValue(wallPainter);
                                    int maskValue = layerMask.value;
                                    if (maskValue == 0)
                                    {
                                          // Устанавливаем маску для слоя Wall (8)
                                          LayerMask newLayerMask = 1 << 8;
                                          wallLayerMaskField.SetValue(wallPainter, newLayerMask);
                                          Debug.Log($"  - Установлен wallLayerMask (LayerMask) для слоя Wall (8)");
                                    }
                              }
                        }
                        catch (System.Exception e)
                        {
                              Debug.LogError($"  - Ошибка при работе с wallLayerMask: {e.Message}");
                        }
                  }

                  // Вызываем метод SelectPaintMaterial для инициализации
                  MethodInfo selectPaintMaterialMethod = wallPainterType.GetMethod("SelectPaintMaterial");
                  if (selectPaintMaterialMethod != null)
                  {
                        try
                        {
                              selectPaintMaterialMethod.Invoke(wallPainter, new object[] { 0 });
                              Debug.Log("  - Вызван метод SelectPaintMaterial(0) для инициализации");
                        }
                        catch (System.Exception e)
                        {
                              Debug.LogError($"  - Ошибка при вызове метода SelectPaintMaterial: {e.Message}");
                        }
                  }
            }
      }
}