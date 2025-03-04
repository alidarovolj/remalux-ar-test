using UnityEngine;
using UnityEngine.UI;

namespace Remalux.AR
{
    /// <summary>
    /// Скрипт для исправления проблем с начальным экраном
    /// </summary>
    public class StartScreenFix : MonoBehaviour
    {
        [Header("Ссылки на UI")]
        [SerializeField] private GameObject startScreen;
        [SerializeField] private Button startButton;
        
        private void Start()
        {
            // Находим начальный экран по имени
            if (startScreen == null)
            {
                Canvas[] canvases = FindObjectsOfType<Canvas>();
                foreach (Canvas canvas in canvases)
                {
                    if (canvas.name.Contains("Canvas") || canvas.name.Contains("UI"))
                    {
                        // Проверяем, содержит ли Canvas текст "Система покраски реальных стен"
                        Text[] texts = canvas.GetComponentsInChildren<Text>(true);
                        foreach (Text text in texts)
                        {
                            if (text.text.Contains("Система покраски") || text.text.Contains("стен"))
                            {
                                startScreen = canvas.gameObject;
                                Debug.Log($"StartScreenFix: Найден начальный экран: {canvas.name}");
                                break;
                            }
                        }
                        
                        if (startScreen != null)
                            break;
                    }
                }
            }
            
            // Находим кнопку "Начать"
            if (startButton == null && startScreen != null)
            {
                Button[] buttons = startScreen.GetComponentsInChildren<Button>(true);
                foreach (Button button in buttons)
                {
                    Text buttonText = button.GetComponentInChildren<Text>();
                    if (buttonText != null && buttonText.text.Contains("Начать"))
                    {
                        startButton = button;
                        Debug.Log($"StartScreenFix: Найдена кнопка 'Начать': {button.name}");
                        break;
                    }
                }
            }
            
            // Если не нашли по тексту, ищем по имени
            if (startButton == null)
            {
                startButton = GameObject.Find("Начать")?.GetComponent<Button>();
                if (startButton == null)
                {
                    startButton = GameObject.Find("StartButton")?.GetComponent<Button>();
                }
                
                if (startButton != null)
                {
                    Debug.Log($"StartScreenFix: Найдена кнопка по имени: {startButton.name}");
                }
            }
            
            // Добавляем обработчик для закрытия начального экрана
            if (startButton != null && startScreen != null)
            {
                startButton.onClick.AddListener(() => {
                    Debug.Log("StartScreenFix: Закрываем начальный экран по нажатию кнопки 'Начать'");
                    startScreen.SetActive(false);
                });
                
                Debug.Log("StartScreenFix: Обработчик для закрытия начального экрана настроен");
            }
            else
            {
                if (startButton == null)
                    Debug.LogError("StartScreenFix: Не удалось найти кнопку 'Начать'");
                
                if (startScreen == null)
                    Debug.LogError("StartScreenFix: Не удалось найти начальный экран");
            }
        }
    }
} 