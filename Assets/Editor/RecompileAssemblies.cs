using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

public class RecompileAssemblies
{
    [MenuItem("Tools/Recompile All Assemblies")]
    public static void Recompile()
    {
        Debug.Log("Recompiling all assemblies...");
        CompilationPipeline.RequestScriptCompilation();
    }
} 