using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;
using Unity.Barracuda;

namespace WallDetection
{
    /// <summary>
    /// Класс для настройки тестовой сцены проверки совместимости моделей с Barracuda
    /// </summary>
    public class ModelCheckerScene : MonoBehaviour
    {
        [Header("Ссылки на компоненты")]
        [SerializeField] private BarracudaModelChecker modelChecker;
        [SerializeField] private Button scanDirectoryButton;
        [SerializeField] private TMP_InputField directoryPathInput;
        [SerializeField] private Button exportResultsButton;
        [SerializeField] private TextMeshProUGUI infoText;
        
        [Header("Основные модели из проекта")]
        [SerializeField] private NNModel[] projectModels;
        
        private void Start()
        {
            SetupUI();
            DisplayProjectInfo();
        }
        
        private void SetupUI()
        {
            if (scanDirectoryButton != null)
                scanDirectoryButton.onClick.AddListener(ScanDirectory);
                
            if (exportResultsButton != null)
                exportResultsButton.onClick.AddListener(ExportResults);
                
            if (directoryPathInput != null)
                directoryPathInput.text = Application.persistentDataPath;
                
            AddDefaultModelsToChecker();
        }
        
        private void AddDefaultModelsToChecker()
        {
            if (modelChecker == null || projectModels == null)
                return;
                
            // Передача моделей из проекта в компонент проверки
            var modelsToCheck = new List<NNModel>();
            
            foreach (var model in projectModels)
            {
                if (model != null)
                    modelsToCheck.Add(model);
            }
            
            // Поиск всех моделей в папке Assets/Models
            string modelsDirectory = Path.Combine(Application.dataPath, "Models");
            if (Directory.Exists(modelsDirectory))
            {
                string[] onnxFiles = Directory.GetFiles(modelsDirectory, "*.onnx", SearchOption.AllDirectories);
                
                if (onnxFiles.Length > 0)
                {
                    SetInfoText($"Найдено {onnxFiles.Length} ONNX файлов в папке Models");
                    
                    // Передаем пути к файлам ONNX в компонент проверки
                    modelChecker.SendMessage("SetOnnxFilePaths", onnxFiles);
                }
            }
            
            if (modelsToCheck.Count > 0)
            {
                SetInfoText($"Найдено {modelsToCheck.Count} моделей в проекте");
                
                // Передаем модели в компонент проверки
                modelChecker.SendMessage("SetModelsToCheck", modelsToCheck.ToArray());
            }
        }
        
        private void ScanDirectory()
        {
            if (directoryPathInput == null || string.IsNullOrEmpty(directoryPathInput.text))
                return;
                
            string directoryPath = directoryPathInput.text;
            
            if (!Directory.Exists(directoryPath))
            {
                SetInfoText($"Директория не существует: {directoryPath}");
                return;
            }
            
            SetInfoText($"Сканирование директории: {directoryPath}...");
            
            try
            {
                string[] onnxFiles = Directory.GetFiles(directoryPath, "*.onnx", SearchOption.AllDirectories);
                
                if (onnxFiles.Length > 0)
                {
                    SetInfoText($"Найдено {onnxFiles.Length} ONNX файлов");
                    
                    // Передаем пути к файлам ONNX в компонент проверки
                    modelChecker.SendMessage("SetOnnxFilePaths", onnxFiles);
                    
                    // Запускаем проверку
                    modelChecker.SendMessage("CheckAllModels");
                }
                else
                {
                    SetInfoText($"ONNX файлы не найдены в {directoryPath}");
                }
            }
            catch (System.Exception e)
            {
                SetInfoText($"Ошибка при сканировании: {e.Message}");
                Debug.LogError($"Ошибка при сканировании директории: {e.Message}");
            }
        }
        
        private void ExportResults()
        {
            if (modelChecker != null)
            {
                modelChecker.SendMessage("SaveReport");
                SetInfoText("Экспорт результатов выполнен");
            }
        }
        
        private void SetInfoText(string message)
        {
            if (infoText != null)
                infoText.text = message;
                
            Debug.Log(message);
        }
        
        private void DisplayProjectInfo()
        {
            SetInfoText(
                $"Проверка совместимости моделей с Barracuda\n" +
                $"Unity версия: {Application.unityVersion}\n" +
                $"Платформа: {Application.platform}\n" +
                $"Для начала проверки нажмите кнопку 'Сканировать директорию' или добавьте модели вручную"
            );
        }
    }
} 