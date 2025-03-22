using UnityEngine;

namespace Remalux.AR
{
    /// <summary>
    /// Автоматически добавляет компонент FixedStartScreenHandler в сцену
    /// </summary>
    public static class FixedStartScreenHandlerApplier
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            Debug.Log("FixedStartScreenHandlerApplier: Инициализация исправления начального экрана (улучшенная версия)");
            
            // Создаем объект с компонентом FixedStartScreenHandler
            GameObject fixObj = new GameObject("FixedStartScreenHandler");
            fixObj.AddComponent<FixedStartScreenHandler>();
            Object.DontDestroyOnLoad(fixObj);
            
            // Отключаем предыдущие исправления, которые могут конфликтовать
            DisablePreviousFixes();
        }
        
        private static void DisablePreviousFixes()
        {
            // Находим и отключаем предыдущие исправления
            string[] fixNames = new string[] {
                "DirectUIFix", 
                "StartScreenFix", 
                "ModalDialogFix"
            };
            
            foreach (string fixName in fixNames)
            {
                GameObject fixObj = GameObject.Find(fixName);
                if (fixObj != null)
                {
                    Debug.Log($"FixedStartScreenHandlerApplier: Отключаем предыдущее исправление {fixName}");
                    fixObj.SetActive(false);
                }
            }
        }
    }
} 