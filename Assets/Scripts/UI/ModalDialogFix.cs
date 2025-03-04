using UnityEngine;
using UnityEngine.UI;

namespace Remalux.AR
{
    /// <summary>
    /// Скрипт для исправления проблем с модальным диалогом
    /// </summary>
    public class ModalDialogFix : MonoBehaviour
    {
        [Header("Ссылки на UI")]
        [SerializeField] private GameObject modalDialog;
        [SerializeField] private Button startButton;
        [SerializeField] private Button resetButton;
        [SerializeField] private Button enablePaintingButton;
        
        private void Start()
        {
            // Находим модальный диалог, если он не назначен
            if (modalDialog == null)
            {
                // Ищем объект с именем, содержащим "Modal" или "Dialog"
                GameObject[] allObjects = FindObjectsOfType<GameObject>();
                foreach (GameObject obj in allObjects)
                {
                    if (obj.name.Contains("Modal") || obj.name.Contains("Dialog") || 
                        obj.name.Contains("Система") || obj.name.Contains("System"))
                    {
                        modalDialog = obj;
                        Debug.Log($"ModalDialogFix: Найден модальный диалог: {obj.name}");
                        break;
                    }
                }
                
                // Если не нашли по имени, ищем по структуре (Canvas с кнопками и текстом)
                if (modalDialog == null)
                {
                    Canvas[] canvases = FindObjectsOfType<Canvas>();
                    foreach (Canvas canvas in canvases)
                    {
                        Button[] buttons = canvas.GetComponentsInChildren<Button>(true);
                        Text[] texts = canvas.GetComponentsInChildren<Text>(true);
                        
                        if (buttons.Length >= 2 && texts.Length >= 1)
                        {
                            modalDialog = canvas.gameObject;
                            Debug.Log($"ModalDialogFix: Найден предполагаемый модальный диалог: {canvas.name}");
                            break;
                        }
                    }
                }
            }
            
            // Находим кнопки, если они не назначены
            FindButtons();
            
            // Добавляем обработчики для закрытия модального диалога
            SetupButtonHandlers();
        }
        
        private void FindButtons()
        {
            if (startButton == null)
            {
                startButton = FindButtonByName("Начать", "Start");
            }
            
            if (resetButton == null)
            {
                resetButton = FindButtonByName("Сбросить", "Reset");
            }
            
            if (enablePaintingButton == null)
            {
                enablePaintingButton = FindButtonByName("Включить покраску", "EnablePainting");
            }
        }
        
        private Button FindButtonByName(params string[] possibleNames)
        {
            Button[] allButtons = FindObjectsOfType<Button>();
            foreach (Button button in allButtons)
            {
                foreach (string name in possibleNames)
                {
                    if (button.name.Contains(name) || 
                        (button.GetComponentInChildren<Text>() != null && 
                         button.GetComponentInChildren<Text>().text.Contains(name)))
                    {
                        Debug.Log($"ModalDialogFix: Найдена кнопка: {button.name}");
                        return button;
                    }
                }
            }
            return null;
        }
        
        private void SetupButtonHandlers()
        {
            if (modalDialog == null)
            {
                Debug.LogError("ModalDialogFix: Модальный диалог не найден!");
                return;
            }
            
            // Добавляем обработчик для кнопки "Начать"
            if (startButton != null)
            {
                startButton.onClick.AddListener(() => {
                    Debug.Log("ModalDialogFix: Закрываем модальный диалог по нажатию кнопки 'Начать'");
                    CloseModalDialog();
                });
            }
            
            // Добавляем обработчик для кнопки "Сбросить"
            if (resetButton != null)
            {
                resetButton.onClick.AddListener(() => {
                    Debug.Log("ModalDialogFix: Закрываем модальный диалог по нажатию кнопки 'Сбросить'");
                    CloseModalDialog();
                });
            }
            
            // Добавляем обработчик для кнопки "Включить покраску"
            if (enablePaintingButton != null)
            {
                enablePaintingButton.onClick.AddListener(() => {
                    Debug.Log("ModalDialogFix: Закрываем модальный диалог по нажатию кнопки 'Включить покраску'");
                    CloseModalDialog();
                });
            }
            
            Debug.Log("ModalDialogFix: Обработчики для закрытия модального диалога настроены");
        }
        
        private void CloseModalDialog()
        {
            if (modalDialog != null)
            {
                modalDialog.SetActive(false);
                Debug.Log($"ModalDialogFix: Модальный диалог {modalDialog.name} закрыт");
            }
        }
    }
}