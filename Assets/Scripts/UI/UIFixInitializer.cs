using UnityEngine;

namespace Remalux.AR
{
    /// <summary>
    /// Инициализирует исправление UI при запуске приложения
    /// </summary>
    public static class UIFixInitializer
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            Debug.Log("UIFixInitializer: Инициализация исправления UI");
            
            // Создаем объект с компонентом UIFixApplier
            GameObject fixApplierObj = new GameObject("UIFixApplier");
            fixApplierObj.AddComponent<UIFixApplier>();
            Object.DontDestroyOnLoad(fixApplierObj);
        }
    }
} 