#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace Remalux.AR
{
    public class SetWallLayerTool : EditorWindow
    {
        private bool addCollider = true;
        private int colliderType = 0; // 0 - Box, 1 - Mesh
        private string[] colliderOptions = new string[] { "Box Collider", "Mesh Collider" };

        [MenuItem("Remalux/Инструменты/Назначить слой Wall")]
        public static void ShowWindow()
        {
            GetWindow<SetWallLayerTool>("Назначить слой Wall");
        }

        private void OnGUI()
        {
            GUILayout.Label("Инструмент для назначения слоя Wall", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUILayout.HelpBox("Этот инструмент назначит слой 'Wall' выбранным объектам, чтобы их можно было красить.", MessageType.Info);
            EditorGUILayout.Space();

            addCollider = EditorGUILayout.Toggle("Добавить коллайдер", addCollider);
            
            if (addCollider)
            {
                colliderType = EditorGUILayout.Popup("Тип коллайдера", colliderType, colliderOptions);
            }

            EditorGUILayout.Space();

            if (GUILayout.Button("Назначить слой Wall выбранным объектам"))
            {
                SetWallLayerToSelectedObjects();
            }

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("Примечание: Убедитесь, что слой 'Wall' создан в настройках проекта.", MessageType.Warning);
        }

        private void SetWallLayerToSelectedObjects()
        {
            GameObject[] selectedObjects = Selection.gameObjects;
            
            if (selectedObjects.Length == 0)
            {
                EditorUtility.DisplayDialog("Ошибка", "Не выбрано ни одного объекта.", "OK");
                return;
            }

            // Проверяем, существует ли слой Wall
            int wallLayer = LayerMask.NameToLayer("Wall");
            if (wallLayer == -1)
            {
                bool createLayer = EditorUtility.DisplayDialog(
                    "Слой не найден", 
                    "Слой 'Wall' не найден в настройках проекта. Хотите создать его?", 
                    "Да", "Нет");
                
                if (createLayer)
                {
                    // Создаем слой Wall
                    SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
                    SerializedProperty layers = tagManager.FindProperty("layers");
                    
                    // Ищем свободный слой
                    for (int i = 8; i < 32; i++)
                    {
                        SerializedProperty layerProp = layers.GetArrayElementAtIndex(i);
                        if (string.IsNullOrEmpty(layerProp.stringValue))
                        {
                            layerProp.stringValue = "Wall";
                            tagManager.ApplyModifiedProperties();
                            wallLayer = i;
                            Debug.Log($"Создан слой 'Wall' с индексом {i}");
                            break;
                        }
                    }
                }
                else
                {
                    return;
                }
            }

            int objectsProcessed = 0;
            
            foreach (GameObject obj in selectedObjects)
            {
                Undo.RecordObject(obj, "Set Wall Layer");
                
                // Назначаем слой Wall
                obj.layer = wallLayer;
                
                // Проверяем наличие Renderer
                if (obj.GetComponent<Renderer>() == null)
                {
                    Debug.LogWarning($"Объект {obj.name} не имеет компонента Renderer. Он не будет отображаться при покраске.");
                }
                
                // Добавляем коллайдер, если нужно
                if (addCollider)
                {
                    Collider existingCollider = obj.GetComponent<Collider>();
                    
                    if (existingCollider == null)
                    {
                        if (colliderType == 0) // Box Collider
                        {
                            Undo.AddComponent<BoxCollider>(obj);
                            Debug.Log($"Добавлен BoxCollider к объекту {obj.name}");
                        }
                        else // Mesh Collider
                        {
                            MeshCollider meshCollider = Undo.AddComponent<MeshCollider>(obj);
                            MeshFilter meshFilter = obj.GetComponent<MeshFilter>();
                            
                            if (meshFilter != null && meshFilter.sharedMesh != null)
                            {
                                meshCollider.sharedMesh = meshFilter.sharedMesh;
                                Debug.Log($"Добавлен MeshCollider к объекту {obj.name}");
                            }
                            else
                            {
                                Debug.LogWarning($"Объект {obj.name} не имеет MeshFilter или Mesh. MeshCollider может работать некорректно.");
                            }
                        }
                    }
                }
                
                objectsProcessed++;
            }
            
            EditorUtility.DisplayDialog("Готово", $"Слой 'Wall' назначен {objectsProcessed} объектам.", "OK");
        }
    }
}
#endif