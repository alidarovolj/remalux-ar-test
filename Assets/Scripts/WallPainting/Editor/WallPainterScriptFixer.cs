#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using System.Reflection;
using System.IO;

namespace Remalux.AR
{
      public static class WallPainterScriptFixer
      {
            [MenuItem("Tools/Wall Painting/Fix/Fix Missing Script References")]
            public static void FixMissingScriptReferences()
            {
                  Debug.Log("=== Начало исправления отсутствующих ссылок на скрипты ===");

                  // Находим все объекты с отсутствующими скриптами
                  GameObject[] allObjects = Object.FindObjectsOfType<GameObject>(true); // включая неактивные объекты
                  int fixedCount = 0;

                  foreach (GameObject obj in allObjects)
                  {
                        // Проверяем компоненты на наличие отсутствующих скриптов
                        Component[] components = obj.GetComponents<Component>();
                        List<int> missingIndices = new List<int>();

                        for (int i = 0; i < components.Length; i++)
                        {
                              if (components[i] == null)
                              {
                                    missingIndices.Add(i);
                              }
                        }

                        if (missingIndices.Count > 0)
                        {
                              Debug.Log($"Объект {obj.name} имеет {missingIndices.Count} отсутствующих скриптов");

                              // Удаляем отсутствующие скрипты
                              SerializedObject serializedObject = new SerializedObject(obj);
                              SerializedProperty componentsProperty = serializedObject.FindProperty("m_Component");

                              // Удаляем с конца, чтобы не нарушить индексы
                              for (int i = missingIndices.Count - 1; i >= 0; i--)
                              {
                                    componentsProperty.DeleteArrayElementAtIndex(missingIndices[i]);
                                    fixedCount++;
                              }

                              serializedObject.ApplyModifiedProperties();
                              EditorUtility.SetDirty(obj);

                              Debug.Log($"Удалено {missingIndices.Count} отсутствующих скриптов с объекта {obj.name}");
                        }

                        // Проверяем, есть ли у объекта компонент WallPainterAutoInitializer
                        WallPainterAutoInitializer autoInitializer = obj.GetComponent<WallPainterAutoInitializer>();
                        if (autoInitializer != null)
                        {
                              // Проверяем, есть ли у объекта компонент WallPainter
                              bool hasWallPainter = false;
                              foreach (Component comp in components)
                              {
                                    if (comp != null && comp.GetType().Name == "WallPainter")
                                    {
                                          hasWallPainter = true;
                                          break;
                                    }
                              }

                              if (!hasWallPainter)
                              {
                                    Debug.LogWarning($"Объект {obj.name} имеет компонент WallPainterAutoInitializer, но не имеет компонента WallPainter. Удаляем автоинициализатор.");
                                    Object.DestroyImmediate(autoInitializer);
                                    fixedCount++;
                              }
                        }
                  }

                  // Сохраняем сцену
                  EditorSceneManager.MarkAllScenesDirty();
                  EditorSceneManager.SaveOpenScenes();

                  Debug.Log($"=== Исправлено {fixedCount} отсутствующих ссылок на скрипты ===");

                  // Запускаем полную настройку WallPainter
                  Debug.Log("Запуск полной настройки WallPainter...");
                  WallPainterFixAll.FixAllWallPaintingIssues();
            }

            [MenuItem("Tools/Wall Painting/Fix/Recreate WallPainter")]
            public static void RecreateWallPainter()
            {
                  Debug.Log("=== Пересоздание компонента WallPainter ===");

                  // Находим все объекты с компонентом WallPainter
                  MonoBehaviour[] allComponents = Object.FindObjectsOfType<MonoBehaviour>();
                  List<GameObject> wallPainterObjects = new List<GameObject>();

                  foreach (MonoBehaviour component in allComponents)
                  {
                        if (component != null && component.GetType().Name == "WallPainter")
                        {
                              wallPainterObjects.Add(component.gameObject);
                        }
                  }

                  if (wallPainterObjects.Count == 0)
                  {
                        Debug.LogWarning("Не найдены объекты с компонентом WallPainter");

                        // Создаем новый объект с компонентом WallPainter
                        GameObject newWallPainterObj = new GameObject("WallPainter");

                        // Добавляем компонент WallPainter
                        System.Type wallPainterType = System.Type.GetType("Remalux.AR.WallPainter, Assembly-CSharp");
                        if (wallPainterType != null)
                        {
                              newWallPainterObj.AddComponent(wallPainterType);
                              Debug.Log($"Создан новый объект {newWallPainterObj.name} с компонентом WallPainter");
                        }
                        else
                        {
                              Debug.LogError("Не удалось найти тип WallPainter");
                        }

                        return;
                  }

                  Debug.Log($"Найдено {wallPainterObjects.Count} объектов с компонентом WallPainter");

                  foreach (GameObject obj in wallPainterObjects)
                  {
                        // Получаем все компоненты
                        Component[] components = obj.GetComponents<Component>();

                        // Находим компонент WallPainter
                        Component wallPainterComponent = null;
                        foreach (Component comp in components)
                        {
                              if (comp != null && comp.GetType().Name == "WallPainter")
                              {
                                    wallPainterComponent = comp;
                                    break;
                              }
                        }

                        if (wallPainterComponent != null)
                        {
                              // Сохраняем значения полей
                              Dictionary<string, object> fieldValues = new Dictionary<string, object>();
                              FieldInfo[] fields = wallPainterComponent.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);

                              foreach (FieldInfo field in fields)
                              {
                                    try
                                    {
                                          object value = field.GetValue(wallPainterComponent);
                                          fieldValues[field.Name] = value;
                                    }
                                    catch (System.Exception e)
                                    {
                                          Debug.LogWarning($"Не удалось получить значение поля {field.Name}: {e.Message}");
                                    }
                              }

                              // Удаляем компонент
                              Object.DestroyImmediate(wallPainterComponent);

                              // Добавляем новый компонент
                              System.Type wallPainterType = System.Type.GetType("Remalux.AR.WallPainter, Assembly-CSharp");
                              if (wallPainterType != null)
                              {
                                    Component newComponent = obj.AddComponent(wallPainterType);

                                    // Восстанавливаем значения полей
                                    foreach (KeyValuePair<string, object> pair in fieldValues)
                                    {
                                          FieldInfo field = wallPainterType.GetField(pair.Key, BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
                                          if (field != null)
                                          {
                                                try
                                                {
                                                      field.SetValue(newComponent, pair.Value);
                                                }
                                                catch (System.Exception e)
                                                {
                                                      Debug.LogWarning($"Не удалось установить значение поля {pair.Key}: {e.Message}");
                                                }
                                          }
                                    }

                                    Debug.Log($"Пересоздан компонент WallPainter на объекте {obj.name}");
                              }
                              else
                              {
                                    Debug.LogError("Не удалось найти тип WallPainter");
                              }
                        }
                  }

                  // Сохраняем сцену
                  EditorSceneManager.MarkAllScenesDirty();
                  EditorSceneManager.SaveOpenScenes();

                  Debug.Log("=== Пересоздание компонента WallPainter завершено ===");

                  // Запускаем полную настройку WallPainter
                  Debug.Log("Запуск полной настройки WallPainter...");
                  WallPainterFixAll.FixAllWallPaintingIssues();
            }
      }
}
#endif