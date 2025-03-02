using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace Remalux.AR
{
      /// <summary>
      /// This script provides a guide for setting up the wall painting system.
      /// Add this component to a GameObject in your scene to see setup instructions in the Inspector.
      /// </summary>
      public class WallPaintingSetupGuide : MonoBehaviour
      {
            [Header("Step 1: Required Prefabs")]
            [Tooltip("Assign the PaintingUI prefab here")]
            public GameObject paintingUIPrefab;

            [Tooltip("Assign the ColorPreview prefab here")]
            public GameObject colorPreviewPrefab;

            [Header("Step 2: Required Materials")]
            [Tooltip("Assign the default wall material here")]
            public Material defaultWallMaterial;

            [Tooltip("Assign all paint materials here")]
            public Material[] paintMaterials;

            [Header("Step 3: Scene References")]
            [Tooltip("Assign your main camera here")]
            public Camera mainCamera;

            [Tooltip("Assign your UI canvas here")]
            public Canvas mainCanvas;

            [Header("Step 4: Wall Layer Settings")]
            [Tooltip("Set the layer mask for wall detection")]
            public LayerMask wallLayerMask = 1; // Default to "Default" layer

            [Header("Setup Status")]
            [SerializeField] private bool prefabsAssigned = false;
            [SerializeField] private bool materialsAssigned = false;
            [SerializeField] private bool referencesAssigned = false;

            private void OnValidate()
            {
                  // Check if prefabs are assigned
                  prefabsAssigned = paintingUIPrefab != null && colorPreviewPrefab != null;

                  // Check if materials are assigned
                  materialsAssigned = defaultWallMaterial != null && paintMaterials != null && paintMaterials.Length > 0;

                  // Check if references are assigned
                  referencesAssigned = mainCamera != null && mainCanvas != null;
            }

            public void SetupScene()
            {
                  if (!prefabsAssigned || !materialsAssigned || !referencesAssigned)
                  {
                        Debug.LogError("Cannot setup scene: Please assign all required references first!");
                        return;
                  }

                  // Create SceneSetup GameObject
                  GameObject setupObj = new GameObject("WallPaintingSystem");
                  SceneSetup sceneSetup = setupObj.AddComponent<SceneSetup>();

                  // Assign references
                  var setupFields = sceneSetup.GetType().GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);

                  foreach (var field in setupFields)
                  {
                        if (field.Name == "paintingUIPrefab")
                              field.SetValue(sceneSetup, paintingUIPrefab);
                        else if (field.Name == "colorPreviewPrefab")
                              field.SetValue(sceneSetup, colorPreviewPrefab);
                        else if (field.Name == "defaultWallMaterial")
                              field.SetValue(sceneSetup, defaultWallMaterial);
                        else if (field.Name == "paintMaterials")
                              field.SetValue(sceneSetup, paintMaterials);
                        else if (field.Name == "mainCamera")
                              field.SetValue(sceneSetup, mainCamera);
                        else if (field.Name == "mainCanvas")
                              field.SetValue(sceneSetup, mainCanvas);
                  }

                  // Configure wall layer mask
                  GameObject wallPainterObj = GameObject.Find("WallPainter");
                  if (wallPainterObj != null)
                  {
                        WallPainter wallPainter = wallPainterObj.GetComponent<WallPainter>();
                        if (wallPainter != null)
                        {
                              var wallLayerField = wallPainter.GetType().GetField("wallLayerMask",
                                  System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

                              if (wallLayerField != null)
                                    wallLayerField.SetValue(wallPainter, wallLayerMask);
                        }
                  }

                  Debug.Log("Wall Painting System setup complete!");

                  // Destroy this guide object
                  Destroy(gameObject);
            }
      }
}