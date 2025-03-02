using UnityEngine;
using Unity.Barracuda;
using System.Threading.Tasks;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityUtils;
using System;
using System.Collections.Generic;

namespace Remalux.AR
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
        private readonly string outputName = "output";
        private readonly int inputSize = 256;

        // Параметры постобработки
        private float confidenceThreshold = 0.4f;
        private int morphologySize = 4;
        private int blurSize = 7;
        private bool useAdaptiveThreshold = true;
        private int adaptiveBlockSize = 13;
        private double adaptiveC = 2.0;

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

        // Методы для установки параметров
        public void SetConfidenceThreshold(float threshold)
        {
            this.confidenceThreshold = Mathf.Clamp(threshold, 0.1f, 0.9f);
        }

        public void SetMorphologySize(int size)
        {
            this.morphologySize = Mathf.Clamp(size, 1, 10);
        }

        public void SetBlurSize(int size)
        {
            this.blurSize = Mathf.Clamp(size, 1, 15);
            // Должен быть нечетным
            if (this.blurSize % 2 == 0)
                this.blurSize++;
        }

        public void SetAdaptiveThreshold(bool useAdaptive, int blockSize, double c)
        {
            this.useAdaptiveThreshold = useAdaptive;

            // Блок должен быть нечетным
            this.adaptiveBlockSize = Mathf.Clamp(blockSize, 3, 21);
            if (this.adaptiveBlockSize % 2 == 0)
                this.adaptiveBlockSize++;

            this.adaptiveC = Mathf.Clamp((float)c, 0.5f, 5f);
        }

        public async Task<SegmentationResult> ProcessFrameAsync(Texture2D frame)
        {
            if (worker == null)
            {
                Debug.LogError("WallSegmentation: Worker не инициализирован");
                return new SegmentationResult(new Mat());
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
                            var rawMask = TensorToMask(output);

                            // Применяем дополнительную постобработку для улучшения качества маски
                            var enhancedMask = EnhanceMask(rawMask);

                            // Освобождаем ресурсы
                            GameObject.Destroy(resized);
                            rawMask.Dispose();

                            return new SegmentationResult(enhancedMask);
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"WallSegmentation: Ошибка при обработке кадра: {e.Message}");
                    return new SegmentationResult(new Mat());
                }
            });
        }

        private Mat EnhanceMask(Mat rawMask)
        {
            try
            {
                // Создаем копию для обработки
                Mat enhancedMask = new Mat();
                rawMask.copyTo(enhancedMask);

                // Применяем пороговую обработку для получения бинарной маски
                if (useAdaptiveThreshold)
                {
                    // Адаптивная пороговая обработка лучше работает при различном освещении
                    Imgproc.adaptiveThreshold(
                        enhancedMask,
                        enhancedMask,
                        255,
                        Imgproc.ADAPTIVE_THRESH_GAUSSIAN_C,
                        Imgproc.THRESH_BINARY,
                        adaptiveBlockSize,
                        adaptiveC
                    );
                }
                else
                {
                    // Обычная пороговая обработка
                    Imgproc.threshold(
                        enhancedMask,
                        enhancedMask,
                        confidenceThreshold * 255,
                        255,
                        Imgproc.THRESH_BINARY
                    );
                }

                // Применяем морфологические операции для улучшения маски
                Mat morphElement = Imgproc.getStructuringElement(
                    Imgproc.MORPH_RECT,
                    new Size(morphologySize, morphologySize)
                );

                // Закрытие (dilate + erode) для заполнения небольших пробелов
                Imgproc.morphologyEx(
                    enhancedMask,
                    enhancedMask,
                    Imgproc.MORPH_CLOSE,
                    morphElement
                );

                // Открытие (erode + dilate) для удаления шума
                Imgproc.morphologyEx(
                    enhancedMask,
                    enhancedMask,
                    Imgproc.MORPH_OPEN,
                    morphElement
                );

                // Применяем размытие для сглаживания границ
                Imgproc.GaussianBlur(
                    enhancedMask,
                    enhancedMask,
                    new Size(blurSize, blurSize),
                    0
                );

                // Снова применяем пороговую обработку для получения четкой бинарной маски
                Imgproc.threshold(
                    enhancedMask,
                    enhancedMask,
                    127,
                    255,
                    Imgproc.THRESH_BINARY
                );

                // Находим контуры для выделения связных областей
                List<MatOfPoint> contours = new List<MatOfPoint>();
                Mat hierarchy = new Mat();
                Imgproc.findContours(
                    enhancedMask,
                    contours,
                    hierarchy,
                    Imgproc.RETR_EXTERNAL,
                    Imgproc.CHAIN_APPROX_SIMPLE
                );

                // Создаем пустую маску
                Mat contourMask = Mat.zeros(enhancedMask.size(), CvType.CV_8UC1);

                // Фильтруем контуры по размеру и форме
                foreach (var contour in contours)
                {
                    // Вычисляем площадь контура
                    double area = Imgproc.contourArea(contour);

                    // Фильтруем маленькие контуры
                    if (area < 100)
                        continue;

                    // Аппроксимируем контур для получения более гладких границ
                    MatOfPoint2f contour2f = new MatOfPoint2f();
                    contour.convertTo(contour2f, CvType.CV_32F);

                    double epsilon = 0.02 * Imgproc.arcLength(contour2f, true);
                    MatOfPoint2f approxCurve = new MatOfPoint2f();
                    Imgproc.approxPolyDP(contour2f, approxCurve, epsilon, true);

                    MatOfPoint approxContour = new MatOfPoint();
                    approxCurve.convertTo(approxContour, CvType.CV_32S);

                    // Рисуем контур на маске
                    Imgproc.drawContours(
                        contourMask,
                        new List<MatOfPoint> { approxContour },
                        0,
                        new Scalar(255),
                        -1 // Заполняем контур
                    );

                    // Освобождаем ресурсы
                    contour2f.Dispose();
                    approxCurve.Dispose();
                    approxContour.Dispose();
                }

                // Освобождаем ресурсы
                enhancedMask.Dispose();
                hierarchy.Dispose();
                foreach (var contour in contours)
                {
                    contour.Dispose();
                }

                return contourMask;
            }
            catch (Exception e)
            {
                Debug.LogError($"WallSegmentation: Ошибка при улучшении маски: {e.Message}");
                Mat result = new Mat();
                rawMask.copyTo(result);
                return result;
            }
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
            Mat mask = new Mat(inputSize, inputSize, CvType.CV_8UC1);

            for (int y = 0; y < inputSize; y++)
            {
                for (int x = 0; x < inputSize; x++)
                {
                    float probability = output[0, y, x, 0];
                    byte value = (byte)(probability * 255);
                    mask.put(y, x, value);
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
