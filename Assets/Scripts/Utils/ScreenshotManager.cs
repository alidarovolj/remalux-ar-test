// ScreenshotManager.cs
using UnityEngine;
using System.IO;
using System;

namespace Remalux.AR
{
    /// <summary>
    /// Управляет созданием и сохранением скриншотов
    /// </summary>
    public class ScreenshotManager : MonoBehaviour
    {
        [Header("Настройки скриншотов")]
        [SerializeField] private string screenshotFolderName = "Remalux_Screenshots";
        [SerializeField] private string fileNamePrefix = "PaintedWall_";
        [SerializeField] private GameObject notificationPrefab;
        [SerializeField] private float notificationDuration = 3f;

        private string screenshotPath;
        private GameObject activeNotification;

        private void Awake()
        {
            // Создаем путь для сохранения скриншотов
            InitializeScreenshotPath();
        }

        /// <summary>
        /// Инициализирует путь для сохранения скриншотов
        /// </summary>
        private void InitializeScreenshotPath()
        {
            // Определяем путь для сохранения в зависимости от платформы
            string basePath = "";

#if UNITY_ANDROID
                basePath = $"{Application.persistentDataPath}/";
#elif UNITY_IOS
            basePath = $"{Application.persistentDataPath}/";
#else
                basePath = $"{Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)}/";
#endif

            screenshotPath = $"{basePath}{screenshotFolderName}/";

            // Создаем директорию, если она не существует
            if (!Directory.Exists(screenshotPath))
            {
                Directory.CreateDirectory(screenshotPath);
                Debug.Log($"Создана директория для скриншотов: {screenshotPath}");
            }
        }

        /// <summary>
        /// Делает и сохраняет скриншот
        /// </summary>
        public void CaptureScreenshot()
        {
            // Формируем имя файла с датой и временем
            string fileName = $"{fileNamePrefix}{DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss")}.png";
            string fullPath = $"{screenshotPath}{fileName}";

            // Делаем скриншот
            ScreenCapture.CaptureScreenshot(fullPath);

            Debug.Log($"Скриншот сохранен: {fullPath}");

            // Показываем уведомление
            ShowNotification($"Скриншот сохранен");
        }

        /// <summary>
        /// Делает и сохраняет скриншот с указанным разрешением
        /// </summary>
        public void CaptureScreenshotHighResolution(int width, int height)
        {
            // Формируем имя файла с датой и временем
            string fileName = $"{fileNamePrefix}{DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss")}_HD.png";
            string fullPath = $"{screenshotPath}{fileName}";

            // Получаем текущее разрешение
            int currentWidth = Screen.width;
            int currentHeight = Screen.height;

            // Создаем текстуру для скриншота
            RenderTexture rt = new RenderTexture(width, height, 24);
            Camera.main.targetTexture = rt;

            // Создаем текстуру для сохранения пикселей
            Texture2D screenShot = new Texture2D(width, height, TextureFormat.RGB24, false);

            // Делаем скриншот
            Camera.main.Render();
            RenderTexture.active = rt;
            screenShot.ReadPixels(new Rect(0, 0, width, height), 0, 0);

            // Очищаем ресурсы
            Camera.main.targetTexture = null;
            RenderTexture.active = null;
            Destroy(rt);

            // Сохраняем скриншот
            byte[] bytes = screenShot.EncodeToPNG();
            System.IO.File.WriteAllBytes(fullPath, bytes);

            Debug.Log($"HD скриншот сохранен: {fullPath}");

            // Показываем уведомление
            ShowNotification($"HD скриншот сохранен");
        }

        /// <summary>
        /// Показывает уведомление о сохранении скриншота
        /// </summary>
        private void ShowNotification(string message)
        {
            // Если есть префаб уведомления, показываем его
            if (notificationPrefab != null)
            {
                // Удаляем предыдущее уведомление, если оно есть
                if (activeNotification != null)
                {
                    Destroy(activeNotification);
                }

                // Создаем новое уведомление
                activeNotification = Instantiate(notificationPrefab, transform);

                // Находим Text компонент для отображения сообщения
                UnityEngine.UI.Text textComponent = activeNotification.GetComponentInChildren<UnityEngine.UI.Text>();
                if (textComponent != null)
                {
                    textComponent.text = message;
                }

                // Автоматически скрываем уведомление через указанное время
                Destroy(activeNotification, notificationDuration);
            }
        }

        /// <summary>
        /// Открывает папку со скриншотами
        /// </summary>
        public void OpenScreenshotFolder()
        {
#if UNITY_EDITOR || UNITY_STANDALONE
            System.Diagnostics.Process.Start(screenshotPath);
#elif UNITY_ANDROID
                // На Android можно открыть галерею через Intent
                AndroidJavaClass intentClass = new AndroidJavaClass("android.content.Intent");
                AndroidJavaObject intent = new AndroidJavaObject("android.content.Intent");
                intent.Call<AndroidJavaObject>("setAction", intentClass.GetStatic<string>("ACTION_VIEW"));
                AndroidJavaClass uriClass = new AndroidJavaClass("android.net.Uri");
                AndroidJavaObject uri = uriClass.CallStatic<AndroidJavaObject>("parse", "file://" + screenshotPath);
                intent.Call<AndroidJavaObject>("setDataAndType", uri, "image/*");
                
                AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                currentActivity.Call("startActivity", intent);
#endif
        }
    }
}