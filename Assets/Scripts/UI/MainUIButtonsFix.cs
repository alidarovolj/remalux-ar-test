using UnityEngine;
using UnityEngine.UI;
using Remalux.AR.Vision;

namespace Remalux.AR
{
    /// <summary>
    /// Исправляет проблемы с кнопками в главном UI
    /// </summary>
    public class MainUIButtonsFix : MonoBehaviour
    {
        [Header("Ссылки на кнопки")]
        [SerializeField] private Button startButton;
        [SerializeField] private Button stopDetectionButton;
        [SerializeField] private Button enablePaintingButton;
        [SerializeField] private Button resetButton;
        
        [Header("Ссылки на менеджеры")]
        [SerializeField] private WallPaintingManager paintingManager;
        [SerializeField] private WallDetector wallDetector;
        
        private void Start()
        {
            // Находим ссылки на компоненты, если они не назначены
            FindReferences();
            
            // Исправляем кнопки
            FixButtons();
        }
        
        private void FindReferences()
        {
            // Находим кнопки по именам, если они не назначены
            if (startButton == null)
            {
                startButton = GameObject.Find("Начать")?.GetComponent<Button>();
                if (startButton == null)
                {
                    startButton = GameObject.Find("StartButton")?.GetComponent<Button>();
                }
            }
            
            if (stopDetectionButton == null)
            {
                stopDetectionButton = GameObject.Find("Стоп детекция")?.GetComponent<Button>();
                if (stopDetectionButton == null)
                {
                    stopDetectionButton = GameObject.Find("StopDetectionButton")?.GetComponent<Button>();
                }
            }
            
            if (enablePaintingButton == null)
            {
                enablePaintingButton = GameObject.Find("Включить покраску")?.GetComponent<Button>();
                if (enablePaintingButton == null)
                {
                    enablePaintingButton = GameObject.Find("EnablePaintingButton")?.GetComponent<Button>();
                }
            }
            
            if (resetButton == null)
            {
                resetButton = GameObject.Find("Сбросить")?.GetComponent<Button>();
                if (resetButton == null)
                {
                    resetButton = GameObject.Find("ResetButton")?.GetComponent<Button>();
                }
            }
            
            // Находим менеджеры
            if (paintingManager == null)
            {
                paintingManager = FindObjectOfType<WallPaintingManager>();
            }
            
            if (wallDetector == null)
            {
                wallDetector = FindObjectOfType<WallDetector>();
            }
        }
        
        private void FixButtons()
        {
            // Исправляем кнопку "Начать"
            if (startButton != null)
            {
                startButton.onClick.RemoveAllListeners();
                startButton.onClick.AddListener(() => {
                    Debug.Log("Нажата кнопка 'Начать'");
                    if (wallDetector != null)
                    {
                        wallDetector.StartDetection();
                    }
                });
                startButton.interactable = true;
            }
            
            // Исправляем кнопку "Стоп детекция"
            if (stopDetectionButton != null)
            {
                stopDetectionButton.onClick.RemoveAllListeners();
                stopDetectionButton.onClick.AddListener(() => {
                    Debug.Log("Нажата кнопка 'Стоп детекция'");
                    if (wallDetector != null)
                    {
                        wallDetector.StopDetection();
                    }
                });
                stopDetectionButton.interactable = true;
            }
            
            // Исправляем кнопку "Включить покраску"
            if (enablePaintingButton != null)
            {
                enablePaintingButton.onClick.RemoveAllListeners();
                enablePaintingButton.onClick.AddListener(() => {
                    Debug.Log("Нажата кнопка 'Включить покраску'");
                    if (paintingManager != null)
                    {
                        paintingManager.StartPainting();
                    }
                });
                enablePaintingButton.interactable = true;
            }
            
            // Исправляем кнопку "Сбросить"
            if (resetButton != null)
            {
                resetButton.onClick.RemoveAllListeners();
                resetButton.onClick.AddListener(() => {
                    Debug.Log("Нажата кнопка 'Сбросить'");
                    if (paintingManager != null)
                    {
                        paintingManager.ResetPainting();
                    }
                });
                resetButton.interactable = true;
            }
            
            Debug.Log("MainUIButtonsFix: Кнопки исправлены");
        }
    }
} 