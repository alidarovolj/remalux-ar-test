using UnityEngine;
using Unity.Barracuda;
using System.Threading.Tasks;
using OpenCvSharp;
using System;

namespace Remalux.AR
{
    public class WallSegmentation : IDisposable
    {
        private NNModel modelAsset;
        private IWorker worker;
        private readonly string outputName = "output";
        private readonly int inputSize = 256;

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

        public async Task<Mat> ProcessFrameAsync(Texture2D frame)
        {
            if (worker == null)
            {
                Debug.LogError("WallSegmentation: Worker не инициализирован");
                return new Mat();
            }

            return await Task.Run(() =>
            {
                try
                {
                    // Предобработка изображения
                    Texture2D resized = ResizeImage(frame);
                    using (var tensor = ImageToTensor(resized))
                    {
                        // Запускаем инференс
                        worker.Execute(tensor);
                        using (var output = worker.PeekOutput(outputName))
                        {
                            // Постобработка результата
                            var result = TensorToMask(output);
                            GameObject.Destroy(resized);
                            return result;
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"WallSegmentation: Ошибка при обработке кадра: {e.Message}");
                    return new Mat();
                }
            });
        }

        private Texture2D ResizeImage(Texture2D source)
        {
            var rt = RenderTexture.GetTemporary(inputSize, inputSize, 0);
            Graphics.Blit(source, rt);
            
            var result = new Texture2D(inputSize, inputSize);
            RenderTexture.active = rt;
            result.ReadPixels(new UnityEngine.Rect(0, 0, inputSize, inputSize), 0, 0);
            result.Apply();
            
            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(rt);
            
            return result;
        }

        private Tensor ImageToTensor(Texture2D image)
        {
            var tensor = new Tensor(1, inputSize, inputSize, 3);
            var pixels = image.GetPixels32();

            for (int y = 0; y < inputSize; y++)
            {
                for (int x = 0; x < inputSize; x++)
                {
                    var pixel = pixels[y * inputSize + x];
                    tensor[0, y, x, 0] = pixel.r / 255f;
                    tensor[0, y, x, 1] = pixel.g / 255f;
                    tensor[0, y, x, 2] = pixel.b / 255f;
                }
            }

            return tensor;
        }

        private Mat TensorToMask(Tensor output)
        {
            var mask = new Mat(inputSize, inputSize, MatType.CV_8UC1);
            
            for (int y = 0; y < inputSize; y++)
            {
                for (int x = 0; x < inputSize; x++)
                {
                    // Предполагаем, что выход модели - вероятность принадлежности к классу стены
                    float probability = output[0, y, x, 0];
                    byte value = (byte)(probability * 255);
                    mask.Set(y, x, value);
                }
            }

            return mask;
        }

        public void Dispose()
        {
            worker?.Dispose();
        }
    }
} 