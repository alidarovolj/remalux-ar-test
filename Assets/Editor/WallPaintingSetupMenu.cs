using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Reflection;

namespace Remalux.Editor
{
      /// <summary>
      /// Unified Wall Painting System setup tool that provides both direct menu setup
      /// and a configuration window
      /// </summary>
      public class WallPaintingSetupMenu : EditorWindow
      {
            // Setup options
            private bool setupXR = true;
            private bool setupWallPainting = true;
            private bool setupUI = true;
            private float cameraYOffset = 1.6f;

            // Configuration window mode
            [MenuItem("Window/Remalux/Wall Painting Setup Window")]
            public static void ShowWindow()
            {
                  GetWindow<WallPaintingSetupMenu>("Wall Painting Setup");
            }

            // Quick setup mode - directly creates scene with default settings
            [MenuItem("Window/Remalux/Quick Wall Painting Setup")]
            public static void QuickSetup()
            {
                  SetupUsingNewComponents();
            }

            // Draw the editor window
            private void OnGUI()
            {
                  GUILayout.Label("Wall Painting System Setup", EditorStyles.boldLabel);
                  EditorGUILayout.Space();

                  EditorGUILayout.HelpBox("This tool will set up wall painting components in your scene.", MessageType.Info);
                  EditorGUILayout.Space();

                  setupXR = EditorGUILayout.Toggle("Setup XR Camera", setupXR);
                  setupWallPainting = EditorGUILayout.Toggle("Setup Wall Painting", setupWallPainting);
                  setupUI = EditorGUILayout.Toggle("Setup UI Elements", setupUI);

                  if (setupXR)
                  {
                        EditorGUI.indentLevel++;
                        cameraYOffset = EditorGUILayout.FloatField("Camera Y Offset", cameraYOffset);
                        EditorGUI.indentLevel--;
                  }

                  EditorGUILayout.Space();

                  if (GUILayout.Button("Setup Using New Components"))
                  {
                        SetupUsingNewComponents();
                  }

                  if (GUILayout.Button("Setup Using New Scene Setup Script"))
                  {
                        SetupUsingSceneSetupScript();
                  }

                  EditorGUILayout.Space();
                  EditorGUILayout.HelpBox("The new setup uses reflection to avoid direct dependencies, making it more compatible with different Unity versions.", MessageType.Info);
            }

            // Setup using the WallPaintingSceneSetup component
            private static void SetupUsingSceneSetupScript()
            {
                  // Create GameObject to hold setup script
                  GameObject setupObj = new GameObject("WallPaintingSetup");

                  // Try to find and add the scene setup component using reflection
                  bool success = false;
                  var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
                  foreach (var assembly in assemblies)
                  {
                        var type = assembly.GetType("Remalux.WallPainting.Setup.WallPaintingSceneSetup");
                        if (type != null)
                        {
                              setupObj.AddComponent(type);

                              // Invoke the AutoSetupScene method
                              var setupComponent = setupObj.GetComponent(type);
                              MethodInfo method = type.GetMethod("AutoSetupScene",
                                  BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

                              if (method != null)
                              {
                                    method.Invoke(setupComponent, null);
                                    success = true;
                              }
                              else
                              {
                                    // Fall back to using Start method through play mode
                                    EditorUtility.DisplayDialog("Setup Information",
                                        "The scene will be set up when you enter Play mode. Please press Play.", "OK");
                                    success = true;
                              }
                              break;
                        }
                  }

                  if (!success)
                  {
                        Debug.LogError("Could not find WallPaintingSceneSetup component. Make sure the Setup assembly is compiled.");
                        EditorUtility.DisplayDialog("Setup Failed",
                            "Could not find WallPaintingSceneSetup component. Make sure the Setup assembly is compiled.", "OK");
                        DestroyImmediate(setupObj);
                  }
            }

            // Setup using the static method from RealWallPaintingSetup
            private static void SetupUsingNewComponents()
            {
                  // Try to find and invoke the setup method using reflection
                  bool success = false;
                  var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
                  foreach (var assembly in assemblies)
                  {
                        var type = assembly.GetType("Remalux.WallPainting.Vision.RealWallPaintingSetup");
                        if (type != null)
                        {
                              var method = type.GetMethod("CreateRealWallPaintingScene",
                                  BindingFlags.Public | BindingFlags.Static);

                              if (method != null)
                              {
                                    method.Invoke(null, null);
                                    success = true;
                                    break;
                              }
                        }
                  }

                  if (!success)
                  {
                        Debug.LogError("Could not find RealWallPaintingSetup.CreateRealWallPaintingScene method.");
                        EditorUtility.DisplayDialog("Setup Failed",
                            "Could not find RealWallPaintingSetup. Make sure the WallPainting Vision assembly is compiled.", "OK");
                  }
                  else
                  {
                        Debug.Log("Wall Painting setup completed successfully!");
                        EditorUtility.DisplayDialog("Setup Complete",
                            "Wall Painting system has been set up successfully in the current scene.", "OK");
                  }
            }
      }
}