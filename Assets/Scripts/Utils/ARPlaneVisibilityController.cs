using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Collections.Generic;
using System.Collections;

namespace Remalux.AR
{
      /// <summary>
      /// Контроллер видимости AR плоскостей
      /// </summary>
      public class ARPlaneVisibilityController : MonoBehaviour
      {
            [Header("AR компоненты")]
            [SerializeField] public ARPlaneManager planeManager;

            [Header("Внешний вид")]
            [SerializeField] public Material wallMaterial;
            [SerializeField] public Material floorMaterial;
            [SerializeField] private float planeAlpha = 0.5f;

            [Header("Настройки")]
            [SerializeField] private bool enablePlaneVisibility = true;
            [SerializeField] private bool highlightVerticalPlanes = true;

            // Делегат и событие для обнаружения стен
            public delegate void WallDetectedHandler(ARPlane wall);
#pragma warning disable 0067
            public event WallDetectedHandler onWallDetected;
#pragma warning restore 0067

            private Dictionary<TrackableId, Material> originalMaterials = new Dictionary<TrackableId, Material>();
            private List<ARPlane> verticalPlanes = new List<ARPlane>();
            private List<ARPlane> horizontalPlanes = new List<ARPlane>();
            private Dictionary<TrackableId, GameObject> planeVisualizations = new Dictionary<TrackableId, GameObject>();

            private Material _cachedWallMaterial;
            private Material _cachedFloorMaterial;

            private void Awake()
            {
                  if (planeManager == null)
                  {
                        planeManager = GetComponent<ARPlaneManager>();
                        if (planeManager == null)
                        {
                              planeManager = FindFirstObjectByType<ARPlaneManager>();
                        }
                  }

                  // Настраиваем ARPlaneManager
                  if (!planeManager.enabled)
                  {
                        Debug.LogWarning("ARPlaneManager отключен! Включаем...");
                        planeManager.enabled = true;
                  }

                  // Убеждаемся, что настройки плоскостей корректны
                  planeManager.requestedDetectionMode = PlaneDetectionMode.Vertical | PlaneDetectionMode.Horizontal;

                  // Проверяем настройки обнаружения плоскостей
                  if (!planeManager.planePrefab)
                  {
                        Debug.LogWarning("ARPlaneManager не имеет установленного префаба плоскости!");

                        // Создаем простой префаб плоскости, если он не задан
                        GameObject planePrefab = new GameObject("AR Plane Prefab");
                        planePrefab.AddComponent<MeshFilter>();
                        MeshRenderer renderer = planePrefab.AddComponent<MeshRenderer>();

                        // Устанавливаем материал
                        Material defaultMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                        defaultMaterial.color = new Color(0.5f, 0.5f, 0.5f, planeAlpha);
                        renderer.material = defaultMaterial;

                        // Устанавливаем префаб
                        planeManager.planePrefab = planePrefab;
                        Debug.Log("Создан стандартный префаб для ARPlaneManager");
                  }
                  else
                  {
                        Debug.Log($"ARPlaneManager использует префаб: {planeManager.planePrefab.name}");

                        // Проверяем наличие MeshRenderer в префабе
                        MeshRenderer prefabRenderer = planeManager.planePrefab.GetComponent<MeshRenderer>();
                        if (prefabRenderer == null)
                        {
                              Debug.LogWarning("Префаб плоскости ARPlaneManager не имеет компонента MeshRenderer!");
                              prefabRenderer = planeManager.planePrefab.AddComponent<MeshRenderer>();

                              // Добавляем MeshFilter, если его нет
                              if (!planeManager.planePrefab.GetComponent<MeshFilter>())
                              {
                                    planeManager.planePrefab.AddComponent<MeshFilter>();
                              }

                              // Устанавливаем материал
                              Material defaultMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                              defaultMaterial.color = new Color(0.5f, 0.5f, 0.5f, planeAlpha);
                              prefabRenderer.material = defaultMaterial;

                              Debug.Log("Добавлены компоненты к префабу плоскости");
                        }

                        // Убеждаемся, что рендерер плоскости включен
                        prefabRenderer.enabled = true;
                  }

                  // Инициализация кэшированных материалов
                  InitializeCachedMaterials();
            }

            private void InitializeCachedMaterials()
            {
                  // Создаем материалы по умолчанию, если они не заданы
                  if (wallMaterial == null)
                  {
                        _cachedWallMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                        _cachedWallMaterial.color = new Color(1.0f, 0.2f, 0.2f, 0.7f);
                  }
                  else
                  {
                        _cachedWallMaterial = new Material(wallMaterial);
                  }

                  if (floorMaterial == null)
                  {
                        _cachedFloorMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                        _cachedFloorMaterial.color = new Color(0.2f, 1.0f, 0.2f, 0.7f);
                  }
                  else
                  {
                        _cachedFloorMaterial = new Material(floorMaterial);
                  }
            }

            private void OnEnable()
            {
                  if (planeManager != null)
                  {
                        planeManager.trackablesChanged.AddListener(OnPlanesChanged);
                  }
                  else
                  {
                        Debug.LogWarning("ARPlaneManager is null in OnEnable");
                  }
            }

            private void OnDisable()
            {
                  if (planeManager != null)
                  {
                        planeManager.trackablesChanged.RemoveListener(OnPlanesChanged);
                  }
            }

