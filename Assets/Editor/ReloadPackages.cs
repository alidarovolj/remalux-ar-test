using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;
using System.Collections.Generic;

public class ReloadPackages
{
    [MenuItem("Tools/Reload Packages")]
    public static void ReloadAllPackages()
    {
        Debug.Log("Reloading all packages...");
        
        // Force a package refresh
        Client.Resolve();
        
        // Mark the editor as dirty to force a recompile
        EditorUtility.SetDirty(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
        
        Debug.Log("Package reload requested. Unity will recompile scripts shortly.");
        
        // Request asset database refresh
        AssetDatabase.Refresh();
    }
    
    [MenuItem("Tools/Fix Missing Assembly References")]
    public static void FixMissingReferences()
    {
        Debug.Log("Fixing missing assembly references...");
        
        // This will force Unity to regenerate project files
        string[] allAsmdefs = AssetDatabase.FindAssets("t:AssemblyDefinitionAsset");
        foreach (string guid in allAsmdefs)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            Debug.Log($"Reimporting: {path}");
        }
        
        Debug.Log("Assembly references fixed. Unity will recompile scripts shortly.");
        AssetDatabase.Refresh();
    }
} 