using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Unity.Barracuda;
using UnityEngine.UI;
using TMPro;

namespace WallDetection
{
    /// <summary>
    /// Инструмент для проверки совместимости ONNX моделей с Barracuda
    /// и определения ключевых параметров модели без запуска полной сцены
    /// </summary>
    public class BarracudaModelChecker : MonoBehaviour
    {
        [Header("Модели для проверки")]
        [SerializeField] private NNModel[] modelsToCheck;
        [SerializeField] private string[] onnxFilePaths;
        
        [Header("UI элементы")]
        [SerializeField] private Transform resultsContainer;
        [SerializeField] private TextMeshProUGUI resultTextPrefab;
        [SerializeField] private Button checkButton;
        [SerializeField] private Button clearButton;
        [SerializeField] private Button saveReportButton;
        [SerializeField] private TextMeshProUGUI statusText;
        
        [Header("Дополнительные настройки")]
        [SerializeField] private bool checkAtStart = false;
        [SerializeField] private bool saveReportAutomatically = false;
        [SerializeField] private string reportFilePath = "model_compatibility_report.txt";
        
        private List<ModelCheckResult> checkResults = new List<ModelCheckResult>();
        
        [Serializable]
        private class ModelCheckResult
        {
            public string modelName;
            public string modelPath;
            public bool isCompatible;
            public string errorMessage;
            public int inputHeight;
            public int inputWidth;
            public int outputHeight;
            public int outputWidth;
            public int outputChannels;
            public string[] inputNames;
            public string[] outputNames;
            public string barracudaVersion;
            public string onnxVersion;
            public long modelSizeInBytes;
            public string[] supportedBackends;
        }
        
        private void Start()
        {
            if (checkButton != null)
                checkButton.onClick.AddListener(CheckAllModels);
                
            if (clearButton != null)
                clearButton.onClick.AddListener(ClearResults);
                
            if (saveReportButton != null)
                saveReportButton.onClick.AddListener(SaveReport);
                
            if (checkAtStart)
                CheckAllModels();
        }
        
        public void CheckAllModels()
        {
            StartCoroutine(CheckAllModelsCoroutine());
        }
        
        private IEnumerator CheckAllModelsCoroutine()
        {
            checkResults.Clear();
            ClearUI();
            
            UpdateStatus("Проверка моделей NNModel...");
            
            // Проверка моделей из списка NNModel
            if (modelsToCheck != null && modelsToCheck.Length > 0)
            {
                for (int i = 0; i < modelsToCheck.Length; i++)
                {
                    yield return CheckModelCompatibility(modelsToCheck[i], null);
                }
            }
            
            UpdateStatus("Проверка моделей ONNX из файловой системы...");
            
            // Проверка моделей из списка файловых путей
            if (onnxFilePaths != null && onnxFilePaths.Length > 0)
            {
                for (int i = 0; i < onnxFilePaths.Length; i++)
                {
                    yield return CheckOnnxFileCompatibility(onnxFilePaths[i]);
                }
            }
            
            UpdateStatus($"Проверка завершена. Проверено моделей: {checkResults.Count}");
            
            // Автоматическое сохранение отчета, если нужно
            if (saveReportAutomatically)
                SaveReport();
        }
        