            /// <summary>
            /// Обработчик события изменения плоскостей
            /// </summary>
            private void OnPlanesChanged(ARTrackablesChangedEventArgs<ARPlane> eventArgs)
            {
                  if (!enablePlaneVisibility)
                  {
                        Debug.Log("Plane visibility disabled. Ignoring plane changes.");
                        return;
                  }

                  // Ограничиваем объем работы для большого количества плоскостей
                  bool hasSignificantChanges = false;
                  int processedCount = 0;
                  int maxProcessedPerFrame = 10; // Максимальное число плоскостей для обработки за один кадр

                  // Обработка добавленных плоскостей
                  if (eventArgs.added != null && eventArgs.added.Count > 0)
                  {
                        Debug.Log($"Added {eventArgs.added.Count} planes");
                        hasSignificantChanges = true;

                        foreach (ARPlane plane in eventArgs.added)
                        {
                              if (plane == null) continue;

                              processedCount++;
                              if (processedCount > maxProcessedPerFrame) break;

                              CreateCustomPlaneVisualization(plane);
                              EnsureMeshCollider(plane);

                              // Если это стена, добавляем в список стен
                              if (IsVerticalPlane(plane))
                              {
                                    verticalPlanes.Add(plane);
                                    Debug.Log($"Vertical plane added: {plane.trackableId}");
                              }
                              else
                              {
                                    horizontalPlanes.Add(plane);
                                    Debug.Log($"Horizontal plane added: {plane.trackableId}");
                              }
                        }
                  }

                  // Сбрасываем счетчик для обработки обновленных плоскостей
                  processedCount = 0;

                  // Обработка обновленных плоскостей - обрабатываем только ограниченное число
                  if (eventArgs.updated != null && eventArgs.updated.Count > 0)
                  {
                        Debug.Log($"Updated {eventArgs.updated.Count} planes");

                        foreach (ARPlane plane in eventArgs.updated)
                        {
                              if (plane == null) continue;

                              processedCount++;
                              if (processedCount > maxProcessedPerFrame) break;

                              // Обновляем визуализацию
                              CreateCustomPlaneVisualization(plane);
                              EnsureMeshCollider(plane);

                              // Проверяем, изменилась ли классификация
                              bool isVertical = IsVerticalPlane(plane);
                              bool inVerticalList = verticalPlanes.Contains(plane);
                              bool inHorizontalList = horizontalPlanes.Contains(plane);

                              // Если плоскость вертикальная, но не в списке вертикальных
                              if (isVertical && !inVerticalList)
                              {
                                    if (inHorizontalList)
                                    {
                                          horizontalPlanes.Remove(plane);
                                    }
                                    verticalPlanes.Add(plane);
                                    Debug.Log($"Plane {plane.trackableId} reclassified as vertical");
                                    hasSignificantChanges = true;
                              }
                              // Если плоскость горизонтальная, но не в списке горизонтальных
                              else if (!isVertical && !inHorizontalList)
                              {
                                    if (inVerticalList)
                                    {
                                          verticalPlanes.Remove(plane);
                                    }
                                    horizontalPlanes.Add(plane);
                                    Debug.Log($"Plane {plane.trackableId} reclassified as horizontal");
                                    hasSignificantChanges = true;
                              }
                        }
                  }

                  // Сбрасываем счетчик для обработки удаленных плоскостей
                  processedCount = 0;

                  // Обработка удаленных плоскостей
                  if (eventArgs.removed != null && eventArgs.removed.Count > 0)
                  {
                        Debug.Log($"Removed {eventArgs.removed.Count} planes");
                        hasSignificantChanges = true;

                        foreach (var kvp in eventArgs.removed)
                        {
                              ARPlane plane = kvp.Value;
                              if (plane == null) continue;

                              processedCount++;
                              if (processedCount > maxProcessedPerFrame) break;

                              // Удаляем соответствующую визуализацию
                              if (planeVisualizations.TryGetValue(plane.trackableId, out GameObject visualization))
                              {
                                    Debug.Log($"Destroying visualization for plane {plane.trackableId}");
                                    Destroy(visualization);
                                    planeVisualizations.Remove(plane.trackableId);
                              }

                              // Удаляем из соответствующего списка
                              verticalPlanes.Remove(plane);
                              horizontalPlanes.Remove(plane);
                        }
                  }

                  // Обновляем общую видимость только при значительных изменениях
                  if (hasSignificantChanges)
                  {
                        UpdateAllVisualizations();
                  }
            }

