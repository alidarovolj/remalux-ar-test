using UnityEngine;
using System;
using System.Collections.Generic;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityUtils;
using Remalux.AR;

namespace Remalux.AR
{
    public class ImageProcessor : IDisposable
    {
        private readonly int width;
        private readonly int height;

        // Параметры обработки изображения
        private int blurSize = 7;
        private int cannyThreshold1 = 30;
        private int cannyThreshold2 = 120;
        private double houghThreshold = 40;
        private double houghMinLineLength = 40;
        private double houghMaxLineGap = 15;
        private int dilationSize = 4;
        private int erosionSize = 2;

        // Параметры фильтрации линий
        private float minLineLength = 25f;
        private float maxLineGap = 25f;
        private float angleThreshold = 8f;
        private float minConfidence = 0.6f;

        public ImageProcessor(int width, int height)
        {
            this.width = width;
            this.height = height;
        }

        // Методы для установки параметров
        public void SetBlurSize(int size)
        {
            this.blurSize = Mathf.Clamp(size, 1, 15);
            // Должен быть нечетным
            if (this.blurSize % 2 == 0)
                this.blurSize++;
        }

        public void SetCannyThresholds(int threshold1, int threshold2)
        {
            this.cannyThreshold1 = Mathf.Clamp(threshold1, 10, 200);
            this.cannyThreshold2 = Mathf.Clamp(threshold2, 50, 300);
        }

        public void SetHoughParameters(double threshold, double minLineLength, double maxLineGap)
        {
            this.houghThreshold = Mathf.Clamp((float)threshold, 10f, 200f);
            this.houghMinLineLength = Mathf.Clamp((float)minLineLength, 10f, 200f);
            this.houghMaxLineGap = Mathf.Clamp((float)maxLineGap, 1f, 50f);
        }

        public void SetMorphologyParameters(int dilationSize, int erosionSize)
        {
            this.dilationSize = Mathf.Clamp(dilationSize, 1, 10);
            this.erosionSize = Mathf.Clamp(erosionSize, 1, 10);
        }

        public void SetLineFilterParameters(float minLineLength, float maxLineGap, float angleThreshold, float minConfidence)
        {
            this.minLineLength = Mathf.Clamp(minLineLength, 10f, 100f);
            this.maxLineGap = Mathf.Clamp(maxLineGap, 5f, 50f);
            this.angleThreshold = Mathf.Clamp(angleThreshold, 1f, 30f);
            this.minConfidence = Mathf.Clamp(minConfidence, 0.1f, 0.9f);
        }

        public Line[] ProcessFrameSimple(Texture2D frame, byte[] wallMask)
        {
            try
            {
                // Конвертируем маску стен в Mat
                Mat mask = new Mat(height, width, CvType.CV_8UC1);
                mask.put(0, 0, wallMask);

                // Применяем морфологические операции для улучшения маски
                Mat processedMask = PreprocessMask(mask);

                // Находим контуры стен
                Mat edges = FindEdges(processedMask);

                // Находим линии с помощью преобразования Хафа
                MatOfPoint2f lines = FindLines(edges);

                // Конвертируем линии в наш формат и фильтруем
                Line[] resultLines = ConvertAndFilterLines(lines);

                // Освобождаем ресурсы OpenCV
                mask.Dispose();
                processedMask.Dispose();
                edges.Dispose();
                lines.Dispose();

                return resultLines;
            }
            catch (Exception e)
            {
                Debug.LogError($"ImageProcessor: Ошибка при обработке кадра: {e.Message}");
                return new Line[0];
            }
        }

        private Mat PreprocessMask(Mat mask)
        {
            // Применяем морфологические операции для улучшения маски
            Mat processedMask = new Mat();
            mask.copyTo(processedMask);

            // Создаем элементы для морфологических операций
            Mat dilationElement = Imgproc.getStructuringElement(
                Imgproc.MORPH_RECT,
                new Size(dilationSize, dilationSize)
            );

            Mat erosionElement = Imgproc.getStructuringElement(
                Imgproc.MORPH_RECT,
                new Size(erosionSize, erosionSize)
            );

            // Применяем дилатацию для заполнения небольших пробелов
            Imgproc.dilate(mask, processedMask, dilationElement);

            // Применяем эрозию для удаления шума
            Imgproc.erode(processedMask, processedMask, erosionElement);

            // Применяем размытие для сглаживания
            Imgproc.GaussianBlur(processedMask, processedMask, new Size(blurSize, blurSize), 0);

            // Применяем пороговую обработку для получения бинарного изображения
            Imgproc.threshold(processedMask, processedMask, 127, 255, Imgproc.THRESH_BINARY);

            // Освобождаем ресурсы
            dilationElement.Dispose();
            erosionElement.Dispose();

            return processedMask;
        }

