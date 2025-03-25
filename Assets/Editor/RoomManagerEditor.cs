using UnityEngine;
using UnityEditor;

namespace Remalux.WallPainting
{
      [CustomEditor(typeof(RoomManager))]
      public class RoomManagerEditor : UnityEditor.Editor
      {
            private SerializedProperty roomWidth;
            private SerializedProperty roomHeight;
            private SerializedProperty roomDepth;
            private SerializedProperty wallThickness;
            private SerializedProperty defaultWallMaterial;

            private void OnEnable()
            {
                  roomWidth = serializedObject.FindProperty("roomWidth");
                  roomHeight = serializedObject.FindProperty("roomHeight");
                  roomDepth = serializedObject.FindProperty("roomDepth");
                  wallThickness = serializedObject.FindProperty("wallThickness");
                  defaultWallMaterial = serializedObject.FindProperty("defaultWallMaterial");
            }

            public override void OnInspectorGUI()
            {
                  serializedObject.Update();

                  EditorGUILayout.LabelField("Room Dimensions", EditorStyles.boldLabel);
                  EditorGUI.indentLevel++;

                  EditorGUILayout.PropertyField(roomWidth, new GUIContent("Width", "Ширина комнаты в метрах"));
                  EditorGUILayout.PropertyField(roomHeight, new GUIContent("Height", "Высота комнаты в метрах"));
                  EditorGUILayout.PropertyField(roomDepth, new GUIContent("Depth", "Глубина комнаты в метрах"));

                  EditorGUI.indentLevel--;

                  EditorGUILayout.Space();
                  EditorGUILayout.LabelField("Wall Settings", EditorStyles.boldLabel);
                  EditorGUI.indentLevel++;

                  EditorGUILayout.PropertyField(wallThickness, new GUIContent("Thickness", "Толщина стен в метрах"));
                  EditorGUILayout.PropertyField(defaultWallMaterial, new GUIContent("Material", "Материал по умолчанию для стен"));

                  EditorGUI.indentLevel--;

                  if (GUILayout.Button("Recreate Room"))
                  {
                        RoomManager roomManager = (RoomManager)target;
                        roomManager.CreateRoom(); // Теперь метод публичный, не нужна рефлексия
                  }

                  serializedObject.ApplyModifiedProperties();
            }
      }
}