            /// <summary>
            /// Обновляет видимость AR плоскости
            /// </summary>
            public void UpdatePlaneVisibility(ARPlane plane)
            {
                  if (plane == null) return;

                  // Проверяем, что плоскость активна
                  if (!plane.gameObject.activeInHierarchy)
                  {
                        Debug.LogWarning($"Плоскость {plane.trackableId} не активна в иерархии");
                        try
                        {
                              // Пытаемся активировать плоскость
                              plane.gameObject.SetActive(true);

                              // Делаем отложенную вторую попытку через корутину
                              StartCoroutine(DelayedActivationCheck(plane));
                        }
                        catch (System.Exception e)
                        {
                              Debug.LogError($"Ошибка при активации плоскости {plane.trackableId}: {e.Message}");
                              return;
                        }
                  }

                  // Получаем компонент рендерера
                  MeshRenderer renderer = plane.GetComponent<MeshRenderer>();
                  if (renderer == null)
                  {
                        Debug.LogWarning($"MeshRenderer на плоскости {plane.trackableId} не найден");

                        // Пробуем добавить рендерер, если его нет
                        renderer = plane.gameObject.AddComponent<MeshRenderer>();
                        MeshFilter meshFilter = plane.GetComponent<MeshFilter>();
                        if (meshFilter == null)
                        {
                              meshFilter = plane.gameObject.AddComponent<MeshFilter>();
                              Debug.Log($"Добавлен MeshFilter на плоскость {plane.trackableId}");
                        }

                        Debug.Log($"Добавлен MeshRenderer на плоскость {plane.trackableId}");
                  }

                  // Убеждаемся, что рендерер включен
                  if (!renderer.enabled)
                  {
                        renderer.enabled = true;
                        Debug.Log($"Включен рендерер на плоскости {plane.trackableId}");
                  }

                  // Сохраняем оригинальный материал, если еще не сохранен
                  if (!originalMaterials.ContainsKey(plane.trackableId))
                  {
                        originalMaterials[plane.trackableId] = renderer.material;
                        Debug.Log($"Сохранен оригинальный материал для плоскости {plane.trackableId}");
                  }

                  // Настраиваем видимость и материал
                  if (enablePlaneVisibility)
                  {
                        // Делаем плоскость видимой
                        renderer.enabled = true;

                        // Определяем тип плоскости (стена или пол)
                        bool isWall = IsVerticalPlane(plane);

                        // Применяем соответствующий материал
                        if (isWall && highlightVerticalPlanes)
                        {
                              if (wallMaterial != null)
                              {
                                    // Яркий и насыщенный красный цвет для стен, чтобы пользователю было понятно, что их можно красить
                                    Color wallColor = new Color(1.0f, 0.2f, 0.2f, 0.8f);

                                    // Создаем новый экземпляр материала для каждой плоскости
                                    Material wallMat = new Material(wallMaterial);
                                    wallMat.color = wallColor;

                                    // Применяем материал
                                    renderer.material = wallMat;

                                    // Установка рендеринга в режиме прозрачности
                                    renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                                    renderer.receiveShadows = false;

                                    Debug.Log($"Применен материал стены к плоскости {plane.trackableId}");
                                    Debug.Log($"Цвет материала стены: {wallMat.color}, непрозрачность: {wallMat.color.a}");
                              }
                              else
                              {
                                    Debug.LogWarning("Материал стены не определен");
                              }

                              // Добавляем MeshCollider для взаимодействия со стеной
                              EnsureMeshCollider(plane.gameObject);
                        }
                        else
                        {
                              if (floorMaterial != null)
                              {
                                    // Задаем более светлый и полупрозрачный цвет для материала пола
                                    Color floorColor = new Color(0.2f, 1.0f, 0.2f, 0.6f);

                                    // Создаем новый экземпляр материала для каждой плоскости
                                    Material floorMat = new Material(floorMaterial);
                                    floorMat.color = floorColor;

                                    // Применяем материал
                                    renderer.material = floorMat;

                                    // Установка рендеринга в режиме прозрачности
                                    renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                                    renderer.receiveShadows = false;

                                    Debug.Log($"Применен материал пола к плоскости {plane.trackableId}");
                                    Debug.Log($"Цвет материала пола: {floorMat.color}, непрозрачность: {floorMat.color.a}");
                              }
                              else
                              {
                                    Debug.LogWarning("Материал пола не определен");
                              }
                        }
                  }
                  else
                  {
                        // Делаем плоскость невидимой
                        renderer.enabled = false;
                        Debug.Log($"Плоскость {plane.trackableId} сделана невидимой");
                  }
            }

            /// <summary>
            /// Корутина для отложенной проверки активации плоскости
            /// </summary>
            private System.Collections.IEnumerator DelayedActivationCheck(ARPlane plane = null)
            {
                  yield return new WaitForSeconds(0.1f);

                  if (plane != null && !plane.gameObject.activeInHierarchy)
                  {
                        // Пробуем еще раз активировать
                        try
                        {
                              plane.gameObject.SetActive(true);
                              Debug.Log($"Повторная попытка активации плоскости: {plane.trackableId}");

                              // Проверяем родителя
                              if (plane.transform.parent != null)
                              {
                                    plane.transform.parent.gameObject.SetActive(true);
                                    Debug.Log($"Активирован родитель плоскости: {plane.transform.parent.name}");
                              }
                        }
                        catch (System.Exception e)
                        {
                              Debug.LogError($"Ошибка при повторной активации плоскости: {e.Message}");
                        }
                  }
            }

            /// <summary>
            /// Проверяет, является ли плоскость вертикальной (стеной)
            /// </summary>
            private bool IsVerticalPlane(ARPlane plane)
            {
                  if (plane == null)
                  {
                        Debug.LogError("IsVerticalPlane: ARPlane is null");
                        return false;
                  }

                  // В AR Foundation 6.0 используем Vector3.up и проверяем угол между нормалью плоскости и вектором вверх
                  Vector3 planeNormal = plane.normal;
                  float angle = Vector3.Angle(planeNormal, Vector3.up);

                  // Если угол около 90 градусов (с некоторым допуском), то это вертикальная плоскость
                  bool isVertical = angle > 45 && angle < 135;

                  if (isVertical)
                  {
                        Debug.Log($"Plane {plane.trackableId} identified as vertical wall (angle: {angle})");
                  }
                  else
                  {
                        Debug.Log($"Plane {plane.trackableId} identified as horizontal floor/ceiling (angle: {angle})");
                  }

                  return isVertical;
            }

            /// <summary>
            /// Убеждается, что у плоскости есть MeshCollider
            /// </summary>
            private void EnsureMeshCollider(GameObject planeObject)
            {
                  if (planeObject == null)
                  {
                        Debug.LogError("EnsureMeshCollider: GameObject is null");
                        return;
                  }

                  MeshCollider meshCollider = planeObject.GetComponent<MeshCollider>();
                  if (meshCollider == null)
                  {
                        Debug.Log($"Добавляем MeshCollider к {planeObject.name}");
                        meshCollider = planeObject.AddComponent<MeshCollider>();
                  }

                  // Проверяем, есть ли у объекта MeshFilter с mesh
                  MeshFilter meshFilter = planeObject.GetComponent<MeshFilter>();
                  if (meshFilter != null && meshFilter.mesh != null)
                  {
                        meshCollider.sharedMesh = meshFilter.mesh;
                        Debug.Log($"Установлен shared mesh для MeshCollider на {planeObject.name}");
                  }
                  else
                  {
                        Debug.LogWarning($"MeshFilter или mesh не найден на {planeObject.name}");
                  }
            }

