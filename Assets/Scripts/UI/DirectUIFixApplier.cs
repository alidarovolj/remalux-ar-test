using UnityEngine;

namespace Remalux.AR
{
    /// <summary>
    /// Автоматически добавляет компонент DirectUIFix в сцену
    /// </summary>
    public static class DirectUIFixApplier
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            Debug.Log("DirectUIFixApplier: Инициализация прямого исправления UI");
            
            // Создаем объект с компонентом DirectUIFix
            GameObject fixObj = new GameObject("DirectUIFix");
            fixObj.AddComponent<DirectUIFix>();
            Object.DontDestroyOnLoad(fixObj);
        }
    }
} 