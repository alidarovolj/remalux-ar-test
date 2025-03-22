using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace Remalux.AR
{
    /// <summary>
    /// Более точное исправление для начального экрана, которое сохраняет основной UI
    /// </summary>
    public class FixedStartScreenHandler : MonoBehaviour
    {
        [Header("Ссылки на UI")]
        [SerializeField] private GameObject modalPanel; // Только модальная панель, а не весь Canvas
        [SerializeField] private Button startButton;
        [SerializeField] private Button stopDetectionButton;
        [SerializeField] private Button enablePaintingButton;
        [SerializeField] private Button resetButton;
        
        private void Start()
        {
            // Запускаем корутину с небольшой задержкой для поиска UI элементов
            StartCoroutine(DelayedInit());
        }
        
        private IEnumerator DelayedInit()
        {
            // Ждем, чтобы все компоненты успели инициализироваться
            yield return new WaitForSeconds(0.5f);
            
            // Находим модальную панель
            FindModalPanel();
            
            // Находим кнопки
            FindButtons();
            
            // Настраиваем обработчики событий
            SetupButtonHandlers();
        }
        
        private void FindModalPanel()
        {
            if (modalPanel != null)
                return;
                
            // Ищем объект с текстом "Система покраски реальных стен"
            Text[] allTexts = FindObjectsOfType<Text>();
            foreach (Text text in allTexts)
            {
                if (text.text.Contains("Система покраски реальных стен"))
                {
                    // Нашли заголовок, теперь ищем родительскую панель
                    Transform parent = text.transform.parent;
                    while (parent != null)
                    {
                        // Ищем панель с компонентом Image или Panel
                        if (parent.GetComponent<Image>() != null || 
                            parent.name.Contains("Panel") || 
                            parent.name.Contains("Dialog") || 
                            parent.name.Contains("Modal"))
                        {
                            modalPanel = parent.gameObject;
                            Debug.Log($"FixedStartScreenHandler: Найдена модальная панель: {modalPanel.name}");
                            break;
                        }
                        parent = parent.parent;
                    }
                    
                    if (modalPanel != null)
                        break;
                }
            }
            
            // Если не нашли по тексту, ищем по имени
            if (modalPanel == null)
            {
                GameObject[] allObjects = FindObjectsOfType<GameObject>();
                foreach (GameObject obj in allObjects)
                {
                    if (obj.name.Contains("Panel") || 
                        obj.name.Contains("Dialog") || 
                        obj.name.Contains("Modal") || 
                        obj.name.Contains("Instruction"))
                    {
                        // Проверяем, есть ли у объекта кнопка "Начать"
                        Button[] buttons = obj.GetComponentsInChildren<Button>(true);
                        foreach (Button button in buttons)
                        {
                            Text buttonText = button.GetComponentInChildren<Text>();
                            if (buttonText != null && buttonText.text.Contains("Начать"))
                            {
                                modalPanel = obj;
                                Debug.Log($"FixedStartScreenHandler: Найдена модальная панель по кнопке: {obj.name}");
                                break;
                            }
                        }
                        
                        if (modalPanel != null)
                            break;
                    }
                }
            }
        }
        
        private void FindButtons()
        {
            // Находим кнопку "Начать"
            if (startButton == null)
            {
                startButton = FindButtonByText("Начать");
                if (startButton == null)
                {
                    startButton = GameObject.Find("StartButton")?.GetComponent<Button>();
                }
                
                if (startButton != null)
                {
                    Debug.Log($"FixedStartScreenHandler: Найдена кнопка 'Начать': {startButton.name}");
                }
            }
            
            // Находим кнопку "Стоп детекция"
            if (stopDetectionButton == null)
            {
                stopDetectionButton = FindButtonByText("Стоп детекция");
                if (stopDetectionButton == null)
                {
                    stopDetectionButton = GameObject.Find("StopDetectionButton")?.GetComponent<Button>();
                }
                
                if (stopDetectionButton != null)
                {
                    Debug.Log($"FixedStartScreenHandler: Найдена кнопка 'Стоп детекция': {stopDetectionButton.name}");
                }
            }
            
            // Находим кнопку "Включить покраску"
            if (enablePaintingButton == null)
            {
                enablePaintingButton = FindButtonByText("Включить покраску");
                if (enablePaintingButton == null)
                {
                    enablePaintingButton = GameObject.Find("EnablePaintingButton")?.GetComponent<Button>();
                }
                
                if (enablePaintingButton != null)
                {
                    Debug.Log($"FixedStartScreenHandler: Найдена кнопка 'Включить покраску': {enablePaintingButton.name}");
                }
            }
            
            // Находим кнопку "Сбросить"
            if (resetButton == null)
            {
                resetButton = FindButtonByText("Сбросить");
                if (resetButton == null)
                {
                    resetButton = GameObject.Find("ResetButton")?.GetComponent<Button>();
                }
                
                if (resetButton != null)
                {
                    Debug.Log($"FixedStartScreenHandler: Найдена кнопка 'Сбросить': {resetButton.name}");
                }
            }
        }
        
        private Button FindButtonByText(string buttonText)
        {
            Button[] allButtons = FindObjectsOfType<Button>();
            foreach (Button button in allButtons)
            {
                Text text = button.GetComponentInChildren<Text>();
                if (text != null && text.text.Contains(buttonText))
                {
                    return button;
                }
            }
            return null;
        }
        
        private void SetupButtonHandlers()
        {
            if (modalPanel == null)
            {
                Debug.LogError("FixedStartScreenHandler: Модальная панель не найдена!");
                return;
            }
            
            // Добавляем обработчик для кнопки "Начать"
            if (startButton != null)
            {
                // Сохраняем существующие обработчики
                var existingListeners = new System.Collections.Generic.List<UnityEngine.Events.UnityAction>();
                for (int i = 0; i < startButton.onClick.GetPersistentEventCount(); i++)
                {
                    var target = startButton.onClick.GetPersistentTarget(i);
                    var methodName = startButton.onClick.GetPersistentMethodName(i);
                    if (target != null && !string.IsNullOrEmpty(methodName))
                    {
                        Debug.Log($"FixedStartScreenHandler: Сохранен существующий обработчик: {target}.{methodName}");
                    }
                }
                
                // Добавляем новый обработчик для закрытия модальной панели
                startButton.onClick.AddListener(() => {
                    Debug.Log($"FixedStartScreenHandler: Закрываем модальную панель {modalPanel.name} по нажатию кнопки 'Начать'");
                    modalPanel.SetActive(false);
                });
                
                Debug.Log("FixedStartScreenHandler: Добавлен обработчик для закрытия модальной панели");
            }
            
            // Также добавляем обработчики для других кнопок, если они находятся на модальной панели
            Button[] modalButtons = modalPanel.GetComponentsInChildren<Button>(true);
            foreach (Button button in modalButtons)
            {
                if (button != startButton)
                {
                    button.onClick.AddListener(() => {
                        Debug.Log($"FixedStartScreenHandler: Закрываем модальную панель {modalPanel.name} по нажатию кнопки {button.name}");
                        modalPanel.SetActive(false);
                    });
                }
            }
        }
    }
} 