            // Overload for ARPlane objects
            private void EnsureMeshCollider(ARPlane plane)
            {
                  if (plane == null)
                  {
                        Debug.LogError("EnsureMeshCollider: ARPlane is null");
                        return;
                  }

                  EnsureMeshCollider(plane.gameObject);
            }

            /// <summary>
            /// Обновляет видимость для всех существующих плоскостей
            /// </summary>
            public void UpdateAllPlanesVisibility()
            {
                  if (planeManager == null) return;

                  foreach (ARPlane plane in planeManager.trackables)
                  {
                        UpdatePlaneVisibility(plane);
                  }
            }

            /// <summary>
            /// Включает или отключает видимость AR плоскостей
            /// </summary>
            public void SetPlaneVisibility(bool visible)
            {
                  enablePlaneVisibility = visible;
                  UpdateAllPlanesVisibility();
            }

            /// <summary>
            /// Включает или отключает подсветку вертикальных плоскостей (стен)
            /// </summary>
            public void SetHighlightWalls(bool highlight)
            {
                  highlightVerticalPlanes = highlight;
                  UpdateAllPlanesVisibility();
            }

            /// <summary>
            /// Сбрасывает все плоскости в их исходное состояние
            /// Восстанавливает оригинальные материалы и настройки
            /// </summary>
            public void ResetAllPlanes()
            {
                  if (planeManager == null)
                  {
                        Debug.LogWarning("AR Plane Manager не найден при попытке сброса плоскостей");
                        return;
                  }

                  Debug.Log("Сброс всех плоскостей в исходное состояние...");

                  // Перебираем все плоскости и восстанавливаем их оригинальные материалы
                  foreach (ARPlane plane in planeManager.trackables)
                  {
                        if (plane != null)
                        {
                              TrackableId trackableId = plane.trackableId;
                              // Восстанавливаем оригинальный материал, если он существует
                              if (originalMaterials.ContainsKey(trackableId))
                              {
                                    GameObject planeObj = plane.gameObject;
                                    if (planeObj != null)
                                    {
                                          MeshRenderer renderer = planeObj.GetComponentInChildren<MeshRenderer>();
                                          if (renderer != null)
                                          {
                                                renderer.material = originalMaterials[trackableId];
                                                Debug.Log($"Восстановлен оригинальный материал для плоскости {trackableId}");
                                          }
                                    }
                              }
                        }
                  }

                  // Обновляем видимость всех плоскостей
                  UpdateAllPlanesVisibility();
            }

            /// <summary>
            /// Принудительно показывает все плоскости
            /// </summary>
            public void ForceShowAllPlanes()
            {
                  if (planeManager == null)
                  {
                        Debug.LogWarning("AR Plane Manager не найден при попытке форсированного показа плоскостей");
                        return;
                  }

                  // Убеждаемся, что AR Plane Manager включен
                  if (!planeManager.enabled)
                  {
                        planeManager.enabled = true;
                        Debug.Log("AR Plane Manager был отключен и теперь включен");
                  }

                  // Убеждаемся, что настройки плоскостей корректны
                  planeManager.requestedDetectionMode = PlaneDetectionMode.Vertical | PlaneDetectionMode.Horizontal;

                  // Устанавливаем флаг видимости в true
                  enablePlaneVisibility = true;

                  // Для начала удалим все существующие визуализации для гарантии чистого обновления
                  foreach (ARPlane plane in planeManager.trackables)
                  {
                        if (plane != null)
                        {
                              RemoveCustomVisualization(plane);
                        }
                  }

                  Debug.Log($"ВАЖНО: Начинаем принудительную активацию плоскостей. Всего плоскостей: {planeManager.trackables.count}");

                  // Перебираем все плоскости и принудительно их активируем
                  int activatedCount = 0;
                  foreach (ARPlane plane in planeManager.trackables)
                  {
                        if (plane != null && plane.gameObject != null)
                        {
                              // Принудительно активируем игровой объект плоскости
                              if (!plane.gameObject.activeSelf)
                              {
                                    plane.gameObject.SetActive(true);
                                    Debug.Log($"Активирован объект плоскости: {plane.trackableId}");
                              }

                              // Создаем новую пользовательскую визуализацию
                              GameObject visualization = CreateCustomPlaneVisualization(plane);
                              if (visualization != null)
                              {
                                    // Делаем дополнительную проверку, что визуализация активирована
                                    if (!visualization.activeSelf)
                                    {
                                          visualization.SetActive(true);
                                    }

                                    // Принудительно поднимаем визуализацию по Y оси для лучшей видимости
                                    visualization.transform.localPosition = new Vector3(0, 0.02f, 0);

                                    // Применяем более яркие цвета на месте
                                    MeshRenderer renderer = visualization.GetComponent<MeshRenderer>();
                                    if (renderer != null && renderer.material != null)
                                    {
                                          bool isWall = IsVerticalPlane(plane);
                                          if (isWall)
                                          {
                                                // Очень яркий красный для стен
                                                renderer.material.color = new Color(1.0f, 0.0f, 0.0f, 1.0f);
                                          }
                                          else
                                          {
                                                // Очень яркий зеленый для пола
                                                renderer.material.color = new Color(0.0f, 1.0f, 0.0f, 1.0f);
                                          }

                                          // Усиливаем яркость материала
                                          if (renderer.material.HasProperty("_EmissionColor"))
                                          {
                                                renderer.material.EnableKeyword("_EMISSION");
                                                renderer.material.SetColor("_EmissionColor", renderer.material.color * 2.0f);
                                          }
                                    }

                                    activatedCount++;
                                    Debug.Log($"СОЗДАНА И АКТИВИРОВАНА ВИЗУАЛИЗАЦИЯ ДЛЯ ПЛОСКОСТИ {plane.trackableId} (ПОЗИЦИЯ: {plane.transform.position})");
                              }

                              // Обеспечиваем наличие MeshCollider для взаимодействия
                              EnsureMeshCollider(plane.gameObject);
                        }
                  }

                  Debug.Log($"ВАЖНО: Принудительно активировано {activatedCount} плоскостей из {planeManager.trackables.count} доступных");

                  // Запускаем проверку видимости плоскостей
                  StartCoroutine(CheckPlanesVisibilityAfterDelay());
            }

