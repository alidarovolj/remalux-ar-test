using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace Remalux.AR
{
    /// <summary>
    /// Прямое исправление UI, показанного на скриншоте
    /// </summary>
    public class DirectUIFix : MonoBehaviour
    {
        private void Start()
        {
            // Запускаем корутину для исправления UI с небольшой задержкой
            StartCoroutine(FixUIWithDelay());
        }
        
        private IEnumerator FixUIWithDelay()
        {
            // Ждем, чтобы все компоненты успели инициализироваться
            yield return new WaitForSeconds(0.5f);
            
            // Находим все Canvas в сцене
            Canvas[] allCanvases = FindObjectsOfType<Canvas>();
            
            // Ищем Canvas с текстом "Система покраски реальных стен"
            foreach (Canvas canvas in allCanvases)
            {
                Text[] texts = canvas.GetComponentsInChildren<Text>(true);
                foreach (Text text in texts)
                {
                    if (text.text.Contains("Система покраски реальных стен"))
                    {
                        Debug.Log($"DirectUIFix: Найден Canvas с заголовком 'Система покраски реальных стен': {canvas.name}");
                        
                        // Находим все кнопки на этом Canvas
                        Button[] buttons = canvas.GetComponentsInChildren<Button>(true);
                        
                        // Ищем кнопку "Начать"
                        Button startButton = null;
                        foreach (Button button in buttons)
                        {
                            Text buttonText = button.GetComponentInChildren<Text>();
                            if (buttonText != null && buttonText.text.Contains("Начать"))
                            {
                                startButton = button;
                                Debug.Log($"DirectUIFix: Найдена кнопка 'Начать': {button.name}");
                                break;
                            }
                        }
                        
                        // Если нашли кнопку "Начать", добавляем обработчик для закрытия Canvas
                        if (startButton != null)
                        {
                            startButton.onClick.AddListener(() => {
                                Debug.Log($"DirectUIFix: Закрываем Canvas {canvas.name} по нажатию кнопки 'Начать'");
                                canvas.gameObject.SetActive(false);
                            });
                            
                            // Также добавляем обработчик для всех других кнопок на этом Canvas
                            foreach (Button button in buttons)
                            {
                                if (button != startButton)
                                {
                                    button.onClick.AddListener(() => {
                                        Debug.Log($"DirectUIFix: Закрываем Canvas {canvas.name} по нажатию кнопки {button.name}");
                                        canvas.gameObject.SetActive(false);
                                    });
                                }
                            }
                            
                            Debug.Log($"DirectUIFix: Обработчики для закрытия Canvas {canvas.name} настроены");
                        }
                        else
                        {
                            Debug.LogError("DirectUIFix: Не удалось найти кнопку 'Начать'");
                        }
                        
                        // Добавляем обработчик для закрытия Canvas по клику в любом месте
                        GameObject clickHandler = new GameObject("ClickHandler");
                        clickHandler.transform.SetParent(canvas.transform, false);
                        RectTransform rectTransform = clickHandler.AddComponent<RectTransform>();
                        rectTransform.anchorMin = Vector2.zero;
                        rectTransform.anchorMax = Vector2.one;
                        rectTransform.offsetMin = Vector2.zero;
                        rectTransform.offsetMax = Vector2.zero;
                        
                        // Добавляем компонент для обработки кликов
                        ClickHandler handler = clickHandler.AddComponent<ClickHandler>();
                        handler.canvasToClose = canvas.gameObject;
                        
                        // Устанавливаем порядок сортировки, чтобы clickHandler был позади кнопок
                        rectTransform.SetAsFirstSibling();
                        
                        Debug.Log($"DirectUIFix: Добавлен обработчик кликов для закрытия Canvas {canvas.name}");
                    }
                }
            }
        }
        
        /// <summary>
        /// Вспомогательный класс для обработки кликов
        /// </summary>
        private class ClickHandler : MonoBehaviour, UnityEngine.EventSystems.IPointerClickHandler
        {
            public GameObject canvasToClose;
            
            public void OnPointerClick(UnityEngine.EventSystems.PointerEventData eventData)
            {
                Debug.Log($"DirectUIFix: Закрываем Canvas {canvasToClose.name} по клику в любом месте");
                canvasToClose.SetActive(false);
            }
        }
    }
} 