using UnityEngine;
using UnityEditor;
using Remalux.AR;

namespace Remalux.WallPainting
{
      public class WallPaintingSetupEditor : EditorWindow
      {
            [MenuItem("Window/Remalux/Wall Painting Setup")]
            public static void ShowWindow()
            {
                  GetWindow<WallPaintingSetupEditor>("Wall Painting Setup");
            }

            private void OnGUI()
            {
                  GUILayout.Label("Wall Painting Setup Guide", EditorStyles.boldLabel);
                  EditorGUILayout.Space();

                  if (GUILayout.Button("Validate Setup"))
                  {
                        WallPaintingSetupGuide.ValidateSetup();
                  }

                  EditorGUILayout.Space();

                  if (GUILayout.Button("Create Default UI Prefabs"))
                  {
                        if (EditorUtility.DisplayDialog("Create Default Prefabs",
                            "This will create default UI prefabs in the Assets/Prefabs/UI folder. Do you want to continue?",
                            "Yes", "No"))
                        {
                              WallPaintingSetupGuide.CreateDefaultPrefabs();
                        }
                  }

                  EditorGUILayout.Space();
                  EditorGUILayout.HelpBox(
                      "This tool helps you set up the Wall Painting system by:\n" +
                      "1. Validating required tags and layers\n" +
                      "2. Checking UI prefabs\n" +
                      "3. Creating default UI prefabs if needed",
                      MessageType.Info);
            }
      }
}