            /// <summary>
            /// Создает специальную визуализацию для AR плоскости
            /// </summary>
            private GameObject CreateCustomPlaneVisualization(ARPlane plane)
            {
                  if (plane == null || !plane.gameObject.activeInHierarchy)
                  {
                        return null;
                  }

                  // Проверяем, существует ли уже визуализация для этой плоскости
                  string visualName = $"CustomVisual_{plane.trackableId}";
                  Transform existingVisual = plane.transform.Find(visualName);

                  if (existingVisual != null)
                  {
                        // Если визуализация уже существует, проверяем её состояние
                        if (!existingVisual.gameObject.activeSelf)
                        {
                              existingVisual.gameObject.SetActive(true);
                        }

                        // Обновляем визуализацию только если меш плоскости изменился
                        MeshFilter planeMeshFilter = plane.GetComponent<MeshFilter>();
                        MeshFilter visualMeshFilter = existingVisual.GetComponent<MeshFilter>();

                        if (planeMeshFilter != null && visualMeshFilter != null &&
                            planeMeshFilter.mesh != null && visualMeshFilter.mesh != null &&
                            planeMeshFilter.mesh.vertexCount != visualMeshFilter.mesh.vertexCount)
                        {
                              // Если меш изменился, копируем его
                              UpdatePlaneVisualizationMesh(existingVisual.gameObject, plane);
                        }

                        return existingVisual.gameObject;
                  }

                  // Создаем новый объект для визуализации
                  GameObject visualObject = new GameObject(visualName);
                  visualObject.transform.SetParent(plane.transform, false);
                  visualObject.transform.localPosition = new Vector3(0, 0.001f, 0); // Немного поднимаем над плоскостью
                  visualObject.transform.localRotation = Quaternion.identity;

                  // Добавляем компоненты
                  MeshFilter meshFilter = visualObject.AddComponent<MeshFilter>();
                  MeshRenderer meshRenderer = visualObject.AddComponent<MeshRenderer>();

                  // Устанавливаем настройки рендеринга
                  meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                  meshRenderer.receiveShadows = false;
                  meshRenderer.allowOcclusionWhenDynamic = false;

                  // Определяем тип плоскости и применяем соответствующий материал
                  bool isWall = IsVerticalPlane(plane);

                  if (isWall)
                  {
                        // Для стен используем кэшированный материал стены
                        meshRenderer.material = _cachedWallMaterial;
                  }
                  else
                  {
                        // Для пола используем кэшированный материал пола
                        meshRenderer.material = _cachedFloorMaterial;
                  }

                  // Копируем меш из плоскости или создаем прямоугольный меш
                  UpdatePlaneVisualizationMesh(visualObject, plane);

                  // Отключаем стандартный рендерер AR плоскости, чтобы избежать наложения
                  MeshRenderer planeRenderer = plane.GetComponent<MeshRenderer>();
                  if (planeRenderer != null)
                  {
                        planeRenderer.enabled = false;
                  }

                  // Делаем объект визуализации активным
                  visualObject.SetActive(true);

                  // Сохраняем ссылку на визуализацию в словаре
                  planeVisualizations[plane.trackableId] = visualObject;

                  return visualObject;
            }

            /// <summary>
            /// Обновляет меш для визуализации плоскости
            /// </summary>
            private void UpdatePlaneVisualizationMesh(GameObject visualObject, ARPlane plane)
            {
                  if (visualObject == null || plane == null)
                        return;

                  MeshFilter meshFilter = visualObject.GetComponent<MeshFilter>();
                  if (meshFilter == null)
                        return;

                  // Пытаемся взять меш из компонента плоскости
                  MeshFilter planeMeshFilter = plane.GetComponent<MeshFilter>();

                  if (planeMeshFilter != null && planeMeshFilter.mesh != null)
                  {
                        try
                        {
                              // Копируем меш
                              Mesh sharedMesh = planeMeshFilter.sharedMesh;
                              if (sharedMesh != null)
                              {
                                    // Создаем копию меша
                                    Mesh newMesh = new Mesh();
                                    newMesh.vertices = sharedMesh.vertices;
                                    newMesh.triangles = sharedMesh.triangles;
                                    newMesh.normals = sharedMesh.normals;
                                    newMesh.uv = sharedMesh.uv;

                                    meshFilter.mesh = newMesh;
                                    return;
                              }
                        }
                        catch (System.Exception e)
                        {
                              Debug.LogError($"Ошибка при копировании меша: {e.Message}");
                        }
                  }

                  // Если не удалось скопировать меш, создаем простой прямоугольник
                  Mesh simpleMesh = new Mesh();

                  // Получаем размеры плоскости
                  Vector2 size = plane.size;
                  float width = size.x;
                  float height = size.y;

                  // Если размеры слишком малы, используем минимальные значения
                  width = Mathf.Max(width, 0.1f);
                  height = Mathf.Max(height, 0.1f);

                  // Создаем простой прямоугольник
                  Vector3[] vertices = new Vector3[4]
                  {
                        new Vector3(-width/2, 0, -height/2),
                        new Vector3(width/2, 0, -height/2),
                        new Vector3(width/2, 0, height/2),
                        new Vector3(-width/2, 0, height/2)
                  };

                  int[] triangles = new int[6]
                  {
                        0, 1, 2,
                        0, 2, 3
                  };

                  Vector2[] uv = new Vector2[4]
                  {
                        new Vector2(0, 0),
                        new Vector2(1, 0),
                        new Vector2(1, 1),
                        new Vector2(0, 1)
                  };

                  Vector3[] normals = new Vector3[4]
                  {
                        Vector3.up,
                        Vector3.up,
                        Vector3.up,
                        Vector3.up
                  };

                  simpleMesh.vertices = vertices;
                  simpleMesh.triangles = triangles;
                  simpleMesh.uv = uv;
                  simpleMesh.normals = normals;

                  meshFilter.mesh = simpleMesh;
            }