        private IEnumerator CheckModelCompatibility(NNModel nnModel, string filePath)
        {
            ModelCheckResult result = new ModelCheckResult();
            
            try
            {
                result.modelName = nnModel != null ? nnModel.name : Path.GetFileName(filePath);
                result.modelPath = filePath ?? "NNModel Asset";
                result.barracudaVersion = "Unity Barracuda " + Application.unityVersion;
                
                // Загрузка модели
                Model model = null;
                
                if (nnModel != null)
                {
                    model = ModelLoader.Load(nnModel);
                }
                else if (!string.IsNullOrEmpty(filePath))
                {
                    model = ModelLoader.Load(filePath);
                }
                
                if (model == null)
                {
                    throw new Exception("Не удалось загрузить модель");
                }
                
                // Проверка совместимости с Barracuda
                result.isCompatible = true;
                
                // Сохранение информации о модели
                result.inputNames = model.inputs.ToArray(x => x.name);
                result.outputNames = model.outputs.ToArray();
                
                // Получение размеров входа/выхода
                if (model.inputs.Count > 0)
                {
                    var input = model.inputs[0];
                    if (input.shape.Length >= 3)
                    {
                        result.inputHeight = input.shape[2];
                        result.inputWidth = input.shape[3];
                    }
                }
                
                // Определение размеров выхода сложнее, это приблизительная оценка
                // для стандартных моделей сегментации
                var outputLayers = model.layers.FindAll(l => model.outputs.Contains(l.name));
                if (outputLayers.Count > 0 && outputLayers[0].datasets.Count > 0)
                {
                    var outputShape = outputLayers[0].datasets[0].shape;
                    if (outputShape.Length >= 3)
                    {
                        result.outputHeight = outputShape[1];
                        result.outputWidth = outputShape[2];
                        result.outputChannels = outputShape[0];
                    }
                }
                
                // Проверка поддерживаемых бэкендов
                List<string> supportedBackends = new List<string>();
                foreach (WorkerFactory.Type backendType in Enum.GetValues(typeof(WorkerFactory.Type)))
                {
                    if (WorkerFactory.IsType(backendType, model))
                        supportedBackends.Add(backendType.ToString());
                }
                result.supportedBackends = supportedBackends.ToArray();
                
                // Размер модели (приблизительно)
                result.modelSizeInBytes = nnModel != null ? nnModel.modelAsset.bytes.Length : new FileInfo(filePath).Length;
                
                // Попытка определить версию ONNX
                result.onnxVersion = "Unknown";
                foreach (var layer in model.layers)
                {
                    if (layer.flags.HasFlag(Layer.Flags.Preserve) && layer.name.Contains("opset"))
                    {
                        result.onnxVersion = layer.name;
                        break;
                    }
                }
            }
            catch (Exception e)
            {
                result.isCompatible = false;
                result.errorMessage = e.Message;
                Debug.LogError($"Ошибка при проверке модели {result.modelName}: {e.Message}");
            }
            
            checkResults.Add(result);
            AddResultToUI(result);
            
            yield return null;
        }
        
        private IEnumerator CheckOnnxFileCompatibility(string filePath)
        {
            if (File.Exists(filePath))
            {
                yield return CheckModelCompatibility(null, filePath);
            }
            else
            {
                ModelCheckResult result = new ModelCheckResult();
                result.modelName = Path.GetFileName(filePath);
                result.modelPath = filePath;
                result.isCompatible = false;
                result.errorMessage = "Файл не найден";
                
                checkResults.Add(result);
                AddResultToUI(result);
                
                Debug.LogError($"Файл модели не найден: {filePath}");
                yield return null;
            }
        }
        
        private void AddResultToUI(ModelCheckResult result)
        {
            if (resultsContainer == null || resultTextPrefab == null)
                return;
                
            TextMeshProUGUI resultText = Instantiate(resultTextPrefab, resultsContainer);
            
            string statusIcon = result.isCompatible ? "✅" : "❌";
            string backendInfo = result.supportedBackends != null && result.supportedBackends.Length > 0 ? 
                                string.Join(", ", result.supportedBackends) : "Неизвестно";
            
            resultText.text = $"{statusIcon} <b>{result.modelName}</b>\n" +
                              $"Совместимость: {(result.isCompatible ? "Да" : "Нет")}\n" +
                              $"Размер: {FormatFileSize(result.modelSizeInBytes)}\n" +
                              $"Вход: {result.inputWidth}x{result.inputHeight}\n" +
                              $"Выход: {result.outputWidth}x{result.outputHeight}x{result.outputChannels}\n" +
                              $"Бэкенды: {backendInfo}\n" +
                              (string.IsNullOrEmpty(result.errorMessage) ? "" : $"Ошибка: {result.errorMessage}\n");
        }
        
        private string FormatFileSize(long byteCount)
        {
            if (byteCount < 1024)
                return $"{byteCount} B";
            if (byteCount < 1024 * 1024)
                return $"{byteCount / 1024f:F1} KB";
            if (byteCount < 1024 * 1024 * 1024)
                return $"{byteCount / (1024f * 1024f):F1} MB";
            
            return $"{byteCount / (1024f * 1024f * 1024f):F1} GB";
        }
        
        private void ClearUI()
        {
            if (resultsContainer == null)
                return;
                
            foreach (Transform child in resultsContainer)
            {
                Destroy(child.gameObject);
            }
        }
        
        private void ClearResults()
        {
            checkResults.Clear();
            ClearUI();
            UpdateStatus("Результаты очищены");
        }
        
