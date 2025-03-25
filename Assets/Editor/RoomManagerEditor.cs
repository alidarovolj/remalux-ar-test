using UnityEngine;
using UnityEditor;

namespace Remalux.WallPainting
{
      [CustomEditor(typeof(RoomManager))]
      public class RoomManagerEditor : UnityEditor.Editor
      {
            private SerializedProperty wallHeight;
            private SerializedProperty wallThickness;
            private SerializedProperty defaultWallMaterial;

            private void OnEnable()
            {
                  wallHeight = serializedObject.FindProperty("wallHeight");
                  wallThickness = serializedObject.FindProperty("wallThickness");
                  defaultWallMaterial = serializedObject.FindProperty("defaultWallMaterial");
            }

            public override void OnInspectorGUI()
            {
                  serializedObject.Update();

                  EditorGUILayout.LabelField("Wall Settings", EditorStyles.boldLabel);
                  EditorGUI.indentLevel++;

                  EditorGUILayout.PropertyField(wallHeight, new GUIContent("Height", "Высота стен в метрах"));
                  EditorGUILayout.PropertyField(wallThickness, new GUIContent("Thickness", "Толщина стен в метрах"));
                  EditorGUILayout.PropertyField(defaultWallMaterial, new GUIContent("Material", "Материал по умолчанию для стен"));

                  EditorGUI.indentLevel--;

                  EditorGUILayout.Space();

                  if (GUILayout.Button("Create Default Room"))
                  {
                        RoomManager roomManager = (RoomManager)target;
                        roomManager.CreateDefaultRoom();
                  }

                  if (GUILayout.Button("Clear Room"))
                  {
                        RoomManager roomManager = (RoomManager)target;
                        roomManager.ClearWalls();
                  }

                  serializedObject.ApplyModifiedProperties();
            }
      }
}