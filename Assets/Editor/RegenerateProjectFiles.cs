using UnityEditor;

public class RegenerateProjectFiles
{
      [MenuItem("Tools/Regenerate Project Files")]
      public static void Regenerate()
      {
            // Это вызовет принудительную регенерацию файлов проектов
            System.Type projectWindowUtilType = typeof(UnityEditor.ProjectWindowUtil);
            var syncVSMethod = projectWindowUtilType.GetMethod("SyncSolution",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);

            if (syncVSMethod != null)
            {
                  syncVSMethod.Invoke(null, null);
                  EditorUtility.DisplayDialog("Regenerated", "Project files have been regenerated.", "OK");
            }
            else
            {
                  EditorUtility.DisplayDialog("Error", "Could not find SyncSolution method.", "OK");
            }
      }
}