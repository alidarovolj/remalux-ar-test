using UnityEngine;
using UnityEditor;
using Unity.Barracuda;
using System.IO;
using System.Collections.Generic;
using System;
using System.Text;

namespace WallDetection.Editor
{
    /// <summary>
    /// Редактор для проверки совместимости ONNX моделей с Barracuda
    /// </summary>
    public class BarracudaModelCheckerEditor : EditorWindow
    {
        private List<NNModel> modelsToCheck = new List<NNModel>();
        private List<string> onnxFilePaths = new List<string>();
        private Vector2 scrollPosition;
        private Dictionary<NNModel, ModelCheckResult> modelResults = new Dictionary<NNModel, ModelCheckResult>();
        private Dictionary<string, ModelCheckResult> fileResults = new Dictionary<string, ModelCheckResult>();
        private bool showOnlyIncompatible = false;
        private bool checkingInProgress = false;
        private string statusMessage = "";
        private string searchDirectory = "";
        private bool foldout = true;
        
        [MenuItem("Remalux AR/Model Compatibility Checker")]
        public static void ShowWindow()
        {
            EditorWindow.GetWindow<BarracudaModelCheckerEditor>("Model Checker");
        }
        
        private class ModelCheckResult
        {
            public string modelName;
            public bool isCompatible;
            public string errorMessage;
            public int inputHeight;
            public int inputWidth;
            public int outputHeight;
            public int outputWidth;
            public int outputChannels;
            public string[] inputNames;
            public string[] outputNames;
            public string[] supportedBackends;
            public long modelSizeInBytes;
        }
        
