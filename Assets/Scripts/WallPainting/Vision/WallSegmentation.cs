using UnityEngine;
using Unity.Barracuda;
using System.Threading.Tasks;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityUtils;
using System;
using System.Collections.Generic;

namespace Remalux.WallPainting.Vision
{
      public class SegmentationResult : IDisposable
      {
            private Mat mat;
            public SegmentationResult(Mat mat) { this.mat = mat; }
            public byte[] ToBytes()
            {
                  if (mat == null || mat.empty())
                        return new byte[0];

                  // Получаем размеры матрицы
                  int width = mat.width();
                  int height = mat.height();
                  int channels = mat.channels();
                  int totalBytes = width * height * channels;

                  // Создаем массив байтов
                  byte[] result = new byte[totalBytes];

                  // Копируем данные из матрицы в массив
                  mat.get(0, 0, result);

                  return result;
            }
            public void Dispose() => mat?.Dispose();
      }

      public class WallSegmentation : IDisposable
      {
            private NNModel modelAsset;
            private IWorker worker;

            public WallSegmentation()
            {
                  InitializeModel();
            }

            private void InitializeModel()
            {
                  try
                  {
                        // Загружаем модель из ресурсов
                        modelAsset = Resources.Load<NNModel>("Models/WallSegmentation");
                        if (modelAsset == null)
                        {
                              Debug.LogError("WallSegmentation: Не удалось загрузить модель");
                              return;
                        }

                        var runtimeModel = ModelLoader.Load(modelAsset);
                        worker = WorkerFactory.CreateWorker(WorkerFactory.Type.ComputePrecompiled, runtimeModel);
                  }
                  catch (Exception e)
                  {
                        Debug.LogError($"WallSegmentation: Ошибка при инициализации модели: {e.Message}");
                  }
            }

            public void Dispose()
            {
                  worker?.Dispose();
                  modelAsset = null;
            }
      }
}