            /// <summary>
            /// Находит плоскость по клику на экране
            /// </summary>
            public ARPlane GetPlaneByScreenPosition(Vector2 screenPosition, ARRaycastManager raycastManager, Camera arCamera)
            {
                  if (raycastManager == null)
                  {
                        Debug.LogError("ARRaycastManager не найден");
                        return null;
                  }

                  if (arCamera == null)
                  {
                        Debug.LogError("AR Camera не найдена");
                        return null;
                  }

                  // Для хранения результатов raycast
                  List<ARRaycastHit> raycastHits = new List<ARRaycastHit>();

                  // Используем все возможные типы трекабельных объектов для максимального охвата
                  TrackableType trackableTypes =
                        TrackableType.PlaneWithinPolygon |
                        TrackableType.PlaneEstimated |
                        TrackableType.PlaneWithinBounds |
                        TrackableType.AllTypes;

                  // Выполняем raycast
                  if (raycastManager.Raycast(screenPosition, raycastHits, trackableTypes))
                  {
                        // Сортируем попадания по расстоянию (ближайшие первыми)
                        raycastHits.Sort((hit1, hit2) => hit1.distance.CompareTo(hit2.distance));

                        // Сначала пробуем найти стену среди попаданий
                        foreach (var hit in raycastHits)
                        {
                              // Выводим информацию о попадании
                              Debug.Log($"Попадание в позиции {hit.pose.position}, trackableId: {hit.trackableId}, расстояние: {hit.distance}");

                              // Проверяем, есть ли плоскость с таким id
                              if (planeManager != null)
                              {
                                    ARPlane plane = planeManager.GetPlane(hit.trackableId);
                                    if (plane != null)
                                    {
                                          Debug.Log($"Найдена плоскость: {plane.trackableId}, смещение: {plane.center}, размер: {plane.size}");

                                          // Если это стена, сразу возвращаем ее
                                          if (IsVerticalPlane(plane))
                                          {
                                                Debug.Log($"Это вертикальная плоскость (стена) - возвращаем ее");
                                                return plane;
                                          }
                                    }
                              }
                        }

                        // Если стены не найдены, возвращаем ближайшую плоскость
                        ARRaycastHit closestHit = raycastHits[0];
                        if (planeManager != null)
                        {
                              ARPlane plane = planeManager.GetPlane(closestHit.trackableId);
                              if (plane != null)
                              {
                                    // Проверяем, есть ли у плоскости кастомная визуализация
                                    string visualName = $"CustomVisual_{plane.trackableId}";
                                    Transform visualTrans = plane.transform.Find(visualName);
                                    if (visualTrans != null)
                                    {
                                          Debug.Log($"Найдена кастомная визуализация для плоскости {plane.trackableId}");
                                    }

                                    return plane;
                              }
                              else
                              {
                                    Debug.LogWarning($"Плоскость с ID {closestHit.trackableId} не найдена в ARPlaneManager");
                              }
                        }
                  }
                  else
                  {
                        Debug.Log("Луч не попал ни в одну из распознанных плоскостей");
                  }

                  return null;
            }

            /// <summary>
            /// Проверяет, можно ли взаимодействовать с точкой на экране (не на UI)
            /// </summary>
            public bool CanInteractWithPoint(Vector2 screenPosition)
            {
                  // Проверяем, что точка не находится над UI элементами
                  if (UnityEngine.EventSystems.EventSystem.current != null)
                  {
                        if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
                        {
                              return false;
                        }

                        // Проверка для мобильных устройств
                        if (Input.touchCount > 0)
                        {
                              Touch touch = Input.GetTouch(0);
                              if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject(touch.fingerId))
                              {
                                    return false;
                              }
                        }
                  }

                  return true;
            }

            private void Update()
            {
                  // Удалим обновление визуализаций каждый кадр, т.к. это вызывает проблемы производительности
                  // UpdateAllVisualizations();
            }

            private void Start()
            {
                  // Вызываем принудительное отображение плоскостей сразу
                  ForceShowAllPlanes();

                  // Запускаем диагностику видимости плоскостей
                  Invoke("DebugAllPlanesVisibility", 2.0f);

                  // Также запланируем повторные вызовы с интервалами, чтобы обработать плоскости,
                  // которые могут быть обнаружены позже, но с большим интервалом
                  InvokeRepeating("ForceShowAllPlanes", 1.0f, 3.0f);

                  // Запускаем корутину для постоянной проверки видимости плоскостей
                  StartCoroutine(EnsurePlanesVisibilityCoroutine());
            }

