using System;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using System.Reflection;
using System.Collections.Generic;

namespace Remalux.Editor
{
    // This script prevents the build system from copying files in a way that creates circular dependencies
    [InitializeOnLoad]
    public static class PreventLibraryCopies
    {
        private static readonly string[] ProblematicDlls = new string[]
        {
            "Unity.Barracuda.dll",
            "Unity.XR.ARSubsystems.dll",
            "Unity.XR.ARFoundation.dll",
            "Unity.XR.CoreUtils.dll"
        };
        
        // Static constructor runs when Unity loads
        static PreventLibraryCopies()
        {
            // Hook into the build pipeline early
            EditorApplication.delayCall += ApplyPatches;
        }
        
        private static void ApplyPatches()
        {
            Debug.Log("Applying build system patches to prevent circular references...");
            try
            {
                // Create assembly reference files if they don't exist
                CreateAssemblyReferences();
                
                // Add a define symbol to avoid direct Barracuda usage
                string currentDefines = PlayerSettings.GetScriptingDefineSymbolsForGroup(
                    EditorUserBuildSettings.selectedBuildTargetGroup);
                
                if (!currentDefines.Contains("USE_BARRACUDA_INDIRECTLY"))
                {
                    PlayerSettings.SetScriptingDefineSymbolsForGroup(
                        EditorUserBuildSettings.selectedBuildTargetGroup,
                        currentDefines + ";USE_BARRACUDA_INDIRECTLY");
                    Debug.Log("Added USE_BARRACUDA_INDIRECTLY scripting define symbol");
                }
                
                // Modify assembly loading order if needed
                ModifyAssemblyLoadOrder();
            }
            catch (Exception e)
            {
                Debug.LogError($"Error applying build system patches: {e.Message}\n{e.StackTrace}");
            }
        }
        
        private static void CreateAssemblyReferences()
        {
            string barracudaFixPath = "Assets/asmref-barracuda-fix.asmref";
            if (!System.IO.File.Exists(barracudaFixPath))
            {
                string content = "{\n    \"reference\": \"Unity.Barracuda\"\n}";
                System.IO.File.WriteAllText(barracudaFixPath, content);
                AssetDatabase.ImportAsset(barracudaFixPath);
                Debug.Log("Created Barracuda assembly reference file");
            }
        }
        
        private static void ModifyAssemblyLoadOrder()
        {
            try
            {
                // This is a 'last resort' approach - it uses reflection to 
                // change private Unity build pipeline behavior
                
                // Find the ScriptCompilationPipeline type
                Type pipelineType = Type.GetType("UnityEditor.Scripting.ScriptCompilationPipeline, UnityEditor.CoreModule");
                if (pipelineType != null)
                {
                    // Get the s_assemblyLoadOrder field
                    FieldInfo loadOrderField = pipelineType.GetField("s_assemblyLoadOrder", 
                        BindingFlags.NonPublic | BindingFlags.Static);
                    
                    if (loadOrderField != null)
                    {
                        // Get the current load order
                        var currentOrder = loadOrderField.GetValue(null) as IList<string>;
                        
                        if (currentOrder != null)
                        {
                            bool modified = false;
                            
                            // Ensure problematic DLLs come first in the load order
                            foreach (string dll in ProblematicDlls)
                            {
                                int index = currentOrder.IndexOf(dll);
                                if (index > 0)
                                {
                                    // Move to front
                                    currentOrder.RemoveAt(index);
                                    List<string> newOrder = new List<string>(currentOrder.Count + 1);
                                    newOrder.Add(dll);
                                    foreach (string item in currentOrder)
                                    {
                                        newOrder.Add(item);
                                    }
                                    
                                    // Set back to the field
                                    loadOrderField.SetValue(null, newOrder);
                                    modified = true;
                                    Debug.Log($"Modified assembly load order to prioritize {dll}");
                                }
                            }
                            
                            if (modified)
                            {
                                // Force a domain reload to apply changes
                                EditorUtility.RequestScriptReload();
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Could not modify assembly load order: {e.Message}");
            }
        }
    }
} 