using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

#if UNITY_IOS
using UnityEngine.XR.ARKit;
#endif

/// <summary>
/// Контроллер для сохранения и загрузки AR World Map на iOS устройствах
/// </summary>
public class ARWorldMapController : MonoBehaviour
{
    [Header("AR Компоненты")]
    [Tooltip("AR Session компонент")]
    public ARSession arSession;
    
    [Tooltip("AR Session Origin компонент")]
    public ARSessionOrigin arSessionOrigin;

    [Header("Настройки сохранения")]
    [Tooltip("Автоматически загружать карту при старте")]
    public bool autoLoadOnStart = true;
    
    [Tooltip("Автоматически сохранять карту при выходе")]
    public bool autoSaveOnQuit = true;
    
    [Tooltip("Интервал автосохранения (в секундах, 0 - отключено)")]
    public float autoSaveInterval = 30f;

    // Путь для сохранения ARWorldMap
    private string worldMapSavePath;
    
    // Флаг готовности AR сессии
    private bool isSessionReady = false;
    
    // Время последнего автосохранения
    private float lastAutoSaveTime = 0f;

    void Start()
    {
        if (arSession == null)
        {
            arSession = FindObjectOfType<ARSession>();
        }
        
        if (arSessionOrigin == null)
        {
            arSessionOrigin = FindObjectOfType<ARSessionOrigin>();
        }

        // Путь для сохранения ARWorldMap
        worldMapSavePath = Path.Combine(Application.persistentDataPath, "arworldmap.data");
        
        // Подписываемся на событие инициализации AR сессии
        ARSession.stateChanged += OnARSessionStateChanged;
        
        // Если включена автозагрузка, пытаемся загрузить карту мира
        if (autoLoadOnStart)
        {
            StartCoroutine(WaitForSessionAndLoad());
        }
    }

    void Update()
    {
        // Автосохранение по интервалу
        if (autoSaveInterval > 0 && isSessionReady)
        {
            if (Time.time - lastAutoSaveTime > autoSaveInterval)
            {
                SaveWorldMap();
                lastAutoSaveTime = Time.time;
            }
        }
    }

    void OnApplicationPause(bool pauseStatus)
    {
        // Сохраняем карту мира при переходе в фоновый режим
        if (pauseStatus && autoSaveOnQuit && isSessionReady)
        {
            SaveWorldMap();
        }
    }

    void OnApplicationQuit()
    {
        // Сохраняем карту мира при выходе из приложения
        if (autoSaveOnQuit && isSessionReady)
        {
            SaveWorldMap();
        }
    }

    void OnDestroy()
    {
        // Отписываемся от события
        ARSession.stateChanged -= OnARSessionStateChanged;
    }

    /// <summary>
    /// Обработчик изменения состояния AR сессии
    /// </summary>
    private void OnARSessionStateChanged(ARSessionStateChangedEventArgs args)
    {
        switch (args.state)
        {
            case ARSessionState.Ready:
            case ARSessionState.SessionTracking:
                isSessionReady = true;
                break;
            default:
                isSessionReady = false;
                break;
        }
    }

    /// <summary>
    /// Ожидаем готовности AR сессии перед загрузкой карты
    /// </summary>
    private IEnumerator WaitForSessionAndLoad()
    {
        // Ждем некоторое время для инициализации
        yield return new WaitForSeconds(2f);
        
        // Ждем готовности AR сессии
        while (!isSessionReady)
        {
            yield return new WaitForSeconds(0.5f);
        }
        
        // Загружаем карту мира
        LoadWorldMap();
    }

    /// <summary>
    /// Сохраняет текущую AR World Map
    /// </summary>
    public void SaveWorldMap()
    {
        #if UNITY_IOS
        if (!isSessionReady)
        {
            Debug.LogWarning("AR сессия не готова для сохранения карты мира");
            return;
        }

        StartCoroutine(SaveWorldMapRoutine());
        #else
        Debug.LogWarning("Сохранение AR World Map поддерживается только на iOS устройствах");
        #endif
    }

