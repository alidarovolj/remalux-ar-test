using UnityEngine;
using UnityEngine.EventSystems;

namespace Remalux.AR
{
    public class SingleEventSystem : EventSystem
    {
        private static SingleEventSystem instance;

        protected override void Awake()
        {
            if (instance != null && instance != this)
            {
                Debug.LogWarning($"Найден дополнительный EventSystem '{gameObject.name}'. Удаляем...");
                Destroy(gameObject);
                return;
            }

            instance = this;

            // Проверяем наличие input module
            if (GetComponent<BaseInputModule>() == null)
            {
                Debug.LogWarning("Input Module не найден. Добавляем StandaloneInputModule.");
                gameObject.AddComponent<StandaloneInputModule>();
            }

            // Проверяем наличие других EventSystem в сцене
            CheckForDuplicateEventSystems();

            base.Awake();
        }

        protected override void OnEnable()
        {
            if (instance != null && instance != this)
            {
                Debug.LogWarning($"Найден дополнительный EventSystem '{gameObject.name}'. Удаляем...");
                Destroy(gameObject);
                return;
            }

            // Проверяем наличие других EventSystem в сцене
            CheckForDuplicateEventSystems();

            base.OnEnable();
        }

        protected override void OnDisable()
        {
            if (instance == this)
            {
                instance = null;
            }
            base.OnDisable();
        }

        private void CheckForDuplicateEventSystems()
        {
            // Проверяем наличие других EventSystem в сцене
            var systems = FindObjectsByType<EventSystem>(FindObjectsSortMode.None);
            if (systems.Length > 1)
            {
                foreach (var sys in systems)
                {
                    if (sys != null && sys != this && sys.gameObject != null)
                    {
                        Debug.LogWarning($"Найден дополнительный EventSystem '{sys.gameObject.name}'. Удаляем...");
                        DestroyImmediate(sys.gameObject);
                    }
                }
            }
        }
    }
}