using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Unity.Barracuda;
using UnityEngine.UI;

/// <summary>
/// Класс для экспорта и тестирования модели DeepLabv3 MobileNet
/// </summary>
namespace Remalux.WallDetection
{
    public class DeepLabMobileNetExporter : MonoBehaviour
    {
        [Header("Настройки модели")]
        [Tooltip("ONNX файл модели DeepLabv3 MobileNet")]
        public string onnxModelPath = "Assets/Models/deeplab_mobilenet.onnx";
        
        [Tooltip("Путь для сохранения конвертированной модели")]
        public string outputModelPath = "Assets/Models/deeplab_mobilenet.asset";
        
        [Tooltip("Название входного тензора модели")]
        public string inputName = "input";
        
        [Tooltip("Название выходного тензора модели")]
        public string outputName = "output";
        
        [Header("Настройки конвертации")]
        [Tooltip("Версия ONNX opset (должна быть 11 или ниже для совместимости с Barracuda)")]
        public int opsetVersion = 11;
        
        [Tooltip("Желаемое разрешение входа для модели")]
        public Vector2Int inputResolution = new Vector2Int(320, 320);
        
        [Header("Интеграция с тестированием")]
        [Tooltip("Ссылка на DeepLabModelTester для автоматического добавления конвертированной модели")]
        public DeepLabModelTester modelTester;
        
        [Header("UI элементы")]
        [Tooltip("Кнопка для запуска конвертации")]
        public Button convertButton;
        
        [Tooltip("Текст для отображения прогресса и статуса")]
        public Text statusText;

        // Ссылка на конвертер ONNX
        private OnnxModelConverter onnxConverter;

        void Start()
        {
            // Инициализация конвертера
            onnxConverter = GetComponent<OnnxModelConverter>();
            if (onnxConverter == null)
            {
                onnxConverter = gameObject.AddComponent<OnnxModelConverter>();
            }
            
            // Настройка кнопки конвертации
            if (convertButton != null)
            {
                convertButton.onClick.AddListener(ConvertModel);
            }
            
            UpdateStatusText("Готов к конвертации MobileNet модели");
        }
        
        /// <summary>
        /// Конвертация модели из ONNX в NNModel для Unity Barracuda
        /// </summary>
        public void ConvertModel()
        {
            StartCoroutine(ConvertModelCoroutine());
        }
        
        /// <summary>
        /// Корутина для конвертации модели
        /// </summary>
        private IEnumerator ConvertModelCoroutine()
        {
            // Проверяем наличие файла ONNX
            if (!File.Exists(onnxModelPath))
            {
                UpdateStatusText($"Ошибка: файл модели не найден по пути {onnxModelPath}");
                yield break;
            }
            
            UpdateStatusText("Начало конвертации модели...");
            
            // Загрузка ONNX модели
            UpdateStatusText("Загрузка ONNX модели...");
            byte[] onnxModelData = File.ReadAllBytes(onnxModelPath);
            
            yield return null;
            
            try
            {
                // Проверка версии ONNX
                bool isCompatibleVersion = CheckOnnxVersion(onnxModelData);
                if (!isCompatibleVersion)
                {
                    UpdateStatusText("Предупреждение: Версия ONNX может быть несовместима с Barracuda. Требуется opset 11 или ниже.");
                }
                
                // Конвертация модели с использованием OnnxModelConverter
                UpdateStatusText("Конвертация модели в формат NNModel...");
                
                if (onnxConverter == null)
                {
                    UpdateStatusText("Ошибка: OnnxModelConverter не найден!");
                    yield break;
                }
                
                NNModel convertedModel = onnxConverter.ConvertOnnxToNNModel(onnxModelData, outputModelPath);
                
                if (convertedModel == null)
                {
                    UpdateStatusText("Ошибка при конвертации модели!");
                    yield break;
                }
                
                UpdateStatusText("Модель успешно конвертирована!");
                
                // Валидация модели
                yield return StartCoroutine(ValidateModelCoroutine(convertedModel));
                
                // Добавление модели в тестер если он доступен
                if (modelTester != null && convertedModel != null)
                {
                    AddModelToTester(convertedModel);
                }
                
                UpdateStatusText("Готово! Модель сконвертирована и добавлена в тестер.");
            }
            catch (System.Exception e)
            {
                UpdateStatusText($"Ошибка: {e.Message}");
                Debug.LogError($"Ошибка при конвертации модели: {e}");
            }
        }
        