    /// <summary>
    /// Загружает сохраненную AR World Map
    /// </summary>
    public void LoadWorldMap()
    {
        #if UNITY_IOS
        if (!isSessionReady)
        {
            Debug.LogWarning("AR сессия не готова для загрузки карты мира");
            return;
        }

        StartCoroutine(LoadWorldMapRoutine());
        #else
        Debug.LogWarning("Загрузка AR World Map поддерживается только на iOS устройствах");
        #endif
    }

    #if UNITY_IOS
    /// <summary>
    /// Корутина для сохранения AR World Map
    /// </summary>
    private IEnumerator SaveWorldMapRoutine()
    {
        var sessionSubsystem = (ARKitSessionSubsystem)arSession.subsystem;
        if (sessionSubsystem == null)
        {
            Debug.LogError("ARKit сессия недоступна");
            yield break;
        }

        // Запрашиваем текущую карту мира
        var getWorldMapRequest = sessionSubsystem.GetARWorldMapAsync();
        
        // Ждем завершения запроса
        while (!getWorldMapRequest.status.IsDone())
        {
            yield return null;
        }

        // Проверяем успешность запроса
        if (getWorldMapRequest.status.IsError())
        {
            Debug.LogError($"Ошибка получения AR World Map: {getWorldMapRequest.status}");
            yield break;
        }

        // Получаем карту мира и сериализуем её в бинарные данные
        var worldMap = getWorldMapRequest.GetWorldMap();
        var worldMapData = worldMap.Serialize(Unity.Collections.Allocator.Temp);

        try
        {
            // Сохраняем данные в файл
            File.WriteAllBytes(worldMapSavePath, worldMapData.ToArray());
            
            Debug.Log($"AR World Map успешно сохранена: {worldMapSavePath}");
            worldMapData.Dispose();
        }
        catch (Exception e)
        {
            Debug.LogError($"Ошибка сохранения AR World Map: {e.Message}");
            worldMapData.Dispose();
        }
    }

    /// <summary>
    /// Корутина для загрузки AR World Map
    /// </summary>
    private IEnumerator LoadWorldMapRoutine()
    {
        // Проверяем наличие файла с сохраненной картой
        if (!File.Exists(worldMapSavePath))
        {
            Debug.LogWarning($"Файл AR World Map не найден: {worldMapSavePath}");
            yield break;
        }

        var sessionSubsystem = (ARKitSessionSubsystem)arSession.subsystem;
        if (sessionSubsystem == null)
        {
            Debug.LogError("ARKit сессия недоступна");
            yield break;
        }

        try
        {
            // Читаем данные из файла
            byte[] worldMapData = File.ReadAllBytes(worldMapSavePath);
            
            // Создаем NativeArray из бинарных данных
            var worldMapNativeData = new Unity.Collections.NativeArray<byte>(
                worldMapData, 
                Unity.Collections.Allocator.Temp
            );
            
            // Десериализуем в ARWorldMap
            var worldMap = ARWorldMap.Deserialize(worldMapNativeData);
            worldMapNativeData.Dispose();
            
            if (!worldMap.valid)
            {
                Debug.LogError("Загруженная AR World Map недействительна");
                yield break;
            }

            // Сбрасываем текущую сессию
            arSession.Reset();
            
            // Ждем некоторое время для перезапуска сессии
            yield return new WaitForSeconds(0.5f);
            
            // Применяем загруженную карту
            sessionSubsystem.ApplyWorldMap(worldMap);
            
            Debug.Log("AR World Map успешно загружена и применена");
        }
        catch (Exception e)
        {
            Debug.LogError($"Ошибка загрузки AR World Map: {e.Message}");
        }
    }
    #endif

    /// <summary>
    /// Удаляет сохраненную карту мира
    /// </summary>
    public void ClearSavedWorldMap()
    {
        if (File.Exists(worldMapSavePath))
        {
            File.Delete(worldMapSavePath);
            Debug.Log("Сохраненная AR World Map удалена");
        }
    }
} 