        private void OnGUI()
        {
            GUILayout.Label("Barracuda Model Compatibility Checker", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            searchDirectory = EditorGUILayout.TextField("Search Directory", searchDirectory);
            if (GUILayout.Button("Browse", GUILayout.Width(80)))
            {
                string directory = EditorUtility.OpenFolderPanel("Select directory with ONNX models", "", "");
                if (!string.IsNullOrEmpty(directory))
                {
                    searchDirectory = directory;
                }
            }
            EditorGUILayout.EndHorizontal();
            
            if (GUILayout.Button("Scan Directory for ONNX Models"))
            {
                if (!string.IsNullOrEmpty(searchDirectory) && Directory.Exists(searchDirectory))
                {
                    try
                    {
                        onnxFilePaths.Clear();
                        string[] files = Directory.GetFiles(searchDirectory, "*.onnx", SearchOption.AllDirectories);
                        foreach (string file in files)
                        {
                            onnxFilePaths.Add(file);
                        }
                        
                        statusMessage = $"Found {onnxFilePaths.Count} ONNX files in {searchDirectory}";
                    }
                    catch (Exception e)
                    {
                        statusMessage = $"Error scanning directory: {e.Message}";
                    }
                }
                else
                {
                    statusMessage = "Please select a valid directory.";
                }
            }
            
            EditorGUILayout.Space();
            
            GUILayout.Label("Models in Project", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            
            NNModel newModel = (NNModel)EditorGUILayout.ObjectField("Add Model", null, typeof(NNModel), false);
            if (newModel != null && !modelsToCheck.Contains(newModel))
            {
                modelsToCheck.Add(newModel);
            }
            
            if (GUILayout.Button("Find All Models"))
            {
                FindAllModelsInProject();
            }
            
            EditorGUILayout.EndHorizontal();
            
            if (modelsToCheck.Count > 0)
            {
                foldout = EditorGUILayout.Foldout(foldout, $"Models to check ({modelsToCheck.Count})");
                
                if (foldout)
                {
                    EditorGUI.indentLevel++;
                    for (int i = 0; i < modelsToCheck.Count; i++)
                    {
                        EditorGUILayout.BeginHorizontal();
                        modelsToCheck[i] = (NNModel)EditorGUILayout.ObjectField(modelsToCheck[i], typeof(NNModel), false);
                        
                        if (GUILayout.Button("Remove", GUILayout.Width(80)))
                        {
                            modelsToCheck.RemoveAt(i);
                            break;
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                    EditorGUI.indentLevel--;
                }
            }
            
            if (onnxFilePaths.Count > 0)
            {
                GUILayout.Label($"ONNX Files to check: {onnxFilePaths.Count}");
            }
            
            EditorGUILayout.Space();
            
            EditorGUILayout.BeginHorizontal();
            
            GUI.enabled = !checkingInProgress && (modelsToCheck.Count > 0 || onnxFilePaths.Count > 0);
            if (GUILayout.Button("Check Compatibility"))
            {
                CheckModelCompatibility();
            }
            
            GUI.enabled = !checkingInProgress && (modelResults.Count > 0 || fileResults.Count > 0);
            if (GUILayout.Button("Export Report"))
            {
                ExportReport();
            }
            
            GUI.enabled = !checkingInProgress;
            if (GUILayout.Button("Clear All"))
            {
                ClearAll();
            }
            
            EditorGUILayout.EndHorizontal();
            
            showOnlyIncompatible = EditorGUILayout.Toggle("Show only incompatible", showOnlyIncompatible);
            
            EditorGUILayout.Space();
            
            if (!string.IsNullOrEmpty(statusMessage))
            {
                EditorGUILayout.HelpBox(statusMessage, MessageType.Info);
            }
            
            EditorGUILayout.Space();
            
            // Display results
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            if (modelResults.Count > 0 || fileResults.Count > 0)
            {
                GUILayout.Label("Results", EditorStyles.boldLabel);
                
                // Project models results
                foreach (var model in modelResults.Keys)
                {
                    if (model == null) continue;
                    
                    var result = modelResults[model];
                    if (showOnlyIncompatible && result.isCompatible)
                        continue;
                        
                    DisplayModelResult(model.name, result);
                }
                
                // ONNX file results
                foreach (var filePath in fileResults.Keys)
                {
                    var result = fileResults[filePath];
                    if (showOnlyIncompatible && result.isCompatible)
                        continue;
                        
                    DisplayModelResult(Path.GetFileName(filePath), result);
                }
            }
            
            EditorGUILayout.EndScrollView();
        }
        
        private void DisplayModelResult(string modelName, ModelCheckResult result)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label($"{modelName}", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            GUILayout.Label(result.isCompatible ? "✅ Compatible" : "❌ Not Compatible", 
                result.isCompatible ? EditorStyles.boldLabel : EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();
            
            if (!result.isCompatible && !string.IsNullOrEmpty(result.errorMessage))
            {
                EditorGUILayout.HelpBox(result.errorMessage, MessageType.Error);
            }
            
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label($"Input: {result.inputWidth}x{result.inputHeight}");
            GUILayout.Label($"Output: {result.outputWidth}x{result.outputHeight}x{result.outputChannels}");
            EditorGUILayout.EndHorizontal();
            
            if (result.supportedBackends != null && result.supportedBackends.Length > 0)
            {
                GUILayout.Label($"Supported backends: {string.Join(", ", result.supportedBackends)}");
            }
            
            if (result.modelSizeInBytes > 0)
            {
                GUILayout.Label($"Model size: {FormatFileSize(result.modelSizeInBytes)}");
            }
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }
        
        private void FindAllModelsInProject()
        {
            modelsToCheck.Clear();
            string[] guids = AssetDatabase.FindAssets("t:NNModel");
            
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                NNModel model = AssetDatabase.LoadAssetAtPath<NNModel>(path);
                if (model != null)
                {
                    modelsToCheck.Add(model);
                }
            }
            
            statusMessage = $"Found {modelsToCheck.Count} models in project.";
        }
        
        private void CheckModelCompatibility()
        {
            if (checkingInProgress)
                return;
                
            checkingInProgress = true;
            statusMessage = "Checking model compatibility...";
            
            try
            {
                modelResults.Clear();
                fileResults.Clear();
                
                // Check project models
                foreach (var model in modelsToCheck)
                {
                    if (model == null) continue;
                    
                    ModelCheckResult result = CheckModel(model);
                    modelResults[model] = result;
                }
                
                // Check ONNX files
                foreach (var filePath in onnxFilePaths)
                {
                    if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) continue;
                    
                    ModelCheckResult result = CheckOnnxFile(filePath);
                    fileResults[filePath] = result;
                }
                
                statusMessage = $"Checked {modelResults.Count + fileResults.Count} models. " +
                                $"Compatible: {CountCompatible()}, Incompatible: {CountIncompatible()}";
            }
            catch (Exception e)
            {
                statusMessage = $"Error during check: {e.Message}";
                Debug.LogException(e);
            }
            finally
            {
                checkingInProgress = false;
                Repaint();
            }
        }
        
        private int CountCompatible()
        {
            int count = 0;
            
            foreach (var result in modelResults.Values)
                if (result.isCompatible) count++;
                
            foreach (var result in fileResults.Values)
                if (result.isCompatible) count++;
                
            return count;
        }
        
        private int CountIncompatible()
        {
            int count = 0;
            
            foreach (var result in modelResults.Values)
                if (!result.isCompatible) count++;
                
            foreach (var result in fileResults.Values)
                if (!result.isCompatible) count++;
                
            return count;
        }
        
        private ModelCheckResult CheckModel(NNModel model)
        {
            ModelCheckResult result = new ModelCheckResult();
            result.modelName = model.name;
            
            try
            {
                // Загрузка модели через Barracuda
                Model runtimeModel = ModelLoader.Load(model);
                
                // Если модель загружена без исключений, проверяем основные параметры
                if (runtimeModel == null)
                {
                    result.isCompatible = false;
                    result.errorMessage = "Failed to load model";
                    return result;
                }
                
                result.isCompatible = true;
                result.modelSizeInBytes = model.modelAsset.bytes.Length;
                
                // Получение размеров входа/выхода
                if (runtimeModel.inputs.Count > 0)
                {
                    var input = runtimeModel.inputs[0];
                    result.inputNames = new string[] { input.name };
                    if (input.shape.Length >= 3)
                    {
                        result.inputHeight = input.shape[2];
                        result.inputWidth = input.shape[3];
                    }
                }
                
                // Определение размеров выхода
                result.outputNames = runtimeModel.outputs;
                var outputLayers = runtimeModel.layers.FindAll(l => runtimeModel.outputs.Contains(l.name));
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
                    if (WorkerFactory.IsType(backendType, runtimeModel))
                        supportedBackends.Add(backendType.ToString());
                }
                result.supportedBackends = supportedBackends.ToArray();
            }
            catch (Exception e)
            {
                result.isCompatible = false;
                result.errorMessage = e.Message;
            }
            
            return result;
        }
        
        private ModelCheckResult CheckOnnxFile(string filePath)
        {
            ModelCheckResult result = new ModelCheckResult();
            result.modelName = Path.GetFileName(filePath);
            
            try
            {
                if (!File.Exists(filePath))
                {
                    result.isCompatible = false;
                    result.errorMessage = "File does not exist";
                    return result;
                }
                
                // Загрузка модели через Barracuda
                Model runtimeModel = ModelLoader.Load(filePath);
                
                // Если модель загружена без исключений, проверяем основные параметры
                if (runtimeModel == null)
                {
                    result.isCompatible = false;
                    result.errorMessage = "Failed to load model";
                    return result;
                }
                
                result.isCompatible = true;
                result.modelSizeInBytes = new FileInfo(filePath).Length;
                
                // Получение размеров входа/выхода
                if (runtimeModel.inputs.Count > 0)
                {
                    var input = runtimeModel.inputs[0];
                    result.inputNames = new string[] { input.name };
                    if (input.shape.Length >= 3)
                    {
                        result.inputHeight = input.shape[2];
                        result.inputWidth = input.shape[3];
                    }
                }
                
                // Определение размеров выхода
                result.outputNames = runtimeModel.outputs;
                var outputLayers = runtimeModel.layers.FindAll(l => runtimeModel.outputs.Contains(l.name));
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
                    if (WorkerFactory.IsType(backendType, runtimeModel))
                        supportedBackends.Add(backendType.ToString());
                }
                result.supportedBackends = supportedBackends.ToArray();
            }
            catch (Exception e)
            {
                result.isCompatible = false;
                result.errorMessage = e.Message;
            }
            
            return result;
        }
        
        private void ExportReport()
        {
            if (modelResults.Count == 0 && fileResults.Count == 0)
                return;
                
            string path = EditorUtility.SaveFilePanel("Save compatibility report", "", "model_compatibility_report.txt", "txt");
            
            if (string.IsNullOrEmpty(path))
                return;
                
            try
            {
                StringBuilder sb = new StringBuilder();
                
                sb.AppendLine("# ONNX Model Compatibility Report with Barracuda");
                sb.AppendLine($"Generated: {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}");
                sb.AppendLine($"Unity version: {Application.unityVersion}");
                sb.AppendLine("-----------------------------------");
                
                // Project models
                if (modelResults.Count > 0)
                {
                    sb.AppendLine("\n## Models in Project");
                    
                    foreach (var model in modelResults.Keys)
                    {
                        if (model == null) continue;
                        
                        var result = modelResults[model];
                        WriteModelResult(sb, model.name, result);
                    }
                }
                
                // ONNX files
                if (fileResults.Count > 0)
                {
                    sb.AppendLine("\n## External ONNX Files");
                    
                    foreach (var filePath in fileResults.Keys)
                    {
                        var result = fileResults[filePath];
                        WriteModelResult(sb, Path.GetFileName(filePath), result, filePath);
                    }
                }
                
                File.WriteAllText(path, sb.ToString());
                statusMessage = $"Report saved to {path}";
            }
            catch (Exception e)
            {
                statusMessage = $"Error saving report: {e.Message}";
                Debug.LogException(e);
            }
        }
        
        private void WriteModelResult(StringBuilder sb, string modelName, ModelCheckResult result, string filePath = "")
        {
            sb.AppendLine($"\n### Model: {modelName}");
            if (!string.IsNullOrEmpty(filePath))
                sb.AppendLine($"Path: {filePath}");
                
            sb.AppendLine($"Compatible: {(result.isCompatible ? "Yes" : "No")}");
            
            if (!result.isCompatible && !string.IsNullOrEmpty(result.errorMessage))
                sb.AppendLine($"Error: {result.errorMessage}");
                
            sb.AppendLine($"Model size: {FormatFileSize(result.modelSizeInBytes)}");
            sb.AppendLine($"Input dimensions: {result.inputWidth}x{result.inputHeight}");
            sb.AppendLine($"Output dimensions: {result.outputWidth}x{result.outputHeight}x{result.outputChannels}");
            
            if (result.inputNames != null && result.inputNames.Length > 0)
                sb.AppendLine($"Input nodes: {string.Join(", ", result.inputNames)}");
                
            if (result.outputNames != null && result.outputNames.Length > 0)
                sb.AppendLine($"Output nodes: {string.Join(", ", result.outputNames)}");
                
            if (result.supportedBackends != null && result.supportedBackends.Length > 0)
                sb.AppendLine($"Supported backends: {string.Join(", ", result.supportedBackends)}");
                
            sb.AppendLine("-----------------------------------");
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
        
        private void ClearAll()
        {
            modelsToCheck.Clear();
            onnxFilePaths.Clear();
            modelResults.Clear();
            fileResults.Clear();
            statusMessage = "All data cleared.";
        }
    }
} 