        private Mat FindEdges(Mat mask)
        {
            Mat edges = new Mat();

            // Применяем детектор границ Canny
            Imgproc.Canny(mask, edges, cannyThreshold1, cannyThreshold2);

            return edges;
        }

        private MatOfPoint2f FindLines(Mat edges)
        {
            // Находим линии с помощью вероятностного преобразования Хафа
            MatOfPoint2f lines = new MatOfPoint2f();
            Imgproc.HoughLinesP(
                edges,
                lines,
                1,
                Mathf.Deg2Rad,
                (int)houghThreshold,
                houghMinLineLength,
                houghMaxLineGap
            );

            return lines;
        }

        private Line[] ConvertAndFilterLines(MatOfPoint2f houghLines)
        {
            if (houghLines.empty())
                return new Line[0];

            var lines = new List<Line>();
            float[] linesArray = new float[houghLines.total() * houghLines.channels()];
            houghLines.get(0, 0, linesArray);

            for (int i = 0; i < linesArray.Length; i += 4)
            {
                // Конвертируем в наш формат
                Vector2 start = new Vector2(linesArray[i], linesArray[i + 1]);
                Vector2 end = new Vector2(linesArray[i + 2], linesArray[i + 3]);

                // Вычисляем длину линии
                float length = Vector2.Distance(start, end);

                // Фильтруем по длине
                if (length < minLineLength)
                    continue;

                // Вычисляем уверенность на основе длины
                // Чем длиннее линия, тем выше уверенность
                float confidence = Mathf.Clamp01(length / (width * 0.5f));

                // Фильтруем по уверенности
                if (confidence < minConfidence)
                    continue;

                lines.Add(new Line(start, end, confidence));
            }

            // Объединяем близкие линии
            return MergeSimilarLines(lines);
        }

        private Line[] MergeSimilarLines(List<Line> lines)
        {
            if (lines.Count <= 1)
                return lines.ToArray();

            var mergedLines = new List<Line>();
            var processedIndices = new HashSet<int>();

            for (int i = 0; i < lines.Count; i++)
            {
                if (processedIndices.Contains(i))
                    continue;

                var currentLine = lines[i];
                processedIndices.Add(i);

                for (int j = i + 1; j < lines.Count; j++)
                {
                    if (processedIndices.Contains(j))
                        continue;

                    var otherLine = lines[j];

                    // Проверяем, можно ли объединить линии
                    if (CanMergeLines(currentLine, otherLine))
                    {
                        currentLine = MergeLines(currentLine, otherLine);
                        processedIndices.Add(j);
                    }
                }

                mergedLines.Add(currentLine);
            }

            return mergedLines.ToArray();
        }

        private bool CanMergeLines(Line line1, Line line2)
        {
            // Проверяем, имеют ли линии схожее направление
            float angle = Vector2.Angle(line1.direction, line2.direction);
            if (angle > angleThreshold && angle < 180 - angleThreshold)
                return false;

            // Проверяем расстояние между линиями
            float distance = MinDistanceBetweenLines(line1, line2);
            return distance < maxLineGap;
        }

        private float MinDistanceBetweenLines(Line line1, Line line2)
        {
            // Находим минимальное расстояние между концами линий
            float d1 = Vector2.Distance(line1.start, line2.start);
            float d2 = Vector2.Distance(line1.start, line2.end);
            float d3 = Vector2.Distance(line1.end, line2.start);
            float d4 = Vector2.Distance(line1.end, line2.end);

            return Mathf.Min(d1, d2, d3, d4);
        }

        private Line MergeLines(Line line1, Line line2)
        {
            // Находим самые дальние точки для создания новой линии
            Vector2 start, end;

            // Находим самые дальние точки
            Vector2[] points = { line1.start, line1.end, line2.start, line2.end };
            FindFarthestPoints(points, out start, out end);

            // Усредняем уверенность
            float confidence = (line1.confidence + line2.confidence) * 0.5f;

            return new Line(start, end, confidence);
        }

        private void FindFarthestPoints(Vector2[] points, out Vector2 p1, out Vector2 p2)
        {
            float maxDistance = 0;
            p1 = points[0];
            p2 = points[1];

            for (int i = 0; i < points.Length; i++)
            {
                for (int j = i + 1; j < points.Length; j++)
                {
                    float distance = Vector2.Distance(points[i], points[j]);
                    if (distance > maxDistance)
                    {
                        maxDistance = distance;
                        p1 = points[i];
                        p2 = points[j];
                    }
                }
            }
        }

        public void Dispose()
        {
            // Очистка ресурсов при необходимости
        }
    }
}