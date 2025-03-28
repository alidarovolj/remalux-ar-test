#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace Remalux.WallPainting
{
      public class MaterialCreator : EditorWindow
      {
            private string materialsPath = "Assets/Materials/Paints";

            [MenuItem("Remalux/Инструменты/Создать материалы")]
            public static void ShowWindow()
            {
                  GetWindow<MaterialCreator>("Создание материалов");
            }

            private void OnGUI()
            {
                  GUILayout.Label("Создание материалов для покраски", EditorStyles.boldLabel);
                  EditorGUILayout.Space();

                  materialsPath = EditorGUILayout.TextField("Путь к материалам", materialsPath);

                  if (GUILayout.Button("Создать все материалы"))
                  {
                        CreateAllMaterials();
                  }
            }

            private void CreateAllMaterials()
            {
                  // Создаем директорию, если она не существует
                  if (!System.IO.Directory.Exists(materialsPath))
                  {
                        System.IO.Directory.CreateDirectory(materialsPath);
                  }

                  // Создаем материал по умолчанию
                  CreateDefaultWallMaterial();

                  // Создаем базовые материалы для покраски
                  CreatePaintMaterials();

                  AssetDatabase.Refresh();
                  EditorUtility.DisplayDialog("Готово", "Все материалы созданы успешно!", "OK");
            }

            private void CreateDefaultWallMaterial()
            {
                  // Создаем материал по умолчанию
                  Material defaultMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                  defaultMaterial.color = new Color(0.8f, 0.8f, 0.8f); // Светло-серый цвет
                  defaultMaterial.name = "DefaultWallMaterial";

                  // Сохраняем материал
                  string assetPath = $"{materialsPath}/DefaultWallMaterial.mat";
                  AssetDatabase.CreateAsset(defaultMaterial, assetPath);
                  AssetDatabase.SaveAssets();
            }

            private void CreatePaintMaterials()
            {
                  // Создаем несколько базовых материалов для покраски
                  Color[] colors = new Color[]
                  {
                        Color.red,
                        Color.blue,
                        Color.green,
                        Color.yellow,
                        Color.white
                  };

                  for (int i = 0; i < colors.Length; i++)
                  {
                        Material paintMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                        paintMaterial.color = colors[i];
                        paintMaterial.name = $"PaintMaterial_{i + 1}";

                        // Сохраняем материал
                        string assetPath = $"{materialsPath}/PaintMaterial_{i + 1}.mat";
                        AssetDatabase.CreateAsset(paintMaterial, assetPath);
                        AssetDatabase.SaveAssets();
                  }
            }
      }
}
#endif