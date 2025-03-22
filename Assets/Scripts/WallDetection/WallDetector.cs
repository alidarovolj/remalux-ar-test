using UnityEngine;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityUtils;
using System.Collections.Generic;

namespace Remalux.WallDetection
{
      public class WallDetector : MonoBehaviour
      {
            [Header("Detection Settings")]
            [SerializeField] private float minWallLength = 100f; // Минимальная длина стены в пикселях
            [SerializeField] private float verticalAngleThreshold = 5f; // Допустимое отклонение от вертикали в градусах
            [SerializeField] private float lineClusterThreshold = 30f; // Расстояние для кластеризации линий

            [Header("Edge Detection")]
            [SerializeField] private double cannyThreshold1 = 50;
            [SerializeField] private double cannyThreshold2 = 150;
            [SerializeField] private int cannyApertureSize = 3;

            [Header("Debug Visualization")]
            [SerializeField] private bool showDebugImage = true;
            [SerializeField] private GameObject debugQuad;

            private Mat edges;
            private Mat lines;
            private Mat debugMat;

            private void Start()
            {
                  // Инициализация матриц
                  edges = new Mat();
                  lines = new Mat();
                  debugMat = new Mat();
            }

            public void ProcessFrame(Texture2D cameraFrame)
            {
                  // Конвертируем текстуру в Mat
                  Mat frameMat = new Mat(cameraFrame.height, cameraFrame.width, CvType.CV_8UC4);
                  Utils.texture2DToMat(cameraFrame, frameMat);

                  // Конвертируем в градации серого
                  Mat grayMat = new Mat();
                  Imgproc.cvtColor(frameMat, grayMat, Imgproc.COLOR_RGBA2GRAY);

                  // Уменьшаем шум
                  Imgproc.GaussianBlur(grayMat, grayMat, new Size(5, 5), 1.5);

                  // Определяем края
                  Imgproc.Canny(grayMat, edges, cannyThreshold1, cannyThreshold2, cannyApertureSize);

                  // Находим линии
                  Imgproc.HoughLines(edges, lines, 1, Mathf.Deg2Rad, 150);

                  // Создаем копию для визуализации
                  if (showDebugImage)
                  {
                        frameMat.copyTo(debugMat);
                  }

                  // Обрабатываем найденные линии
                  List<Vector4> wallLines = ProcessLines();

                  // Визуализируем результаты
                  if (showDebugImage)
                  {
                        DrawResults(wallLines);
                        UpdateDebugView();
                  }

                  // Освобождаем ресурсы
                  frameMat.release();
                  grayMat.release();
            }

            private List<Vector4> ProcessLines()
            {
                  List<Vector4> wallLines = new List<Vector4>();

                  if (lines.empty())
                        return wallLines;

                  // Получаем данные линий
                  float[] linesData = new float[lines.rows() * lines.cols() * lines.channels()];
                  lines.get(0, 0, linesData);

                  // Обрабатываем каждую линию
                  for (int i = 0; i < linesData.Length; i += 2)
                  {
                        float rho = linesData[i];
                        float theta = linesData[i + 1];

                        // Проверяем, вертикальная ли линия
                        float angleDeg = theta * Mathf.Rad2Deg;
                        if (IsVerticalLine(angleDeg))
                        {
                              // Вычисляем точки линии
                              double a = Mathf.Cos(theta);
                              double b = Mathf.Sin(theta);
                              double x0 = a * rho;
                              double y0 = b * rho;

                              double x1 = x0 + 1000 * (-b);
                              double y1 = y0 + 1000 * (a);
                              double x2 = x0 - 1000 * (-b);
                              double y2 = y0 - 1000 * (a);

                              // Добавляем линию если она достаточно длинная
                              float length = Vector2.Distance(
                                  new Vector2((float)x1, (float)y1),
                                  new Vector2((float)x2, (float)y2)
                              );

                              if (length >= minWallLength)
                              {
                                    wallLines.Add(new Vector4((float)x1, (float)y1, (float)x2, (float)y2));
                              }
                        }
                  }

                  return ClusterWallLines(wallLines);
            }

            private bool IsVerticalLine(float angleDeg)
            {
                  // Проверяем, находится ли угол около 90 или 270 градусов
                  float angle90Diff = Mathf.Abs(angleDeg - 90);
                  float angle270Diff = Mathf.Abs(angleDeg - 270);

                  return angle90Diff <= verticalAngleThreshold ||
                         angle270Diff <= verticalAngleThreshold;
            }

            private List<Vector4> ClusterWallLines(List<Vector4> lines)
            {
                  List<Vector4> clusteredLines = new List<Vector4>();
                  List<bool> processed = new List<bool>();

                  for (int i = 0; i < lines.Count; i++)
                        processed.Add(false);

                  // Кластеризуем близкие линии
                  for (int i = 0; i < lines.Count; i++)
                  {
                        if (processed[i])
                              continue;

                        List<Vector4> cluster = new List<Vector4>();
                        cluster.Add(lines[i]);
                        processed[i] = true;

                        for (int j = i + 1; j < lines.Count; j++)
                        {
                              if (processed[j])
                                    continue;

                              // Проверяем расстояние между линиями
                              if (AreLinesSimilar(lines[i], lines[j]))
                              {
                                    cluster.Add(lines[j]);
                                    processed[j] = true;
                              }
                        }

                        // Добавляем усредненную линию из кластера
                        if (cluster.Count > 0)
                        {
                              clusteredLines.Add(AverageLines(cluster));
                        }
                  }

                  return clusteredLines;
            }

            private bool AreLinesSimilar(Vector4 line1, Vector4 line2)
            {
                  // Вычисляем среднюю точку каждой линии
                  Vector2 mid1 = new Vector2(
                      (line1.x + line1.z) * 0.5f,
                      (line1.y + line1.w) * 0.5f
                  );

                  Vector2 mid2 = new Vector2(
                      (line2.x + line2.z) * 0.5f,
                      (line2.y + line2.w) * 0.5f
                  );

                  return Vector2.Distance(mid1, mid2) < lineClusterThreshold;
            }

            private Vector4 AverageLines(List<Vector4> lines)
            {
                  if (lines.Count == 0)
                        return Vector4.zero;

                  Vector2 startSum = Vector2.zero;
                  Vector2 endSum = Vector2.zero;

                  foreach (Vector4 line in lines)
                  {
                        startSum += new Vector2(line.x, line.y);
                        endSum += new Vector2(line.z, line.w);
                  }

                  startSum /= lines.Count;
                  endSum /= lines.Count;

                  return new Vector4(startSum.x, startSum.y, endSum.x, endSum.y);
            }

            private void DrawResults(List<Vector4> wallLines)
            {
                  // Рисуем найденные стены
                  foreach (Vector4 line in wallLines)
                  {
                        Imgproc.line(debugMat,
                            new Point(line.x, line.y),
                            new Point(line.z, line.w),
                            new Scalar(0, 255, 0, 255), 2);
                  }
            }

            private void UpdateDebugView()
            {
                  if (debugQuad != null)
                  {
                        Texture2D debugTexture = new Texture2D(debugMat.cols(), debugMat.rows(),
                            TextureFormat.RGBA32, false);
                        Utils.matToTexture2D(debugMat, debugTexture);
                        debugQuad.GetComponent<Renderer>().material.mainTexture = debugTexture;
                  }
            }

            private void OnDestroy()
            {
                  if (edges != null) edges.release();
                  if (lines != null) lines.release();
                  if (debugMat != null) debugMat.release();
            }
      }
}