using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text.RegularExpressions;
using System;

namespace Remalux.Editor
{
    /// <summary>
    /// Удаляет прямые ссылки на Unity.Barracuda из всех asmdef файлов проекта
    /// </summary>
    [InitializeOnLoad]
    public class RemoveBarracudaFromAsmdef
    {
        static RemoveBarracudaFromAsmdef()
        {
            EditorApplication.delayCall += RemoveBarracudaReferences;
        }
        
        [MenuItem("Remalux/Remove Barracuda From Asmdef")]
        public static void RemoveBarracudaReferences()
        {
            Debug.Log("Scanning for direct Barracuda references in asmdef files...");
            
            int fixedFiles = 0;
            string[] asmdefFiles = Directory.GetFiles(Application.dataPath, "*.asmdef", SearchOption.AllDirectories);
            
            foreach (string asmdefPath in asmdefFiles)
            {
                try
                {
                    string content = File.ReadAllText(asmdefPath);
                    
                    // Регулярные выражения для поиска различных форматов ссылок на Barracuda
                    string pattern1 = @"""Unity\.Barracuda""";
                    string pattern2 = @"""references"":\s*\[\s*[^\]]*""Unity\.Barracuda""[^\]]*\]";
                    
                    // Для первого шаблона - простая замена
                    string newContent = Regex.Replace(content, pattern1, "\"NOT_BARRACUDA\"");
                    
                    // Для второго шаблона - удаление ссылки из массива
                    if (newContent != content)
                    {
                        // Обрабатываем случай, когда Barracuda в середине массива
                        newContent = Regex.Replace(newContent, @",\s*""NOT_BARRACUDA""", "");
                        newContent = Regex.Replace(newContent, @"""NOT_BARRACUDA""\s*,", "");
                        
                        // Чистим пустые массивы ссылок
                        newContent = Regex.Replace(newContent, @"""references"":\s*\[\s*\]", "\"references\": []");
                        
                        File.WriteAllText(asmdefPath, newContent);
                        fixedFiles++;
                        
                        Debug.Log($"Removed Barracuda reference from {asmdefPath}");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error processing {asmdefPath}: {e.Message}");
                }
            }
            
            if (fixedFiles > 0)
            {
                Debug.Log($"Removed Barracuda references from {fixedFiles} files");
                AssetDatabase.Refresh();
            }
            else
            {
                Debug.Log("No direct Barracuda references found in asmdef files");
            }
        }
    }
} 