            /// <summary>
            /// Корутина для постоянной проверки и активации плоскостей
            /// </summary>
            private System.Collections.IEnumerator EnsurePlanesVisibilityCoroutine()
            {
                  // Ждем немного для инициализации AR
                  yield return new WaitForSeconds(2f);

                  int batchSize = 3; // Обрабатываем только 3 плоскости за одну итерацию
                  int batchCounter = 0;
                  List<ARPlane> planesList = new List<ARPlane>();

                  while (true)
                  {
                        // Проверяем все плоскости и активируем их
                        if (planeManager != null && planeManager.enabled)
                        {
                              // Обновляем список плоскостей только раз в несколько циклов
                              batchCounter++;
                              if (batchCounter % 5 == 0 || planesList.Count == 0)
                              {
                                    planesList.Clear();
                                    foreach (ARPlane plane in planeManager.trackables)
                                    {
                                          if (plane != null)
                                          {
                                                planesList.Add(plane);
                                          }
                                    }
                                    batchCounter = 0;
                              }

                              int activatedCount = 0;
                              int processedCount = 0;

                              // Обрабатываем только ограниченное количество плоскостей за итерацию
                              for (int i = 0; i < planesList.Count && processedCount < batchSize; i++)
                              {
                                    ARPlane plane = planesList[i];
                                    if (plane == null) continue;

                                    processedCount++;

                                    // Активируем объект, если он неактивен
                                    if (!plane.gameObject.activeInHierarchy)
                                    {
                                          bool activated = false;
                                          try
                                          {
                                                // Используем метод гарантированной активации
                                                EnsurePlaneIsActive(plane);
                                                activated = true;
                                          }
                                          catch (System.Exception e)
                                          {
                                                Debug.LogError($"Ошибка при активации плоскости: {e.Message}");
                                          }

                                          if (activated)
                                          {
                                                activatedCount++;
                                                yield return null; // Делаем паузу после каждой активации
                                          }
                                    }

                                    // Проверяем рендерер (только если плоскость активна)
                                    if (plane.gameObject.activeInHierarchy)
                                    {
                                          MeshRenderer renderer = plane.GetComponent<MeshRenderer>();
                                          if (renderer != null && !renderer.enabled)
                                          {
                                                renderer.enabled = true;
                                                activatedCount++;
                                          }

                                          // Проверяем наличие кастомной визуализации
                                          string visualName = $"CustomVisual_{plane.trackableId}";
                                          Transform visualTrans = plane.transform.Find(visualName);
                                          if (visualTrans == null)
                                          {
                                                // Вместо немедленного создания запланируем его на следующий кадр
                                                StartCoroutine(CreateVisualizationDelayed(plane));
                                                activatedCount++;
                                          }
                                          else if (!visualTrans.gameObject.activeSelf)
                                          {
                                                visualTrans.gameObject.SetActive(true);
                                                activatedCount++;
                                          }
                                    }

                                    // Делаем паузу после обработки каждой плоскости, если была активация
                                    if (activatedCount > 0)
                                    {
                                          yield return null;
                                    }
                              }

                              if (activatedCount > 0)
                              {
                                    Debug.Log($"Активировано {activatedCount} компонентов плоскостей");
                              }
                        }

                        // Пауза перед следующей проверкой
                        yield return new WaitForSeconds(1.0f);
                  }
            }

            /// <summary>
            /// Корутина для отложенного создания визуализации плоскости
            /// </summary>
            private IEnumerator CreateVisualizationDelayed(ARPlane plane)
            {
                  yield return null; // Ждем следующий кадр
                  if (plane != null && plane.gameObject.activeInHierarchy)
                  {
                        CreateCustomPlaneVisualization(plane);
                  }
            }

            /// <summary>
            /// Корутина для проверки видимости плоскостей после задержки
            /// </summary>
            private System.Collections.IEnumerator CheckPlanesVisibilityAfterDelay()
            {
                  // Ждем задержку перед проверкой видимости
                  yield return new WaitForSeconds(1f);

                  // Проверяем видимость всех плоскостей
                  UpdateAllPlanesVisibility();
            }

            /// <summary>
            /// Диагностический метод для проверки видимости плоскостей
            /// </summary>
            public void DebugAllPlanesVisibility()
            {
                  if (planeManager == null)
                  {
                        Debug.LogError("ДИАГНОСТИКА: AR Plane Manager не найден!");
                        return;
                  }

                  int totalPlanes = planeManager.trackables.count;
                  int activePlanes = 0;
                  int inactivePlanes = 0;
                  int planesWithVisualizations = 0;
                  int planesWithActiveVisualizations = 0;
                  int verticalPlanes = 0;
                  int horizontalPlanes = 0;

                  Debug.Log($"==== ДИАГНОСТИКА ВИДИМОСТИ ПЛОСКОСТЕЙ ====");
                  Debug.Log($"Всего плоскостей: {totalPlanes}");

                  foreach (ARPlane plane in planeManager.trackables)
                  {
                        if (plane == null)
                        {
                              Debug.LogWarning("Найдена null-плоскость в trackables!");
                              continue;
                        }

                        // Проверяем активность плоскости
                        if (plane.gameObject.activeSelf)
                        {
                              activePlanes++;
                        }
                        else
                        {
                              inactivePlanes++;
                              Debug.LogWarning($"Неактивная плоскость: {plane.trackableId} на позиции {plane.transform.position}");
                              // Активируем неактивные плоскости
                              plane.gameObject.SetActive(true);
                        }

                        // Проверяем тип плоскости
                        bool isVertical = IsVerticalPlane(plane);
                        if (isVertical)
                        {
                              verticalPlanes++;
                              Debug.Log($"Вертикальная плоскость (СТЕНА): {plane.trackableId} на позиции {plane.transform.position}");
                        }
                        else
                        {
                              horizontalPlanes++;
                        }

                        // Проверяем наличие визуализации
                        string visualName = $"CustomVisual_{plane.trackableId}";
                        Transform visualTrans = plane.transform.Find(visualName);
                        if (visualTrans != null)
                        {
                              planesWithVisualizations++;

                              if (visualTrans.gameObject.activeSelf)
                              {
                                    planesWithActiveVisualizations++;
                              }
                              else
                              {
                                    Debug.LogWarning($"Неактивная визуализация для плоскости: {plane.trackableId}");
                                    // Активируем неактивные визуализации
                                    visualTrans.gameObject.SetActive(true);
                              }

                              // Проверяем рендерер
                              MeshRenderer renderer = visualTrans.GetComponent<MeshRenderer>();
                              if (renderer != null)
                              {
                                    if (!renderer.enabled)
                                    {
                                          Debug.LogWarning($"Отключен рендерер визуализации для плоскости: {plane.trackableId}");
                                          renderer.enabled = true;
                                    }

                                    // Устанавливаем очень яркий цвет для гарантированной видимости
                                    if (isVertical)
                                    {
                                          // Ярко-красный для стен
                                          renderer.material.color = new Color(1.0f, 0.0f, 0.0f, 1.0f);
                                    }
                                    else
                                    {
                                          // Ярко-зеленый для пола
                                          renderer.material.color = new Color(0.0f, 1.0f, 0.0f, 1.0f);
                                    }

                                    // Максимально усиливаем яркость через эмиссию
                                    if (renderer.material.HasProperty("_EmissionColor"))
                                    {
                                          renderer.material.EnableKeyword("_EMISSION");
                                          renderer.material.SetColor("_EmissionColor", renderer.material.color * 3.0f);
                                    }
                              }
                              else
                              {
                                    Debug.LogError($"Отсутствует MeshRenderer на визуализации для плоскости: {plane.trackableId}");
                              }
                        }
                        else
                        {
                              Debug.LogWarning($"Отсутствует визуализация для плоскости: {plane.trackableId}");
                              // Создаем визуализацию
                              CreateCustomPlaneVisualization(plane);
                        }
                  }

                  Debug.Log($"Активных плоскостей: {activePlanes} из {totalPlanes}");
                  Debug.Log($"Неактивных плоскостей: {inactivePlanes} из {totalPlanes}");
                  Debug.Log($"Плоскостей с визуализациями: {planesWithVisualizations} из {totalPlanes}");
                  Debug.Log($"Плоскостей с активными визуализациями: {planesWithActiveVisualizations} из {totalPlanes}");
                  Debug.Log($"Вертикальных плоскостей (СТЕН): {verticalPlanes} из {totalPlanes}");
                  Debug.Log($"Горизонтальных плоскостей (ПОЛ): {horizontalPlanes} из {totalPlanes}");
                  Debug.Log($"==== КОНЕЦ ДИАГНОСТИКИ ====");
            }

