using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using UnityEngine.UI;

namespace Remalux.WallPainting
{
      public class WallPaintingSetupGuide
      {
            private static readonly string[] RequiredTags = new string[] { "Wall", "Floor", "Ceiling" };
            private static readonly string[] RequiredLayers = new string[] { "Wall", "UI" };

            public static void ValidateSetup()
            {
                  ValidateTags();
                  ValidateLayers();
                  ValidatePrefabs();
            }

            private static void ValidateTags()
            {
                  var missingTags = new List<string>();
                  foreach (var tag in RequiredTags)
                  {
                        if (!TagExists(tag))
                        {
                              missingTags.Add(tag);
                        }
                  }

                  if (missingTags.Count > 0)
                  {
                        Debug.LogWarning($"Отсутствуют необходимые теги: {string.Join(", ", missingTags)}");
                        EditorUtility.DisplayDialog("Отсутствующие теги",
                            $"Для корректной работы системы необходимо добавить следующие теги:\n{string.Join("\n", missingTags)}",
                            "OK");
                  }
            }

            private static void ValidateLayers()
            {
                  var missingLayers = new List<string>();
                  foreach (var layer in RequiredLayers)
                  {
                        if (!LayerExists(layer))
                        {
                              missingLayers.Add(layer);
                        }
                  }

                  if (missingLayers.Count > 0)
                  {
                        Debug.LogWarning($"Отсутствуют необходимые слои: {string.Join(", ", missingLayers)}");
                        EditorUtility.DisplayDialog("Отсутствующие слои",
                            $"Для корректной работы системы необходимо добавить следующие слои:\n{string.Join("\n", missingLayers)}",
                            "OK");
                  }
            }

            private static void ValidatePrefabs()
            {
                  var missingPrefabs = new List<string>();

                  // Проверяем UI префабы
                  var uiElementsCreator = Object.FindObjectOfType<UIElementsCreator>();
                  if (uiElementsCreator != null)
                  {
                        var prefabFields = new Dictionary<string, GameObject>
                {
                    { "buttonPrefab", uiElementsCreator.GetType().GetField("buttonPrefab", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(uiElementsCreator) as GameObject },
                    { "textPrefab", uiElementsCreator.GetType().GetField("textPrefab", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(uiElementsCreator) as GameObject },
                    { "imagePrefab", uiElementsCreator.GetType().GetField("imagePrefab", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(uiElementsCreator) as GameObject }
                };

                        foreach (var prefab in prefabFields)
                        {
                              if (prefab.Value == null)
                              {
                                    missingPrefabs.Add(prefab.Key);
                              }
                        }
                  }

                  if (missingPrefabs.Count > 0)
                  {
                        Debug.LogWarning($"Отсутствуют необходимые префабы: {string.Join(", ", missingPrefabs)}");
                        EditorUtility.DisplayDialog("Отсутствующие префабы",
                            $"Для корректной работы системы необходимо назначить следующие префабы в компоненте UIElementsCreator:\n{string.Join("\n", missingPrefabs)}",
                            "OK");
                  }
            }

            private static bool TagExists(string tag)
            {
                  try
                  {
                        GameObject.FindGameObjectWithTag(tag);
                        return true;
                  }
                  catch
                  {
                        return false;
                  }
            }

            private static bool LayerExists(string layerName)
            {
                  for (int i = 0; i < 32; i++)
                  {
                        if (LayerMask.LayerToName(i) == layerName)
                        {
                              return true;
                        }
                  }
                  return false;
            }

            public static void CreateDefaultPrefabs()
            {
                  // Создаем UI префабы
                  CreateButtonPrefab();
                  CreateTextPrefab();
                  CreateImagePrefab();
            }

            private static void CreateButtonPrefab()
            {
                  var buttonObj = new GameObject("ButtonPrefab");
                  buttonObj.AddComponent<RectTransform>();
                  buttonObj.AddComponent<CanvasRenderer>();
                  var image = buttonObj.AddComponent<Image>();
                  image.color = Color.white;
                  var button = buttonObj.AddComponent<Button>();

                  // Создаем текст для кнопки
                  var textObj = new GameObject("Text");
                  textObj.transform.SetParent(buttonObj.transform, false);
                  var textRect = textObj.AddComponent<RectTransform>();
                  textRect.anchorMin = Vector2.zero;
                  textRect.anchorMax = Vector2.one;
                  textRect.sizeDelta = Vector2.zero;
                  textRect.anchoredPosition = Vector2.zero;

                  var text = textObj.AddComponent<Text>();
                  text.text = "Button";
                  text.color = Color.black;
                  text.alignment = TextAnchor.MiddleCenter;
                  text.fontSize = 14;

                  // Сохраняем префаб
                  PrefabUtility.SaveAsPrefabAsset(buttonObj, "Assets/Prefabs/UI/ButtonPrefab.prefab");
                  Object.DestroyImmediate(buttonObj);
            }

            private static void CreateTextPrefab()
            {
                  var textObj = new GameObject("TextPrefab");
                  textObj.AddComponent<RectTransform>();
                  var text = textObj.AddComponent<Text>();
                  text.text = "Text";
                  text.color = Color.white;
                  text.alignment = TextAnchor.MiddleCenter;
                  text.fontSize = 14;

                  // Сохраняем префаб
                  PrefabUtility.SaveAsPrefabAsset(textObj, "Assets/Prefabs/UI/TextPrefab.prefab");
                  Object.DestroyImmediate(textObj);
            }

            private static void CreateImagePrefab()
            {
                  var imageObj = new GameObject("ImagePrefab");
                  imageObj.AddComponent<RectTransform>();
                  imageObj.AddComponent<CanvasRenderer>();
                  var image = imageObj.AddComponent<Image>();
                  image.color = Color.white;

                  // Сохраняем префаб
                  PrefabUtility.SaveAsPrefabAsset(imageObj, "Assets/Prefabs/UI/ImagePrefab.prefab");
                  Object.DestroyImmediate(imageObj);
            }
      }
}