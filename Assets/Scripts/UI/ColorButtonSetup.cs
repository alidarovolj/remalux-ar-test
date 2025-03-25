using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

namespace Remalux.AR
{
      public class ColorButtonSetup : EditorWindow
      {
            [MenuItem("Remalux/Setup Color Button Prefab")]
            public static void SetupColorButtonPrefab()
            {
                  // Create a new button GameObject
                  GameObject buttonObj = new GameObject("ColorButton");
                  buttonObj.transform.SetParent(null);

                  // Add required components
                  Image image = buttonObj.AddComponent<Image>();
                  Button button = buttonObj.AddComponent<Button>();
                  ColorButton colorButton = buttonObj.AddComponent<ColorButton>();

                  // Configure the button
                  button.transition = Selectable.Transition.ColorTint;
                  button.navigation = new Navigation { mode = Navigation.Mode.None };
                  button.targetGraphic = image;

                  // Set default color
                  image.color = Color.white;

                  // Create the prefab
                  string prefabPath = "Assets/Prefabs/UI/ColorButton.prefab";
                  GameObject prefab = PrefabUtility.SaveAsPrefabAsset(buttonObj, prefabPath);

                  // Clean up
                  DestroyImmediate(buttonObj);

                  Debug.Log("ColorButton prefab created at: " + prefabPath);
                  Selection.activeGameObject = prefab;
            }
      }
}