using UnityEngine;
using UnityEditor;
using System.IO;
using System;

/// <summary>
/// Editor utility that adds menu items to help fix Barracuda dependency issues
/// </summary>
public class BarracudaFixMenu : MonoBehaviour
{
    [MenuItem("Tools/Fix All Barracuda Issues")]
    public static void FixAllBarracudaIssues()
    {
        // Apply all fixes
        AddDefineSymbols();
        CopyBarracudaDll();
        CreateAssemblyReference();
        
        // Refresh AssetDatabase to ensure Unity sees the changes
        AssetDatabase.Refresh();
        
        EditorUtility.DisplayDialog(
            "Barracuda Fix Applied", 
            "All Barracuda fixes have been applied.\n\n" +
            "If you still experience issues, please restart Unity.",
            "OK");
    }

    private static void AddDefineSymbols()
    {
        string[] symbols = {"USE_BARRACUDA_INDIRECTLY", "FIXED_BUILD_ORDER"};
        string definesString = PlayerSettings.GetScriptingDefineSymbolsForGroup(
            EditorUserBuildSettings.selectedBuildTargetGroup);
        
        bool changed = false;
        foreach (string symbol in symbols)
        {
            if (!definesString.Contains(symbol))
            {
                if (definesString.Length > 0 && !definesString.EndsWith(";"))
                    definesString += ";";
                definesString += symbol;
                changed = true;
            }
        }
        
        if (changed)
        {
            PlayerSettings.SetScriptingDefineSymbolsForGroup(
                EditorUserBuildSettings.selectedBuildTargetGroup, definesString);
            Debug.Log("Added Barracuda fix define symbols: " + string.Join(", ", symbols));
        }
    }

    private static void CopyBarracudaDll()
    {
        // Create plugins directory if it doesn't exist
        string pluginsDir = "Assets/Plugins/BarracudaFix";
        if (!Directory.Exists(pluginsDir))
        {
            Directory.CreateDirectory(pluginsDir);
        }
        
        // Find Barracuda DLL in Library folder
        string libraryPath = Path.Combine(Application.dataPath, "..", "Library");
        string[] dllPaths = Directory.GetFiles(libraryPath, "Unity.Barracuda.dll", SearchOption.AllDirectories);
        
        if (dllPaths.Length > 0)
        {
            string targetPath = Path.Combine(pluginsDir, "Unity.Barracuda.dll");
            File.Copy(dllPaths[0], targetPath, true);
            Debug.Log("Copied Barracuda DLL to Plugins folder: " + targetPath);
        }
        else
        {
            Debug.LogWarning("Could not find Unity.Barracuda.dll in Library folder");
        }
    }

    private static void CreateAssemblyReference()
    {
        string refPath = "Assets/asmref-barracuda-fix.asmref";
        string content = "{\n    \"reference\": \"Unity.Barracuda\"\n}";
        
        File.WriteAllText(refPath, content);
        Debug.Log("Created assembly reference file: " + refPath);
    }
} 