            /// <summary>
            /// Обновляет существующую визуализацию плоскости
            /// </summary>
            private void UpdateCustomPlaneVisualization(ARPlane plane)
            {
                  if (plane == null) return;

                  string visualName = $"CustomVisual_{plane.trackableId}";
                  Transform visualTransform = plane.transform.Find(visualName);

                  if (visualTransform != null)
                  {
                        GameObject visualObj = visualTransform.gameObject;
                        if (!visualObj.activeSelf)
                        {
                              visualObj.SetActive(true);
                        }

                        // Обновляем меш, если он изменился
                        MeshFilter planeMeshFilter = plane.GetComponent<MeshFilter>();
                        MeshFilter visualMeshFilter = visualObj.GetComponent<MeshFilter>();

                        if (planeMeshFilter != null && planeMeshFilter.mesh != null &&
                            visualMeshFilter != null && planeMeshFilter.mesh != visualMeshFilter.mesh)
                        {
                              visualMeshFilter.mesh = planeMeshFilter.mesh;
                              Debug.Log($"Обновлен меш для плоскости {plane.trackableId}");
                        }

                        // Обновляем материал на основе новой классификации
                        MeshRenderer renderer = visualObj.GetComponent<MeshRenderer>();
                        if (renderer != null)
                        {
                              bool isWall = IsVerticalPlane(plane);

                              // Применяем соответствующий материал
                              if (isWall)
                              {
                                    // Для стен используем яркий красный материал
                                    renderer.material.color = new Color(1.0f, 0.0f, 0.0f, 1.0f);

                                    if (renderer.material.HasProperty("_EmissionColor"))
                                    {
                                          renderer.material.EnableKeyword("_EMISSION");
                                          renderer.material.SetColor("_EmissionColor", new Color(1.0f, 0.0f, 0.0f, 1.0f));
                                    }
                              }
                              else
                              {
                                    // Для пола используем яркий зеленый материал
                                    renderer.material.color = new Color(0.0f, 1.0f, 0.0f, 1.0f);

                                    if (renderer.material.HasProperty("_EmissionColor"))
                                    {
                                          renderer.material.EnableKeyword("_EMISSION");
                                          renderer.material.SetColor("_EmissionColor", new Color(0.0f, 1.0f, 0.0f, 1.0f));
                                    }
                              }
                        }
                  }
                  else
                  {
                        // Если визуализация не существует, создаем новую
                        CreateCustomPlaneVisualization(plane);
                  }
            }

            /// <summary>
            /// Обновляет все пользовательские визуализации плоскостей
            /// </summary>
            private void UpdateAllVisualizations()
            {
                  if (planeManager == null) return;

                  foreach (ARPlane plane in planeManager.trackables)
                  {
                        if (plane != null && plane.gameObject != null && plane.gameObject.activeSelf)
                        {
                              UpdateCustomPlaneVisualization(plane);
                        }
                  }
            }

            /// <summary>
            /// Удаляет пользовательскую визуализацию плоскости
            /// </summary>
            private void RemoveCustomVisualization(ARPlane plane)
            {
                  if (plane == null) return;

                  // Находим и удаляем объект визуализации
                  string visualName = $"CustomVisual_{plane.trackableId}";
                  Transform visualTrans = plane.transform.Find(visualName);
                  if (visualTrans != null)
                  {
                        GameObject.Destroy(visualTrans.gameObject);
                        Debug.Log($"Удалена визуализация для плоскости: {plane.trackableId}");
                  }
            }

            /// <summary>
            /// Гарантирует, что плоскость активна в иерархии
            /// </summary>
            private void EnsurePlaneIsActive(ARPlane plane)
            {
                  if (plane == null) return;

                  // Делегируем работу вспомогательному классу
                  ARPlaneActivator.EnsurePlaneIsActive(plane, this);
            }
      }
}