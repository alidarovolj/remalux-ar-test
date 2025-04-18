using UnityEngine;

/// <summary>
/// Скрипт для объекта покрашенной стены
/// </summary>
public class PaintedWall : MonoBehaviour
{
    [Header("Настройки отображения")]
    [Tooltip("Renderer для материала покраски")]
    public Renderer wallRenderer;
    
    [Tooltip("Альфа-канал покраски (0-1)")]
    [Range(0, 1)]
    public float paintAlpha = 0.75f;
    
    [Tooltip("Время жизни эффекта при создании (сек)")]
    public float initialFadeInTime = 0.5f;

    // Исходный цвет и текстура для хранения
    private Color originalColor;
    private Texture originalTexture;
    
    // Флаг инициализации
    private bool initialized = false;
    
    // Время для эффектов
    private float currentTime = 0f;

    void Start()
    {
        if (wallRenderer == null)
        {
            wallRenderer = GetComponent<Renderer>();
        }
        
        if (wallRenderer != null)
        {
            // Сохраняем исходный цвет и текстуру
            originalColor = wallRenderer.material.color;
            originalTexture = wallRenderer.material.mainTexture;
            
            // Настраиваем начальную прозрачность
            Color tmpColor = wallRenderer.material.color;
            tmpColor.a = 0;
            wallRenderer.material.color = tmpColor;
            
            initialized = true;
        }
    }

    void Update()
    {
        if (!initialized)
            return;
            
        // Эффект плавного появления при создании
        if (currentTime < initialFadeInTime)
        {
            currentTime += Time.deltaTime;
            float normalizedTime = currentTime / initialFadeInTime;
            
            // Настраиваем прозрачность
            Color tmpColor = wallRenderer.material.color;
            tmpColor.a = Mathf.Lerp(0, paintAlpha, normalizedTime);
            wallRenderer.material.color = tmpColor;
        }
    }

    /// <summary>
    /// Устанавливает текстуру маски для покраски
    /// </summary>
    public void SetTexture(Texture2D texture)
    {
        if (wallRenderer != null && texture != null)
        {
            wallRenderer.material.mainTexture = texture;
            originalTexture = texture;
        }
    }

    /// <summary>
    /// Устанавливает цвет покраски
    /// </summary>
    public void SetColor(Color color)
    {
        if (wallRenderer != null)
        {
            // Сохраняем альфа-канал
            float alpha = wallRenderer.material.color.a;
            
            // Устанавливаем новый цвет с сохранением прозрачности
            Color newColor = new Color(color.r, color.g, color.b, alpha);
            wallRenderer.material.color = newColor;
            originalColor = color;
        }
    }

    /// <summary>
    /// Устанавливает прозрачность покраски
    /// </summary>
    public void SetAlpha(float alpha)
    {
        if (wallRenderer != null)
        {
            Color tmpColor = wallRenderer.material.color;
            tmpColor.a = alpha;
            wallRenderer.material.color = tmpColor;
            paintAlpha = alpha;
        }
    }

    /// <summary>
    /// Эффект мигания для привлечения внимания
    /// </summary>
    public void Flash(float duration = 0.5f, float intensity = 1.2f)
    {
        if (wallRenderer != null)
        {
            // Сохраняем исходные параметры
            Color currentColor = wallRenderer.material.color;
            
            // Усиливаем насыщенность цвета
            Color flashColor = new Color(
                Mathf.Clamp01(currentColor.r * intensity),
                Mathf.Clamp01(currentColor.g * intensity),
                Mathf.Clamp01(currentColor.b * intensity),
                currentColor.a
            );
            
            // Применяем цвет для привлечения внимания
            wallRenderer.material.color = flashColor;
            
            // Возвращаем исходный цвет через указанное время
            Invoke("RestoreColor", duration);
        }
    }

    /// <summary>
    /// Восстанавливает исходный цвет
    /// </summary>
    private void RestoreColor()
    {
        if (wallRenderer != null)
        {
            Color tmpColor = originalColor;
            tmpColor.a = paintAlpha;
            wallRenderer.material.color = tmpColor;
        }
    }

    /// <summary>
    /// Масштабирует объект по размеру плоскости
    /// </summary>
    public void FitToPlane(Vector3 planeSize)
    {
        transform.localScale = new Vector3(planeSize.x, planeSize.y, transform.localScale.z);
    }
} 