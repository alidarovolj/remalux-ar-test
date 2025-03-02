using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Remalux.AR
{
    [System.Serializable]
    public class WallData
    {
        public string id;
        public Vector3 position;
        public Vector3 rotation;
        public Vector3 scale;
        public string colorCode;
        public float area;
    }

    [System.Serializable]
    public class ProjectData
    {
        public string name;
        public string date;
        public List<WallData> walls;
        public Dictionary<string, float> paintRequirements;
        public string thumbnailPath;
    }

    public class ProjectManager : MonoBehaviour
    {
        [Header("Настройки проекта")]
        [SerializeField] private string projectsFolder = "Projects";
        [SerializeField] private int maxProjects = 10;
        [SerializeField] private Camera screenshotCamera;

        private ProjectData currentProject;
        private List<ProjectData> savedProjects = new List<ProjectData>();
        private WallPaintingManager wallPaintingManager;
        private ColorManager colorManager;

        private void Start()
        {
            wallPaintingManager = FindFirstObjectByType<WallPaintingManager>();
            colorManager = FindFirstObjectByType<ColorManager>();
            LoadProjects();
        }

        public void CreateNewProject(string name)
        {
            currentProject = new ProjectData
            {
                name = name,
                date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                walls = new List<WallData>(),
                paintRequirements = new Dictionary<string, float>()
            };
        }

        public void SaveCurrentProject()
        {
            if (currentProject == null) return;

            // Обновляем данные о стенах
            UpdateWallData();

            // Создаем скриншот
            currentProject.thumbnailPath = CaptureScreenshot();

            // Сохраняем проект
            string json = JsonUtility.ToJson(currentProject);
            string projectPath = Path.Combine(Application.persistentDataPath, projectsFolder, $"{currentProject.name}.json");

            Directory.CreateDirectory(Path.GetDirectoryName(projectPath));
            File.WriteAllText(projectPath, json);

            // Обновляем список проектов
            if (!savedProjects.Any(p => p.name == currentProject.name))
            {
                savedProjects.Add(currentProject);
                if (savedProjects.Count > maxProjects)
                    savedProjects.RemoveAt(0);
            }
        }

        public void LoadProject(string projectName)
        {
            string projectPath = Path.Combine(Application.persistentDataPath, projectsFolder, $"{projectName}.json");
            if (!File.Exists(projectPath)) return;

            string json = File.ReadAllText(projectPath);
            currentProject = JsonUtility.FromJson<ProjectData>(json);

            // Восстанавливаем стены
            RestoreWalls();
        }

        private void UpdateWallData()
        {
            currentProject.walls.Clear();
            currentProject.paintRequirements.Clear();

            var walls = wallPaintingManager.GetPaintedWalls();
            foreach (var wall in walls)
            {
                var wallData = new WallData
                {
                    id = wall.GetInstanceID().ToString(),
                    position = wall.transform.position,
                    rotation = wall.transform.eulerAngles,
                    scale = wall.transform.localScale,
                    colorCode = colorManager.GetCurrentDuluxColor()?.code,
                    area = CalculateWallArea(wall)
                };

                currentProject.walls.Add(wallData);

                // Обновляем требования к краске
                if (!string.IsNullOrEmpty(wallData.colorCode))
                {
                    if (!currentProject.paintRequirements.ContainsKey(wallData.colorCode))
                        currentProject.paintRequirements[wallData.colorCode] = 0;

                    currentProject.paintRequirements[wallData.colorCode] += CalculatePaintRequired(wallData.area);
                }
            }
        }

        private void RestoreWalls()
        {
            wallPaintingManager.ClearWalls();

            foreach (var wallData in currentProject.walls)
            {
                var duluxColor = colorManager.GetColorByCode(wallData.colorCode);
                if (duluxColor != null)
                {
                    wallPaintingManager.CreateWall(
                        wallData.position,
                        Quaternion.Euler(wallData.rotation),
                        wallData.scale,
                        duluxColor.color
                    );
                }
            }
        }

        private float CalculateWallArea(GameObject wall)
        {
            var meshFilter = wall.GetComponent<MeshFilter>();
            if (meshFilter == null || meshFilter.mesh == null) return 0;

            // Используем размеры меша и масштаб объекта для расчета площади
            var bounds = meshFilter.mesh.bounds;
            var scale = wall.transform.localScale;
            return bounds.size.x * scale.x * bounds.size.y * scale.y;
        }

        private float CalculatePaintRequired(float area)
        {
            // Примерный расчет необходимого количества краски
            // 1 литр на 10 квадратных метров при двойном покрытии
            return area * 0.1f * 2;
        }

        private string CaptureScreenshot()
        {
            // Создаем уникальное имя файла
            string fileName = $"thumbnail_{DateTime.Now.Ticks}.png";
            string filePath = Path.Combine(Application.persistentDataPath, projectsFolder, fileName);

            // Создаем рендертекстуру
            RenderTexture rt = new RenderTexture(256, 256, 24);
            screenshotCamera.targetTexture = rt;
            screenshotCamera.Render();

            // Создаем текстуру и читаем в неё пиксели
            Texture2D screenshot = new Texture2D(256, 256, TextureFormat.RGB24, false);
            RenderTexture.active = rt;
            screenshot.ReadPixels(new Rect(0, 0, 256, 256), 0, 0);
            screenshot.Apply();

            // Сохраняем файл
            byte[] bytes = screenshot.EncodeToPNG();
            File.WriteAllBytes(filePath, bytes);

            // Очищаем ресурсы
            screenshotCamera.targetTexture = null;
            RenderTexture.active = null;
            Destroy(rt);
            Destroy(screenshot);

            return fileName;
        }

        private void LoadProjects()
        {
            string projectsPath = Path.Combine(Application.persistentDataPath, projectsFolder);
            if (!Directory.Exists(projectsPath)) return;

            var projectFiles = Directory.GetFiles(projectsPath, "*.json");
            savedProjects.Clear();

            foreach (var file in projectFiles)
            {
                string json = File.ReadAllText(file);
                var project = JsonUtility.FromJson<ProjectData>(json);
                savedProjects.Add(project);
            }

            // Сортируем по дате (новые первыми)
            savedProjects = savedProjects
                .OrderByDescending(p => DateTime.Parse(p.date))
                .Take(maxProjects)
                .ToList();
        }

        public List<ProjectData> GetSavedProjects()
        {
            return savedProjects;
        }

        public ProjectData GetCurrentProject()
        {
            return currentProject;
        }

        public Dictionary<string, float> GetPaintRequirements()
        {
            return currentProject?.paintRequirements;
        }
    }
}