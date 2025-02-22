using UnityEngine;
using System;
using Remalux.AR;

namespace Remalux.AR
{
    public class ImageProcessor : IDisposable
    {
        private readonly int width;
        private readonly int height;

        public ImageProcessor(int width, int height)
        {
            this.width = width;
            this.height = height;
        }

        public Line[] ProcessFrameSimple(Texture2D frame, byte[] wallMask)
        {
            // Временная реализация для демонстрации
            return new Line[0];
        }

        public void Dispose()
        {
            // Очистка ресурсов при необходимости
        }
    }
} 