        private void SaveReport()
        {
            if (checkResults.Count == 0)
            {
                UpdateStatus("Нет результатов для сохранения");
                return;
            }
            
            try
            {
                string reportPath = Path.Combine(Application.persistentDataPath, reportFilePath);
                using (StreamWriter writer = new StreamWriter(reportPath))
                {
                    writer.WriteLine("# Отчет о совместимости моделей с Barracuda");
                    writer.WriteLine($"Дата проверки: {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}");
                    writer.WriteLine($"Unity версия: {Application.unityVersion}");
                    writer.WriteLine("-----------------------------------");
                    
                    foreach (var result in checkResults)
                    {
                        writer.WriteLine($"\n## Модель: {result.modelName}");
                        writer.WriteLine($"Путь: {result.modelPath}");
                        writer.WriteLine($"Совместимость: {(result.isCompatible ? "Да" : "Нет")}");
                        
                        if (!result.isCompatible)
                            writer.WriteLine($"Ошибка: {result.errorMessage}");
                            
                        writer.WriteLine($"Размер модели: {FormatFileSize(result.modelSizeInBytes)}");
                        writer.WriteLine($"Размер входа: {result.inputWidth}x{result.inputHeight}");
                        writer.WriteLine($"Размер выхода: {result.outputWidth}x{result.outputHeight}x{result.outputChannels}");
                        
                        if (result.inputNames != null && result.inputNames.Length > 0)
                            writer.WriteLine($"Входные узлы: {string.Join(", ", result.inputNames)}");
                            
                        if (result.outputNames != null && result.outputNames.Length > 0)
                            writer.WriteLine($"Выходные узлы: {string.Join(", ", result.outputNames)}");
                            
                        if (result.supportedBackends != null && result.supportedBackends.Length > 0)
                            writer.WriteLine($"Поддерживаемые бэкенды: {string.Join(", ", result.supportedBackends)}");
                            
                        writer.WriteLine("-----------------------------------");
                    }
                }
                
                UpdateStatus($"Отчет сохранен: {reportPath}");
                Debug.Log($"Отчет о совместимости моделей сохранен: {reportPath}");
            }
            catch (Exception e)
            {
                UpdateStatus($"Ошибка при сохранении: {e.Message}");
                Debug.LogError($"Ошибка при сохранении отчета: {e.Message}");
            }
        }
        
        private void UpdateStatus(string message)
        {
            if (statusText != null)
                statusText.text = message;
                
            Debug.Log(message);
        }
        
        /// <summary>
        /// Статический метод для проверки совместимости модели
        /// Может использоваться из других классов или из редактора
        /// </summary>
        public static bool CheckModelCompatibilityStatic(NNModel model, out string errorMessage)
        {
            errorMessage = "";
            
            try
            {
                if (model == null)
                {
                    errorMessage = "Модель не указана";
                    return false;
                }
                
                // Загрузка модели через Barracuda
                Model runtimeModel = ModelLoader.Load(model);
                
                // Если модель загружена без исключений, проверяем основные параметры
                if (runtimeModel == null)
                {
                    errorMessage = "Не удалось загрузить модель";
                    return false;
                }
                
                // Проверка наличия входов и выходов
                if (runtimeModel.inputs.Count == 0)
                {
                    errorMessage = "Модель не имеет входных узлов";
                    return false;
                }
                
                if (runtimeModel.outputs.Length == 0)
                {
                    errorMessage = "Модель не имеет выходных узлов";
                    return false;
                }
                
                // Проверка создания воркера (может выполняться на поддерживаемом бэкенде)
                bool canCreateWorker = false;
                
                foreach (WorkerFactory.Type backendType in Enum.GetValues(typeof(WorkerFactory.Type)))
                {
                    if (WorkerFactory.IsType(backendType, runtimeModel))
                    {
                        try
                        {
                            using (var worker = WorkerFactory.CreateWorker(backendType, runtimeModel))
                            {
                                // Если воркер создан успешно, модель совместима с этим бэкендом
                                canCreateWorker = true;
                                break;
                            }
                        }
                        catch (Exception workerEx)
                        {
                            Debug.LogWarning($"Не удалось создать воркер для бэкенда {backendType}: {workerEx.Message}");
                        }
                    }
                }
                
                if (!canCreateWorker)
                {
                    errorMessage = "Модель не может быть запущена ни на одном из доступных бэкендов";
                    return false;
                }
                
                return true;
            }
            catch (Exception e)
            {
                errorMessage = e.Message;
                return false;
            }
        }
    }
} 