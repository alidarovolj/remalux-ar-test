using UnityEngine;
using UnityEditor;
using Remalux.AR;

[CustomEditor(typeof(WallPaintingSetupGuide))]
public class WallPaintingSetupEditor : Editor
{
      public override void OnInspectorGUI()
      {
            WallPaintingSetupGuide guide = (WallPaintingSetupGuide)target;

            // Draw the default inspector
            DrawDefaultInspector();

            EditorGUILayout.Space(10);

            // Check if all required fields are assigned
            bool allAssigned = guide.paintingUIPrefab != null &&
                              guide.colorPreviewPrefab != null &&
                              guide.defaultWallMaterial != null &&
                              guide.paintMaterials != null &&
                              guide.paintMaterials.Length > 0 &&
                              guide.mainCamera != null &&
                              guide.mainCanvas != null;

            // Display setup button
            GUI.enabled = allAssigned;

            if (GUILayout.Button("Setup Wall Painting System", GUILayout.Height(40)))
            {
                  guide.SetupScene();
            }

            if (!allAssigned)
            {
                  EditorGUILayout.HelpBox("Please assign all required fields before setting up the system.", MessageType.Warning);
            }

            GUI.enabled = true;

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Quick Assign", EditorStyles.boldLabel);

            // Quick assign buttons
            if (GUILayout.Button("Find Main Camera"))
            {
                  guide.mainCamera = Camera.main;
                  EditorUtility.SetDirty(guide);
            }

            if (GUILayout.Button("Find Canvas"))
            {
                  Canvas[] canvases = Object.FindObjectsOfType<Canvas>();
                  if (canvases.Length > 0)
                  {
                        guide.mainCanvas = canvases[0];
                        EditorUtility.SetDirty(guide);
                  }
            }

            // Layer setup section
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Wall Layer Setup", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Make sure to set up a layer for walls in your project. By default, the system will use the 'Default' layer.", MessageType.Info);

            if (GUILayout.Button("Create 'Wall' Layer"))
            {
                  // This is just a reminder - Unity doesn't allow creating layers via script
                  EditorUtility.DisplayDialog("Layer Creation",
                      "To create a 'Wall' layer:\n\n1. Go to Edit > Project Settings > Tags and Layers\n2. Add a new layer named 'Wall'\n3. Come back and select this layer in the Wall Layer Mask field",
                      "OK");
            }
      }
}