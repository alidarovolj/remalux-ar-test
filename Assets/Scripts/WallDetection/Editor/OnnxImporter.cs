using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Unity.Barracuda;

namespace WallDetection.Editor
{
    /// <summary>
    /// Инструмент для автоматической конвертации ONNX моделей в формат NNModel
    /// с поддержкой пакетной обработки и проверкой совместимости
    /// </summary>
    public class OnnxImporter : EditorWindow
    {
        private string sourceDirectory = "";
        private string destinationDirectory = "Assets/Models";
        private bool deleteOnnxAfterImport = false;
        private bool checkCompatibilityAfterImport = true;
        private bool showAdvancedOptions = false;
        private bool generatePrefabs = false;
        private bool recursiveSearch = true;
        private string modelNameFilter = "*.onnx";
        private Vector2 scrollPosition;
        private List<ConversionItem> conversionQueue = new List<ConversionItem>();
        private bool isProcessing = false;
        private string statusMessage = "";
        private ModelImporterMemoryLayout memoryLayout = ModelImporterMemoryLayout.ChannelsLast;
        private int logVerbosity = 0;
        private bool overwriteExisting = true;
        
        [MenuItem("Remalux AR/ONNX to NNModel Converter")]
        public static void ShowWindow()
        {
            EditorWindow.GetWindow<OnnxImporter>("ONNX Importer");
        }
        
        private class ConversionItem
        {
            public string SourcePath;
            public string DestinationPath;
            public bool IsConverted;
            public bool IsCompatible;
            public string ErrorMessage;
            public string ModelAssetPath;
        }
        
