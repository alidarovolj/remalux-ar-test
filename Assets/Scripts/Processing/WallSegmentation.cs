using UnityEngine;
using System.Threading.Tasks;

public class WallSegmentation
{
    public async Task<float[]> ProcessFrameAsync(Texture2D frame)
    {
        // Временная реализация
        await Task.Yield();
        return new float[frame.width * frame.height];
    }

    public void Dispose()
    {
        // Очистка ресурсов при необходимости
    }
} 