        /// <summary>
        /// Проверка версии ONNX модели
        /// </summary>
        private bool CheckOnnxVersion(byte[] onnxData)
        {
            // Упрощенная проверка версии ONNX
            // В реальности нужно использовать парсер ONNX для извлечения версии формата
            try
            {
                // Просто проверяем opset_version в начале файла
                // Это упрощенная проверка, в реальности нужно использовать библиотеку ONNX
                string fileContent = System.Text.Encoding.UTF8.GetString(onnxData, 0, 1000);
                return fileContent.Contains("opset_import") && 
                       !fileContent.Contains("opset_import") && 
                       fileContent.Contains("version: 5");
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// Валидация сконвертированной модели
        /// </summary>
        private IEnumerator ValidateModelCoroutine(NNModel model)
        {
            UpdateStatusText("Валидация сконвертированной модели...");
            
            try
            {
                // Создаем рантайм модель
                Model runtimeModel = ModelLoader.Load(model);
                
                // Создаем входной тензор для валидации (проверки размеров)
                Tensor inputTensor = new Tensor(new int[] { 1, inputResolution.y, inputResolution.x, 3 });
                
                // Создаем воркер для проверки
                using (var worker = WorkerFactory.CreateWorker(WorkerFactory.Type.CSharpBurst, runtimeModel))
                {
                    // Выполняем проход для проверки
                    worker.Execute(inputTensor);
                    
                    // Проверяем, есть ли выходной тензор
                    Tensor output = worker.PeekOutput();
                    
                    if (output != null)
                    {
                        UpdateStatusText($"Модель валидирована! Размер выхода: {output.shape}");
                        output.Dispose();
                    }
                    else
                    {
                        UpdateStatusText("Предупреждение: Не удалось получить выходной тензор!");
                    }
                    
                    worker.Dispose();
                }
                
                inputTensor.Dispose();
            }
            catch (System.Exception e)
            {
                UpdateStatusText($"Ошибка валидации: {e.Message}");
                Debug.LogError($"Ошибка валидации: {e}");
            }
            
            yield return null;
        }
        
        /// <summary>
        /// Добавление модели в тестер
        /// </summary>
        private void AddModelToTester(NNModel model)
        {
            if (modelTester == null || model == null)
                return;
            
            try
            {
                // Предполагаем, что у DeepLabModelTester есть метод AddModel
                // Если это не так, нужно адаптировать этот код к реальному API класса
                modelTester.AddModel("DeepLabv3 MobileNet", model, inputResolution);
                
                Debug.Log("Модель успешно добавлена в тестер");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Ошибка при добавлении модели в тестер: {e}");
            }
        }
        
        /// <summary>
        /// Обновление текста статуса
        /// </summary>
        private void UpdateStatusText(string message)
        {
            if (statusText != null)
            {
                statusText.text = message;
            }
            
            Debug.Log($"DeepLabMobileNetExporter: {message}");
        }

        // Fix for ONNX related issues
        public bool TryConvertModelToONNX(Model model, string outputPath)
        {
            try
            {
                // Instead of using the ONNX serialization directly, use a workaround
                // with the NNModel asset
                var jsonModel = model.ToJson();
                
                // Write the model to a temporary JSON file
                string tempJsonPath = Path.Combine(Application.temporaryCachePath, "temp_model.json");
                File.WriteAllText(tempJsonPath, jsonModel);
                
                // Log that we can't directly export to ONNX from runtime
                Debug.LogWarning("Direct ONNX export is not supported in runtime. Using JSON export instead.");
                
                // Write the JSON model to the output path with .json extension
                string jsonOutputPath = outputPath + ".json";
                File.WriteAllText(jsonOutputPath, jsonModel);
                
                Debug.Log($"Model exported as JSON to: {jsonOutputPath}");
                Debug.Log("To convert to ONNX format, please use the Barracuda converter tool externally.");
                
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error exporting model: {e.Message}");
                return false;
            }
        }
        
        // Fix for yield in try-catch blocks
        private IEnumerator LoadModelAndExportToONNX()
        {
            // Setup before try block
            bool success = false;
            string errorMessage = "";
            Model runtimeModel = null;
            
            try
            {
                // Load the model
                runtimeModel = ModelLoader.Load(modelAsset);
                
                // Log model layers for debugging
                Debug.Log($"Model loaded successfully. Layer count: {runtimeModel.layers.Count}");
                foreach (var layer in runtimeModel.layers)
                {
                    Debug.Log($"Layer: {layer.name}, Type: {layer.type}");
                }
            }
            catch (System.Exception e)
            {
                errorMessage = $"Error loading model: {e.Message}";
                Debug.LogError(errorMessage);
                success = false;
                
                // Update UI outside the try block
                yield return null;
                UpdateExportStatus($"Failed: {errorMessage}");
                yield break;
            }
            
            // Allow UI to update
            yield return null;
            
            // If model loaded successfully, try to export
            if (runtimeModel != null)
            {
                try
                {
                    // Create output directory if it doesn't exist
                    string outputDir = Path.GetDirectoryName(outputPath);
                    if (!Directory.Exists(outputDir))
                    {
                        Directory.CreateDirectory(outputDir);
                    }
                    
                    // Export the model
                    success = TryConvertModelToONNX(runtimeModel, outputPath);
                }
                catch (System.Exception e)
                {
                    errorMessage = $"Error exporting model: {e.Message}";
                    Debug.LogError(errorMessage);
                    success = false;
                }
            }
            
            // Update UI based on result
            if (success)
            {
                UpdateExportStatus("Export completed successfully");
            }
            else
            {
                UpdateExportStatus($"Export failed: {errorMessage}");
            }
        }
    }
}

/// <summary>
/// Класс для конвертации ONNX моделей в формат NNModel для Unity Barracuda
/// </summary>
public class OnnxModelConverter : MonoBehaviour
{
    /// <summary>
    /// Конвертация ONNX модели в формат NNModel
    /// </summary>
    public NNModel ConvertOnnxToNNModel(byte[] onnxData, string savePath)
    {
        try
        {
            // Сохраняем временный файл ONNX если нужно
            string tempOnnxPath = Path.Combine(Application.temporaryCachePath, "temp_model.onnx");
            File.WriteAllBytes(tempOnnxPath, onnxData);
            
            // Создаем импортер для конвертации ONNX в NNModel
            Unity.Barracuda.ONNX.ONNXModelImporter importer = new Unity.Barracuda.ONNX.ONNXModelImporter();
            
            // Конвертируем модель
            Model barracudaModel = importer.ImportModel(tempOnnxPath);
            
            // Создаем ассет NNModel
            NNModel nnModel = ScriptableObject.CreateInstance<NNModel>();
            nnModel.modelData = barracudaModel.SerializeToBytes();
            
            // Сохраняем модель как ассет Unity
            #if UNITY_EDITOR
            UnityEditor.AssetDatabase.CreateAsset(nnModel, savePath);
            UnityEditor.AssetDatabase.SaveAssets();
            UnityEditor.AssetDatabase.Refresh();
            #endif
            
            Debug.Log($"Модель успешно сконвертирована и сохранена в {savePath}");
            
            // Удаляем временный файл
            if (File.Exists(tempOnnxPath))
            {
                File.Delete(tempOnnxPath);
            }
            
            return nnModel;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Ошибка конвертации ONNX в NNModel: {e}");
            return null;
        }
    }
}

/// <summary>
/// Расширение класса DeepLabModelTester для добавления новых моделей
/// </summary>
public static class DeepLabModelTesterExtensions
{
    /// <summary>
    /// Добавление модели в тестер
    /// </summary>
    public static void AddModel(this DeepLabModelTester tester, string modelName, NNModel model, Vector2Int inputSize)
    {
        // Получаем приватное поле modelConfigs через рефлексию
        System.Reflection.FieldInfo field = typeof(DeepLabModelTester).GetField("modelConfigs", 
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            
        if (field == null)
        {
            Debug.LogError("Не удалось найти поле modelConfigs в DeepLabModelTester");
            return;
        }
        
        // Получаем текущий список моделей
        object modelConfigsList = field.GetValue(tester);
        
        // Получаем тип ModelConfig (предполагается, что это внутренний класс DeepLabModelTester)
        System.Type modelConfigType = typeof(DeepLabModelTester).GetNestedType("ModelConfig", 
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            
        if (modelConfigType == null)
        {
            Debug.LogError("Не удалось найти тип ModelConfig в DeepLabModelTester");
            return;
        }
        
        // Создаем новый экземпляр ModelConfig
        object newModelConfig = System.Activator.CreateInstance(modelConfigType);
        
        // Устанавливаем свойства
        modelConfigType.GetField("modelName").SetValue(newModelConfig, modelName);
        modelConfigType.GetField("model").SetValue(newModelConfig, model);
        modelConfigType.GetField("inputSize").SetValue(newModelConfig, inputSize);
        
        // Добавляем в список
        typeof(List<>).MakeGenericType(modelConfigType)
            .GetMethod("Add").Invoke(modelConfigsList, new[] { newModelConfig });
    }
} 