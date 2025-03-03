using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;

namespace Remalux.AR
{
      /// <summary>
      /// Упрощенная версия селектора цвета краски
      /// </summary>
      public class SimplePaintColorSelector : MonoBehaviour
      {
            [SerializeField] public WallPainter wallPainter;
            [SerializeField] public GameObject colorButtonPrefab;
            [SerializeField] public Transform colorButtonsContainer;
            [SerializeField] public Material[] paintMaterials;
            [SerializeField] public Button resetButton;

            private List<Button> colorButtons = new List<Button>();
            private int selectedColorIndex = -1;
            private bool isInitialized = false;

            /// <summary>
            /// Возвращает подходящий шейдер в зависимости от используемого рендер пайплайна
            /// </summary>
            private Shader GetAppropriateShader()
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

            private void Awake()
            {
                  if (resetButton != null)
                        resetButton.onClick.AddListener(ResetWallColors);
            }

            private void Start()
            {
                  Debug.Log("SimplePaintColorSelector: Starting initialization process");
                  StartCoroutine(DelayedInitialization());
            }

            private IEnumerator DelayedInitialization()
            {
                  yield return null; // Wait one frame

                  if (!isInitialized)
                  {
                        Debug.Log("SimplePaintColorSelector: Beginning delayed initialization");

                        // Find WallPainter if not assigned
                        if (wallPainter == null)
                        {
                              Debug.Log("SimplePaintColorSelector: Looking for WallPainter...");
                              yield return new WaitForSeconds(0.1f); // Small delay to ensure WallPainter has spawned
                              wallPainter = FindObjectOfType<WallPainter>();
                              if (wallPainter == null)
                              {
                                    Debug.LogError("SimplePaintColorSelector: No WallPainter found!");
                                    yield break;
                              }
                              Debug.Log("SimplePaintColorSelector: Found WallPainter");
                        }

                        // Wait for WallPainter to initialize with timeout
                        float timeoutDuration = 5f;
                        float elapsedTime = 0f;

                        while (!wallPainter.IsInitialized)
                        {
                              elapsedTime += Time.deltaTime;
                              if (elapsedTime > timeoutDuration)
                              {
                                    Debug.LogError("SimplePaintColorSelector: Timeout waiting for WallPainter initialization!");
                                    yield break;
                              }
                              yield return null;
                        }

                        Debug.Log("SimplePaintColorSelector: WallPainter is initialized, proceeding with initialization");
                        Initialize();
                  }
            }

            public void Initialize()
            {
                  // Find or get references
                  if (wallPainter == null)
                  {
                        wallPainter = FindObjectOfType<WallPainter>();
                        if (wallPainter == null)
                        {
                              Debug.LogError("SimplePaintColorSelector: No WallPainter found!");
                              return;
                        }
                  }

                  // Get materials from WallPainter if not assigned
                  if (paintMaterials == null || paintMaterials.Length == 0)
                  {
                        paintMaterials = wallPainter.availablePaints;
                        if (paintMaterials == null || paintMaterials.Length == 0)
                        {
                              Debug.LogError("SimplePaintColorSelector: No paint materials available!");
                              return;
                        }
                  }

                  // Setup UI container
                  if (colorButtonsContainer == null)
                  {
                        GameObject container = new GameObject("ColorButtonsContainer");
                        container.transform.SetParent(transform);
                        RectTransform rectTransform = container.AddComponent<RectTransform>();
                        rectTransform.anchorMin = Vector2.zero;
                        rectTransform.anchorMax = Vector2.one;
                        rectTransform.sizeDelta = Vector2.zero;
                        colorButtonsContainer = container.transform;
                  }

                  // Setup button prefab
                  if (colorButtonPrefab == null)
                  {
                        colorButtonPrefab = CreateDefaultButtonPrefab();
                  }

                  // Setup reset button
                  if (resetButton != null)
                  {
                        resetButton.onClick.RemoveAllListeners();
                        resetButton.onClick.AddListener(ResetWallColors);
                  }

                  CreateColorButtons();
                  isInitialized = true;
            }

            private GameObject CreateDefaultButtonPrefab()
            {
                  GameObject buttonObj = new GameObject("ColorButton");
                  RectTransform rectTransform = buttonObj.AddComponent<RectTransform>();
                  rectTransform.sizeDelta = new Vector2(50, 50);

                  Image image = buttonObj.AddComponent<Image>();
                  Button button = buttonObj.AddComponent<Button>();
                  button.targetGraphic = image;

                  return buttonObj;
            }

            private void CreateColorButtons()
            {
                  if (colorButtonPrefab == null || colorButtonsContainer == null || paintMaterials == null)
                  {
                        Debug.LogError("SimplePaintColorSelector: Missing required components!");
                        return;
                  }

                  // Clear existing buttons
                  foreach (var button in colorButtons)
                  {
                        if (button != null)
                        {
                              Destroy(button.gameObject);
                        }
                  }
                  colorButtons.Clear();

                  // Create new buttons
                  for (int i = 0; i < paintMaterials.Length; i++)
                  {
                        Material material = paintMaterials[i];
                        if (material == null) continue;

                        GameObject buttonObj = Instantiate(colorButtonPrefab, colorButtonsContainer);
                        Button button = buttonObj.GetComponent<Button>();

                        if (button != null)
                        {
                              // Set button color
                              Image buttonImage = button.GetComponent<Image>();
                              if (buttonImage != null && material.HasProperty("_Color"))
                              {
                                    buttonImage.color = material.GetColor("_Color");
                              }

                              // Add click listener
                              int index = i;
                              button.onClick.AddListener(() => SelectColor(index));
                              colorButtons.Add(button);
                        }
                  }

                  // Select first color
                  if (colorButtons.Count > 0)
                  {
                        SelectColor(0);
                  }
            }

            private void SelectColor(int index)
            {
                  if (index < 0 || index >= paintMaterials.Length || wallPainter == null)
                        return;

                  selectedColorIndex = index;

                  // Update button visuals
                  for (int i = 0; i < colorButtons.Count; i++)
                  {
                        if (colorButtons[i] != null)
                        {
                              colorButtons[i].transform.localScale = (i == selectedColorIndex)
                                    ? new Vector3(1.2f, 1.2f, 1.2f)
                                    : Vector3.one;
                        }
                  }

                  // Update WallPainter
                  wallPainter.SelectPaintMaterial(index);
            }

            public void ResetWallColors()
            {
                  if (wallPainter != null)
                  {
                        wallPainter.ResetWallMaterials();
                  }
            }
      }
}