        private void OnGUI()
        {
            GUILayout.Label("ONNX to NNModel Converter", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            EditorGUILayout.LabelField("Выберите директорию с ONNX моделями:", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            sourceDirectory = EditorGUILayout.TextField("Директория с ONNX:", sourceDirectory);
            if (GUILayout.Button("Обзор", GUILayout.Width(80)))
            {
                string dir = EditorUtility.OpenFolderPanel("Выберите директорию с ONNX моделями", "", "");
                if (!string.IsNullOrEmpty(dir))
                {
                    sourceDirectory = dir;
                    ScanSourceDirectory();
                }
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            destinationDirectory = EditorGUILayout.TextField("Директория NNModel:", destinationDirectory);
            if (GUILayout.Button("Обзор", GUILayout.Width(80)))
            {
                string dir = EditorUtility.OpenFolderPanel("Выберите директорию для NNModel", "Assets", "");
                if (!string.IsNullOrEmpty(dir))
                {
                    // Преобразуем абсолютный путь в путь относительно проекта
                    string projectPath = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
                    if (dir.StartsWith(projectPath))
                    {
                        destinationDirectory = dir.Substring(projectPath.Length + 1);
                    }
                    else
                    {
                        destinationDirectory = dir;
                    }
                }
            }
            EditorGUILayout.EndHorizontal();
            
            showAdvancedOptions = EditorGUILayout.Foldout(showAdvancedOptions, "Дополнительные настройки");
            
            if (showAdvancedOptions)
            {
                EditorGUI.indentLevel++;
                
                modelNameFilter = EditorGUILayout.TextField("Фильтр файлов:", modelNameFilter);
                recursiveSearch = EditorGUILayout.Toggle("Рекурсивный поиск:", recursiveSearch);
                overwriteExisting = EditorGUILayout.Toggle("Перезаписывать существующие:", overwriteExisting);
                deleteOnnxAfterImport = EditorGUILayout.Toggle("Удалять ONNX после импорта:", deleteOnnxAfterImport);
                checkCompatibilityAfterImport = EditorGUILayout.Toggle("Проверять совместимость:", checkCompatibilityAfterImport);
                generatePrefabs = EditorGUILayout.Toggle("Создавать префабы моделей:", generatePrefabs);
                
                EditorGUILayout.LabelField("Расположение данных в памяти:");
                memoryLayout = (ModelImporterMemoryLayout)EditorGUILayout.EnumPopup(memoryLayout);
                
                logVerbosity = EditorGUILayout.IntSlider("Уровень логирования:", logVerbosity, 0, 3);
                
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.Space();
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Сканировать директорию"))
            {
                ScanSourceDirectory();
            }
            
            GUI.enabled = conversionQueue.Count > 0 && !isProcessing;
            if (GUILayout.Button("Конвертировать все"))
            {
                ConvertAllModels();
            }
            
            GUI.enabled = !isProcessing;
            if (GUILayout.Button("Очистить"))
            {
                conversionQueue.Clear();
                statusMessage = "Очередь конвертации очищена";
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
            
            if (!string.IsNullOrEmpty(statusMessage))
            {
                EditorGUILayout.HelpBox(statusMessage, MessageType.Info);
            }
            
            EditorGUILayout.Space();
            
            // Отображение списка моделей для конвертации
            if (conversionQueue.Count > 0)
            {
                EditorGUILayout.LabelField($"Модели для конвертации ({conversionQueue.Count}):", EditorStyles.boldLabel);
                
                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
                
                for (int i = 0; i < conversionQueue.Count; i++)
                {
                    var item = conversionQueue[i];
                    
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(Path.GetFileName(item.SourcePath), EditorStyles.boldLabel);
                    
                    if (item.IsConverted)
                    {
                        if (item.IsCompatible)
                        {
                            GUILayout.Label("✅ Готово", EditorStyles.boldLabel);
                        }
                        else
                        {
                            GUILayout.Label("⚠️ Конвертировано, но несовместимо", EditorStyles.boldLabel);
                        }
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(item.ErrorMessage))
                        {
                            GUILayout.Label("❌ Ошибка", EditorStyles.boldLabel);
                        }
                        else
                        {
                            GUILayout.Label("⏳ Ожидает", EditorStyles.boldLabel);
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                    
                    EditorGUILayout.LabelField($"Исходный файл: {item.SourcePath}");
                    EditorGUILayout.LabelField($"Целевой файл: {item.DestinationPath}");
                    
                    if (item.IsConverted && !string.IsNullOrEmpty(item.ModelAssetPath))
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField($"NNModel: {item.ModelAssetPath}");
                        
                        if (GUILayout.Button("Выбрать", GUILayout.Width(80)))
                        {
                            Selection.activeObject = AssetDatabase.LoadAssetAtPath<NNModel>(item.ModelAssetPath);
                        }
                        
                        if (GUILayout.Button("Ping", GUILayout.Width(80)))
                        {
                            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<NNModel>(item.ModelAssetPath));
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                    
                    if (!string.IsNullOrEmpty(item.ErrorMessage))
                    {
                        EditorGUILayout.HelpBox(item.ErrorMessage, MessageType.Error);
                    }
                    
                    GUI.enabled = !item.IsConverted && !isProcessing;
                    if (GUILayout.Button("Конвертировать"))
                    {
                        ConvertModel(item);
                    }
                    GUI.enabled = true;
                    
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.Space();
                }
                
                EditorGUILayout.EndScrollView();
            }
        }
        
        private void ScanSourceDirectory()
        {
            if (string.IsNullOrEmpty(sourceDirectory) || !Directory.Exists(sourceDirectory))
            {
                statusMessage = "Пожалуйста, укажите корректную директорию с ONNX моделями";
                return;
            }
            
            conversionQueue.Clear();
            SearchOption searchOption = recursiveSearch ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            
            try
            {
                string[] files = Directory.GetFiles(sourceDirectory, modelNameFilter, searchOption)
                    .Where(f => Path.GetExtension(f).ToLower() == ".onnx")
                    .ToArray();
                
                if (files.Length == 0)
                {
                    statusMessage = $"В указанной директории не найдено ONNX моделей по маске '{modelNameFilter}'";
                    return;
                }
                
                foreach (var file in files)
                {
                    string relativePath = file.Substring(sourceDirectory.Length).TrimStart(Path.DirectorySeparatorChar);
                    string destPath = Path.Combine(destinationDirectory, relativePath);
                    destPath = Path.ChangeExtension(destPath, ".onnx");
                    
                    conversionQueue.Add(new ConversionItem
                    {
                        SourcePath = file,
                        DestinationPath = destPath,
                        IsConverted = false,
                        ErrorMessage = ""
                    });
                }
                
                statusMessage = $"Найдено {files.Length} ONNX моделей для конвертации";
            }
            catch (Exception e)
            {
                statusMessage = $"Ошибка при сканировании директории: {e.Message}";
                Debug.LogError($"Ошибка при сканировании директории: {e.Message}");
            }
        }
        
        private void ConvertAllModels()
        {
            if (conversionQueue.Count == 0)
            {
                statusMessage = "Нет моделей для конвертации";
                return;
            }
            
            EditorCoroutine.Start(ConvertAllModelsCoroutine());
        }
        
        private IEnumerator<YieldInstruction> ConvertAllModelsCoroutine()
        {
            isProcessing = true;
            int successCount = 0;
            int errorCount = 0;
            
            for (int i = 0; i < conversionQueue.Count; i++)
            {
                var item = conversionQueue[i];
                if (!item.IsConverted)
                {
                    statusMessage = $"Конвертация {i + 1} из {conversionQueue.Count}: {Path.GetFileName(item.SourcePath)}";
                    Repaint();
                    
                    ConvertModel(item);
                    
                    if (item.IsConverted)
                        successCount++;
                    else
                        errorCount++;
                    
                    // Пауза между конвертациями, чтобы обновить UI
                    yield return new WaitForSeconds(0.1f);
                }
            }
            
            statusMessage = $"Конвертация завершена. Успешно: {successCount}, С ошибками: {errorCount}";
            isProcessing = false;
            Repaint();
        }
        
        private void ConvertModel(ConversionItem item)
        {
            if (item.IsConverted)
                return;
                
            try
            {
                // Проверяем существование исходного файла
                if (!File.Exists(item.SourcePath))
                {
                    item.ErrorMessage = "Исходный файл не найден";
                    return;
                }
                
                // Создаем директории для целевого файла
                string destDirectory = Path.GetDirectoryName(item.DestinationPath);
                if (!Directory.Exists(destDirectory))
                {
                    Directory.CreateDirectory(destDirectory);
                }
                
                // Проверяем существование целевого файла
                string assetPath = item.DestinationPath;
                if (!assetPath.StartsWith("Assets/"))
                {
                    assetPath = "Assets/" + assetPath;
                }
                
                // Проверяем, существует ли уже ONNX файл в директории проекта
                bool onnxFileExists = File.Exists(assetPath);
                
                // Если ONNX файл уже существует и мы не хотим перезаписывать
                if (onnxFileExists && !overwriteExisting)
                {
                    // Проверяем, существует ли соответствующий NNModel
                    string nnModelPath = Path.ChangeExtension(assetPath, ".asset");
                    if (File.Exists(nnModelPath))
                    {
                        item.IsConverted = true;
                        item.ModelAssetPath = nnModelPath;
                        item.ErrorMessage = "Модель уже существует (пропущено)";
                        return;
                    }
                }
                
                // Копируем ONNX файл в проект, если нужно
                if (!onnxFileExists || overwriteExisting)
                {
                    File.Copy(item.SourcePath, assetPath, overwriteExisting);
                    AssetDatabase.ImportAsset(assetPath);
                }
                
                // Ждем, пока Unity импортирует ONNX файл
                AssetDatabase.Refresh();
                
                // Путь к сгенерированному NNModel
                string nnModelPath = Path.ChangeExtension(assetPath, ".asset");
                
                // Проверяем, существует ли NNModel
                var nnModel = AssetDatabase.LoadAssetAtPath<NNModel>(nnModelPath);
                
                if (nnModel == null)
                {
                    // Если NNModel не был создан автоматически, попробуем создать его программно
                    nnModelPath = ImportOnnxToNNModel(assetPath);
                    nnModel = AssetDatabase.LoadAssetAtPath<NNModel>(nnModelPath);
                    
                    if (nnModel == null)
                    {
                        item.ErrorMessage = "Не удалось создать NNModel";
                        return;
                    }
                }
                
                item.IsConverted = true;
                item.ModelAssetPath = nnModelPath;
                
                // Проверка совместимости, если нужно
                if (checkCompatibilityAfterImport)
                {
                    string errorMessage;
                    item.IsCompatible = BarracudaModelChecker.CheckModelCompatibilityStatic(nnModel, out errorMessage);
                    
                    if (!item.IsCompatible)
                    {
                        item.ErrorMessage = $"Модель несовместима: {errorMessage}";
                    }
                }
                else
                {
                    item.IsCompatible = true;
                }
                
                // Создание префаба, если нужно
                if (generatePrefabs && item.IsCompatible)
                {
                    CreateModelPrefab(nnModel, nnModelPath);
                }
                
                // Удаление ONNX файла, если нужно
                if (deleteOnnxAfterImport && item.IsCompatible)
                {
                    AssetDatabase.DeleteAsset(assetPath);
                }
            }
            catch (Exception e)
            {
                item.ErrorMessage = $"Ошибка конвертации: {e.Message}";
                Debug.LogError($"Ошибка при конвертации {item.SourcePath}: {e.Message}\n{e.StackTrace}");
            }
        }
        
        private string ImportOnnxToNNModel(string onnxPath)
        {
            try
            {
                // Устанавливаем путь для NNModel
                string nnModelPath = Path.ChangeExtension(onnxPath, ".asset");
                
                // Загружаем ONNX как TextAsset
                TextAsset onnxAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(onnxPath);
                if (onnxAsset == null)
                {
                    Debug.LogError($"Не удалось загрузить ONNX файл: {onnxPath}");
                    return null;
                }
                
                // Импортируем модель через ModelImporter
                ModelImporter importer = new ModelImporter(onnxAsset.bytes, verbose: logVerbosity > 0);
                importer.Layout = memoryLayout;
                
                Model model = importer.Import();
                
                // Сохраняем модель как NNModel
                NNModelData assetData = ScriptableObject.CreateInstance<NNModelData>();
                using (MemoryStream memoryStream = new MemoryStream())
                using (BinaryWriter writer = new BinaryWriter(memoryStream))
                {
                    model.SerializeToStream(writer);
                    assetData.Value = memoryStream.ToArray();
                    writer.Close();
                }
                
                AssetDatabase.CreateAsset(assetData, nnModelPath);
                
                // Создаем NNModel
                NNModel nnModel = ScriptableObject.CreateInstance<NNModel>();
                nnModel.modelData = assetData;
                
                // Сохраняем NNModel
                string modelDataPath = AssetDatabase.GetAssetPath(assetData);
                int extensionIndex = modelDataPath.LastIndexOf('.');
                string modelPath = modelDataPath.Substring(0, extensionIndex) + "_NNModel.asset";
                
                AssetDatabase.CreateAsset(nnModel, modelPath);
                AssetDatabase.SaveAssets();
                
                return modelPath;
            }
            catch (Exception e)
            {
                Debug.LogError($"Ошибка программного импорта ONNX: {e.Message}\n{e.StackTrace}");
                return null;
            }
        }
        
        private void CreateModelPrefab(NNModel nnModel, string nnModelPath)
        {
            try
            {
                // Создаем префаб для тестирования модели
                GameObject modelObj = new GameObject(Path.GetFileNameWithoutExtension(nnModelPath) + "_TestPrefab");
                
                // Добавляем компонент DeepLabDecoder
                var decoder = modelObj.AddComponent<DeepLabDecoder>();
                decoder.model = nnModel;
                
                // Путь для сохранения префаба
                string prefabPath = Path.ChangeExtension(nnModelPath, ".prefab");
                
                // Создаем префаб
                #if UNITY_2018_3_OR_NEWER
                PrefabUtility.SaveAsPrefabAsset(modelObj, prefabPath);
                #else
                PrefabUtility.CreatePrefab(prefabPath, modelObj);
                #endif
                
                // Удаляем временный объект
                UnityEngine.Object.DestroyImmediate(modelObj);
                
                Debug.Log($"Создан префаб для модели: {prefabPath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Ошибка создания префаба: {e.Message}");
            }
        }
    }
    
    /// <summary>
    /// Реализация простой корутины для Editor
    /// </summary>
    internal class EditorCoroutine
    {
        private readonly IEnumerator<YieldInstruction> coroutine;
        private static readonly List<EditorCoroutine> activeCoroutines = new List<EditorCoroutine>();
        
        public static EditorCoroutine Start(IEnumerator<YieldInstruction> routine)
        {
            EditorCoroutine coroutine = new EditorCoroutine(routine);
            activeCoroutines.Add(coroutine);
            
            // Запускаем обновление, если это первая корутина
            if (activeCoroutines.Count == 1)
            {
                EditorApplication.update += UpdateCoroutines;
            }
            
            return coroutine;
        }
        
        private static void UpdateCoroutines()
        {
            for (int i = activeCoroutines.Count - 1; i >= 0; i--)
            {
                if (!activeCoroutines[i].Update())
                {
                    activeCoroutines.RemoveAt(i);
                }
            }
            
            // Если корутин больше нет, отписываемся от обновления
            if (activeCoroutines.Count == 0)
            {
                EditorApplication.update -= UpdateCoroutines;
            }
        }
        
        private EditorCoroutine(IEnumerator<YieldInstruction> coroutine)
        {
            this.coroutine = coroutine;
        }
        
        private bool Update()
        {
            if (coroutine.Current == null || 
                !typeof(WaitForSeconds).IsInstanceOfType(coroutine.Current) || 
                ((WaitForSeconds)coroutine.Current).CyclesLeft <= 0)
            {
                return coroutine.MoveNext();
            }
            
            ((WaitForSeconds)coroutine.Current).CyclesLeft--;
            return true;
        }
    }
    
    /// <summary>
    /// Реализация WaitForSeconds для использования в редакторе
    /// </summary>
    internal class WaitForSeconds : YieldInstruction
    {
        public int CyclesLeft { get; set; }
        
        public WaitForSeconds(float seconds)
        {
            CyclesLeft = Mathf.CeilToInt(seconds * 30); // Примерно 30 кадров в секунду
        }
    }
} 