using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;

/// <summary>
/// Редакторский инструмент для автоматической замены устаревших методов FindObjectOfType и FindObjectsOfType
/// </summary>
public class FindObjectTypeReplacer : EditorWindow
{
    private string[] searchPatterns = new string[]
    {
        @"FindObjectOfType\s*<([^>]+)>\s*\(\s*\)",
        @"FindObjectsOfType\s*<([^>]+)>\s*\(\s*\)",
        @"FindObjectsOfType\s*<([^>]+)>\s*\(\s*true\s*\)",
        @"FindObjectsOfType\s*<([^>]+)>\s*\(\s*false\s*\)"
    };

    private string[] replacementPatterns = new string[]
    {
        "FindAnyObjectByType<$1>()",
        "FindObjectsByType<$1>(FindObjectsSortMode.None)",
        "FindObjectsByType<$1>(FindObjectsInactive.Include, FindObjectsSortMode.None)",
        "FindObjectsByType<$1>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)"
    };

    private bool includeObject = true;
    private bool searchInScripts = true;
    private bool searchInEditorScripts = true;
    private bool previewOnly = true;
    private Vector2 scrollPosition;
    private List<ReplacementInfo> replacements = new List<ReplacementInfo>();

    [MenuItem("Remalux/Инструменты/Заменить устаревшие FindObjectOfType")]
    public static void ShowWindow()
    {
        GetWindow<FindObjectTypeReplacer>("Замена FindObjectOfType");
    }

    private void OnGUI()
    {
        GUILayout.Label("Инструмент для замены устаревших методов FindObjectOfType", EditorStyles.boldLabel);
        GUILayout.Space(10);

        includeObject = EditorGUILayout.Toggle("Добавить префикс Object.", includeObject);
        searchInScripts = EditorGUILayout.Toggle("Искать в Scripts", searchInScripts);
        searchInEditorScripts = EditorGUILayout.Toggle("Искать в Editor Scripts", searchInEditorScripts);
        previewOnly = EditorGUILayout.Toggle("Только предпросмотр (без замены)", previewOnly);

        GUILayout.Space(10);

        if (GUILayout.Button("Найти устаревшие методы"))
        {
            FindObsoleteMethodCalls();
        }

        if (replacements.Count > 0)
        {
            GUILayout.Space(10);
            GUILayout.Label($"Найдено {replacements.Count} устаревших вызовов:", EditorStyles.boldLabel);

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            foreach (var replacement in replacements)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField($"Файл: {replacement.FilePath}");
                EditorGUILayout.LabelField($"Строка: {replacement.LineNumber}");
                EditorGUILayout.LabelField("Оригинал:", EditorStyles.boldLabel);
                EditorGUILayout.TextArea(replacement.OriginalText);
                EditorGUILayout.LabelField("Замена:", EditorStyles.boldLabel);
                EditorGUILayout.TextArea(replacement.ReplacementText);
                EditorGUILayout.EndVertical();
                GUILayout.Space(5);
            }
            EditorGUILayout.EndScrollView();

            if (!previewOnly && GUILayout.Button("Применить все замены"))
            {
                ApplyReplacements();
            }
        }
    }

    private void FindObsoleteMethodCalls()
    {
        replacements.Clear();
        string[] scriptPaths = GetScriptPaths();

        foreach (string path in scriptPaths)
        {
            string content = File.ReadAllText(path);
            bool fileModified = false;

            for (int i = 0; i < searchPatterns.Length; i++)
            {
                string pattern = searchPatterns[i];
                string replacement = replacementPatterns[i];

                if (includeObject && !pattern.StartsWith("Object.") && !pattern.StartsWith(@"UnityEngine\.Object\."))
                {
                    pattern = @"(?<!Object\.)(?<!UnityEngine\.Object\.)" + pattern;
                    replacement = "Object." + replacement;
                }

                MatchCollection matches = Regex.Matches(content, pattern);
                foreach (Match match in matches)
                {
                    string originalText = match.Value;
                    string replacedText = Regex.Replace(originalText, pattern, replacement);
                    
                    // Определяем номер строки
                    int lineNumber = content.Substring(0, match.Index).Split('\n').Length;
                    
                    replacements.Add(new ReplacementInfo
                    {
                        FilePath = path,
                        LineNumber = lineNumber,
                        OriginalText = originalText,
                        ReplacementText = replacedText,
                        StartIndex = match.Index,
                        Length = match.Length
                    });
                    
                    fileModified = true;
                }
            }
        }
    }

    private void ApplyReplacements()
    {
        Dictionary<string, List<ReplacementInfo>> fileReplacements = new Dictionary<string, List<ReplacementInfo>>();
        
        // Группируем замены по файлам
        foreach (var replacement in replacements)
        {
            if (!fileReplacements.ContainsKey(replacement.FilePath))
            {
                fileReplacements[replacement.FilePath] = new List<ReplacementInfo>();
            }
            fileReplacements[replacement.FilePath].Add(replacement);
        }
        
        // Применяем замены для каждого файла
        foreach (var filePath in fileReplacements.Keys)
        {
            string content = File.ReadAllText(filePath);
            var replacementsInFile = fileReplacements[filePath];
            
            // Сортируем замены в обратном порядке, чтобы индексы не сбивались при замене
            replacementsInFile.Sort((a, b) => b.StartIndex.CompareTo(a.StartIndex));
            
            foreach (var replacement in replacementsInFile)
            {
                content = content.Remove(replacement.StartIndex, replacement.Length)
                    .Insert(replacement.StartIndex, replacement.ReplacementText);
            }
            
            File.WriteAllText(filePath, content);
        }
        
        AssetDatabase.Refresh();
        replacements.Clear();
        Debug.Log("Все замены успешно применены!");
    }

    private string[] GetScriptPaths()
    {
        List<string> paths = new List<string>();
        
        if (searchInScripts)
        {
            string[] scriptGuids = AssetDatabase.FindAssets("t:Script", new[] { "Assets/Scripts" });
            foreach (string guid in scriptGuids)
            {
                paths.Add(AssetDatabase.GUIDToAssetPath(guid));
            }
        }
        
        if (searchInEditorScripts)
        {
            string[] editorScriptGuids = AssetDatabase.FindAssets("t:Script", new[] { "Assets/Editor" });
            foreach (string guid in editorScriptGuids)
            {
                paths.Add(AssetDatabase.GUIDToAssetPath(guid));
            }
        }
        
        return paths.ToArray();
    }

    private class ReplacementInfo
    {
        public string FilePath;
        public int LineNumber;
        public string OriginalText;
        public string ReplacementText;
        public int StartIndex;
        public int Length;
    }
} 