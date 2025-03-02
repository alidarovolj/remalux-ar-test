using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace Remalux.AR
{
      public class WallPaintingUI : MonoBehaviour
      {
            [Header("Ссылки на менеджеры")]
            [SerializeField] private WallPaintingManager paintingManager;

            [Header("Панели UI")]
            [SerializeField] private GameObject detectionPanel;
            [SerializeField] private GameObject paintingPanel;
            [SerializeField] private GameObject colorPalettePanel;

            [Header("Элементы UI")]
            [SerializeField] private Button startPaintingButton;
            [SerializeField] private Button resetButton;
            [SerializeField] private Button backToDetectionButton;
            [SerializeField] private Button showPaletteButton;
            [SerializeField] private Text statusText;

            [Header("Настройки UI")]
            [SerializeField] private float statusUpdateInterval = 0.5f;

            private float lastStatusUpdateTime;

            private void Start()
            {
                  // Находим менеджер, если не назначен
                  if (paintingManager == null)
                  {
                        paintingManager = FindObjectOfType<WallPaintingManager>();
                  }

                  // Настраиваем кнопки
                  if (startPaintingButton != null)
                  {
                        startPaintingButton.onClick.AddListener(OnStartPaintingClicked);
                        startPaintingButton.interactable = false;
                  }

                  if (resetButton != null)
                  {
                        resetButton.onClick.AddListener(OnResetClicked);
                  }

                  if (backToDetectionButton != null)
                  {
                        backToDetectionButton.onClick.AddListener(OnBackToDetectionClicked);
                  }

                  if (showPaletteButton != null)
                  {
                        showPaletteButton.onClick.AddListener(OnShowPaletteClicked);
                  }

                  // Начальное состояние UI
                  SetDetectionMode(true);
                  if (colorPalettePanel != null)
                  {
                        colorPalettePanel.SetActive(false);
                  }

                  UpdateStatusText("Наведите камеру на стены...");
            }

            private void Update()
            {
                  // Периодически обновляем текст статуса
                  if (Time.time - lastStatusUpdateTime > statusUpdateInterval)
                  {
                        UpdateStatusBasedOnState();
                        lastStatusUpdateTime = Time.time;
                  }
            }

            private void UpdateStatusBasedOnState()
            {
                  if (detectionPanel.activeSelf)
                  {
                        if (!startPaintingButton.interactable)
                        {
                              UpdateStatusText("Наведите камеру на стены...");
                        }
                        else
                        {
                              UpdateStatusText("Стены обнаружены! Нажмите 'Начать покраску'");
                        }
                  }
                  else if (paintingPanel.activeSelf)
                  {
                        UpdateStatusText("Нажмите на стену, чтобы покрасить ее");
                  }
            }

            public void UpdateStatusText(string message)
            {
                  if (statusText != null)
                  {
                        statusText.text = message;
                  }
            }

            public void EnableStartPaintingButton(bool enable)
            {
                  if (startPaintingButton != null)
                  {
                        startPaintingButton.interactable = enable;

                        if (enable)
                        {
                              UpdateStatusText("Стены обнаружены! Нажмите 'Начать покраску'");
                        }
                  }
            }

            private void OnStartPaintingClicked()
            {
                  if (paintingManager != null)
                  {
                        paintingManager.StartPainting();
                        SetDetectionMode(false);
                  }
            }

            private void OnResetClicked()
            {
                  if (paintingManager != null)
                  {
                        paintingManager.ResetPainting();
                        UpdateStatusText("Цвета стен сброшены");
                  }
            }

            private void OnBackToDetectionClicked()
            {
                  if (paintingManager != null)
                  {
                        paintingManager.RestartDetection();
                        SetDetectionMode(true);
                  }
            }

            private void OnShowPaletteClicked()
            {
                  if (colorPalettePanel != null)
                  {
                        bool isActive = colorPalettePanel.activeSelf;
                        colorPalettePanel.SetActive(!isActive);

                        if (showPaletteButton != null && showPaletteButton.GetComponentInChildren<Text>() != null)
                        {
                              showPaletteButton.GetComponentInChildren<Text>().text =
                                  isActive ? "Показать палитру" : "Скрыть палитру";
                        }
                  }
            }

            private void SetDetectionMode(bool isDetectionMode)
            {
                  if (detectionPanel != null)
                  {
                        detectionPanel.SetActive(isDetectionMode);
                  }

                  if (paintingPanel != null)
                  {
                        paintingPanel.SetActive(!isDetectionMode);
                  }

                  if (colorPalettePanel != null)
                  {
                        colorPalettePanel.SetActive(false);
                  }

                  UpdateStatusBasedOnState();
            }
      }
}