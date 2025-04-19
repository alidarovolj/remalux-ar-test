using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using Unity.Barracuda;

#if UNITY_EDITOR
[ExecuteInEditMode]
public class OnnxConverter : MonoBehaviour
{
    [MenuItem("Tools/Convert ONNX to NNModel")]
    public static void ConvertOnnxToNNModel()
    {
        // Путь к исходному ONNX файлу
        string onnxPath = EditorUtility.OpenFilePanel("Select ONNX model", "", "onnx");
        if (string.IsNullOrEmpty(onnxPath))
            return;

        // Создаем путь для сохранения внутри проекта
        string filename = Path.GetFileNameWithoutExtension(onnxPath);
        string assetPath = $"Assets/Resources/Models/{filename}.asset";

        try
        {
            // Убеждаемся, что директории существуют
            string modelsDirectory = Path.Combine(Application.dataPath, "Resources/Models");
            if (!Directory.Exists(modelsDirectory))
            {
                Directory.CreateDirectory(modelsDirectory);
            }

            string tempDirectory = Path.Combine(Application.dataPath, "Resources/Temp");
            if (!Directory.Exists(tempDirectory))
            {
                Directory.CreateDirectory(tempDirectory);
            }

            // Временный путь внутри проекта для импорта
            string tempProjectPath = "Assets/Resources/Temp/temp_model.bytes";
            string tempAbsolutePath = Path.Combine(Application.dataPath, "Resources/Temp/temp_model.bytes");

            // Копируем ONNX файл во временный файл внутри проекта
            File.Copy(onnxPath, tempAbsolutePath, true);
            AssetDatabase.ImportAsset(tempProjectPath);
            AssetDatabase.Refresh();

            // Загружаем файл как TextAsset
            var textAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(tempProjectPath);
            if (textAsset == null)
            {
                Debug.LogError($"Не удалось загрузить модель как TextAsset: {tempProjectPath}");
                return;
            }

            // Создаем NNModel из байтов модели
            ModelBuilder builder = new ModelBuilder();
            using (var memoryStream = new MemoryStream(textAsset.bytes))
            {
                using (var reader = new BinaryReader(memoryStream))
                {
                    try
                    {
                        byte[] bytes = textAsset.bytes;
                        NNModelData modelData = ScriptableObject.CreateInstance<NNModelData>();
                        modelData.Value = bytes;

                        NNModel model = ScriptableObject.CreateInstance<NNModel>();
                        model.modelData = modelData;

                        // Сохраняем модель как ассет
                        AssetDatabase.CreateAsset(model, assetPath);
                        AssetDatabase.AddObjectToAsset(modelData, assetPath);
                        AssetDatabase.SaveAssets();
                        AssetDatabase.Refresh();

                        Debug.Log($"ONNX модель успешно сконвертирована в NNModel: {assetPath}");

                        // Проверяем модель (попытка загрузки)
                        try
                        {
                            var loadedModel = ModelLoader.Load(model, false);
                            Debug.Log($"Модель успешно загружена. Входы: {string.Join(", ", loadedModel.inputs)}");
                            Debug.Log($"Выходы: {string.Join(", ", loadedModel.outputs)}");
                        }
                        catch (Exception ex)
                        {
                            Debug.LogWarning($"Возможно несовместимый формат ONNX: {ex.Message}");
                            Debug.LogWarning("Убедитесь, что модель экспортирована с opset_version=11 или ниже для совместимости с Barracuda.");
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Ошибка при создании модели: {ex.Message}");
                    }
                }
            }

            // Удаляем временный файл
            AssetDatabase.DeleteAsset(tempProjectPath);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Ошибка при конвертации ONNX: {ex.Message}\n{ex.StackTrace}");
        }
    }
}
#endif 