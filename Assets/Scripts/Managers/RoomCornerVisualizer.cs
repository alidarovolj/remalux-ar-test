using UnityEngine;
using System.Collections.Generic;
using Remalux.AR;

namespace Remalux.AR
{
    public class RoomCornerVisualizer : MonoBehaviour
    {
        [SerializeField] private WallDetectionManager wallDetectionManager;
        [SerializeField] private GameObject cornerPrefab;
        [SerializeField] private float cornerScale = 0.1f;
        [SerializeField] private Color highConfidenceColor = Color.green;
        [SerializeField] private Color lowConfidenceColor = Color.yellow;
        [SerializeField] private float confidenceThreshold = 0.8f;

        private Dictionary<string, GameObject> cornerVisuals = new Dictionary<string, GameObject>();

        private void Start()
        {
            if (wallDetectionManager == null)
            {
                wallDetectionManager = FindFirstObjectByType<WallDetectionManager>();
            }

            if (wallDetectionManager != null)
            {
                wallDetectionManager.OnCornerDetected += OnCornerDetected;
            }
            else
            {
                Debug.LogError("RoomCornerVisualizer: WallDetectionManager не найден!");
                enabled = false;
            }

            if (cornerPrefab == null)
            {
                CreateDefaultCornerPrefab();
            }
        }

        private void CreateDefaultCornerPrefab()
        {
            cornerPrefab = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            cornerPrefab.transform.localScale = Vector3.one * cornerScale;
            cornerPrefab.SetActive(false);
        }

        private void OnCornerDetected(RoomCorner corner)
        {
            string cornerId = $"{corner.wall1Id}_{corner.wall2Id}";

            if (!cornerVisuals.ContainsKey(cornerId))
            {
                GameObject cornerVisual = Instantiate(cornerPrefab, corner.position, Quaternion.identity);
                cornerVisual.transform.parent = transform;
                cornerVisual.SetActive(true);

                // Устанавливаем цвет в зависимости от уверенности
                var renderer = cornerVisual.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material.color = corner.confidence >= confidenceThreshold ? 
                        highConfidenceColor : lowConfidenceColor;
                }

                cornerVisuals[cornerId] = cornerVisual;
            }
            else
            {
                // Обновляем существующий визуал
                var cornerVisual = cornerVisuals[cornerId];
                cornerVisual.transform.position = corner.position;

                var renderer = cornerVisual.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material.color = corner.confidence >= confidenceThreshold ? 
                        highConfidenceColor : lowConfidenceColor;
                }
            }
        }

        private void Update()
        {
            // Очищаем визуалы для углов, которые больше не обнаруживаются
            var currentCorners = wallDetectionManager.GetDetectedCorners();
            var cornersToRemove = new List<string>();

            foreach (var visual in cornerVisuals)
            {
                if (!currentCorners.ContainsKey(visual.Key))
                {
                    cornersToRemove.Add(visual.Key);
                }
            }

            foreach (var cornerId in cornersToRemove)
            {
                if (cornerVisuals.TryGetValue(cornerId, out var visual))
                {
                    Destroy(visual);
                    cornerVisuals.Remove(cornerId);
                }
            }
        }

        private void OnDisable()
        {
            if (wallDetectionManager != null)
            {
                wallDetectionManager.OnCornerDetected -= OnCornerDetected;
            }

            foreach (var visual in cornerVisuals.Values)
            {
                Destroy(visual);
            }
            cornerVisuals.Clear();
        }
    }
} 