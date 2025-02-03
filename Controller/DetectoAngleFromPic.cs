using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using System.Drawing.Imaging;

namespace Controller
{
    /// <summary>
    /// Класс для детекции угла на изображении с использованием методов компьютерного зрения.
    /// Реализует алгоритм предобработки изображения, выделения контуров, аппроксимации и вычисления угла.
    /// </summary>
    public class DrillAngleDetector
    {
        // Параметры для предобработки изображения:
        private static readonly Size _blurSize = new Size(5, 5); // Размер ядра для Гауссова размытия.
        private const int _adaptiveBlockSize = 21;               // Размер блока для адаптивного порога.
        private const int _cannyThreshold = 50;                  // Порог для оператора Кэнни.
        private const double _epsilonFactor = 0.02;              // Фактор для аппроксимации контура (Рамера-Дугласа-Пекера).

        /// <summary>
        /// Метод обнаружения угла на изображении.
        /// Если угол найден, возвращается обработанное изображение и angleFound = true,
        /// иначе – исходное изображение и angleFound = false.
        /// </summary>
        /// <param name="bitmap">Исходное изображение в формате Bitmap.</param>
        /// <param name="bmpData">Битовые данные изображения.</param>
        /// <param name="angleFound">Флаг обнаружения угла.</param>
        /// <returns>Обработанное изображение с выделением угла или исходное изображение.</returns>
        public static Bitmap Detect(Bitmap bitmap, BitmapData bmpData, out bool angleFound)
        {
            angleFound = false;
            try
            {
                // Создаем матрицу из Bitmap для обработки алгоритмами Emgu CV.
                using (Mat src = new Mat(bitmap.Height, bitmap.Width, DepthType.Cv8U, 3, bmpData.Scan0, bmpData.Stride))
                {
                    // Предобработка: преобразование, размытие, адаптивный порог.
                    using (Mat processed = PreprocessImage(src))
                    {
                        // Поиск контуров на обработанном изображении.
                        VectorOfVectorOfPoint contours = FindContours(processed);
                        if (contours.Size == 0)
                            return bitmap;

                        // Выбор основного контура (с максимальной площадью).
                        VectorOfPoint contour = GetMainContour(contours);
                        VectorOfPoint approx = new VectorOfPoint();
                        double epsilon = _epsilonFactor * CvInvoke.ArcLength(contour, true);
                        // Аппроксимация контура методом Рамера-Дугласа-Пекера.
                        CvInvoke.ApproxPolyDP(contour, approx, epsilon, true);

                        Point[] vertices = approx.ToArray();
                        // Если вершин меньше 3, угол определить невозможно.
                        if (vertices.Length < 3)
                            return bitmap;

                        // Если ровно 4 вершины – выполняем фильтрацию, чтобы устранить близкие точки.
                        if (vertices.Length == 4)
                            vertices = FilterVertices(vertices, 20);

                        if (vertices.Length < 3)
                            return bitmap;

                        // Вычисляем угол по выбранным вершинам.
                        double angle = CalculateAngle(vertices);
                        angleFound = true;
                        // Визуализируем результат: рисуем контур, вершины и текст с углом.
                        return VisualizeResult(src, contour, vertices, angle);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка в детекции угла: " + ex);
                return bitmap;
            }
        }

        /// <summary>
        /// Предобработка изображения: преобразование в оттенки серого, Гауссово размытие и адаптивный порог.
        /// </summary>
        private static Mat PreprocessImage(Mat src)
        {
            Mat processed = new Mat();
            CvInvoke.CvtColor(src, processed, ColorConversion.Bgr2Gray);
            CvInvoke.GaussianBlur(processed, processed, _blurSize, 1.5);
            CvInvoke.AdaptiveThreshold(processed, processed, 255,
                AdaptiveThresholdType.MeanC, ThresholdType.BinaryInv, _adaptiveBlockSize, 5);
            return processed;
        }

        /// <summary>
        /// Поиск контуров на изображении с использованием оператора Кэнни.
        /// </summary>
        private static VectorOfVectorOfPoint FindContours(Mat processed)
        {
            using (Mat edges = new Mat())
            {
                CvInvoke.Canny(processed, edges, _cannyThreshold, _cannyThreshold * 3);
                VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint();
                CvInvoke.FindContours(edges, contours, null, RetrType.List, ChainApproxMethod.ChainApproxSimple);
                return contours;
            }
        }

        /// <summary>
        /// Выбирает основной контур (с максимальной площадью) из найденных контуров.
        /// </summary>
        private static VectorOfPoint GetMainContour(VectorOfVectorOfPoint contours)
        {
            VectorOfPoint mainContour = new VectorOfPoint();
            double maxArea = 0;
            for (int i = 0; i < contours.Size; i++)
            {
                double area = CvInvoke.ContourArea(contours[i]);
                if (area > maxArea)
                {
                    maxArea = area;
                    mainContour = contours[i];
                }
            }
            return mainContour;
        }

        /// <summary>
        /// Фильтрация вершин: удаляет точки, расстояние между которыми меньше заданного минимума.
        /// </summary>
        /// <param name="points">Исходный массив точек.</param>
        /// <param name="minDistance">Минимальное расстояние между точками.</param>
        /// <returns>Отфильтрованный массив точек.</returns>
        private static Point[] FilterVertices(Point[] points, int minDistance)
        {
            Point[] result = new Point[points.Length];
            int count = 0;
            foreach (Point p in points)
            {
                bool keep = true;
                for (int i = 0; i < count; i++)
                {
                    if (Distance(p, result[i]) < minDistance)
                    {
                        keep = false;
                        break;
                    }
                }
                if (keep)
                    result[count++] = p;
            }
            Array.Resize(ref result, count);
            return result;
        }

        /// <summary>
        /// Вычисляет евклидову дистанцию между двумя точками.
        /// </summary>
        private static double Distance(Point a, Point b)
        {
            int dx = a.X - b.X, dy = a.Y - b.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        /// <summary>
        /// Вычисляет угол между двумя линиями, проведенными от верхней точки к двум опорным.
        /// Сортирует точки по координате Y и считает, что самая верхняя точка – это вершина угла.
        /// </summary>
        private static double CalculateAngle(Point[] vertices)
        {
            Array.Sort(vertices, (a, b) => a.Y.CompareTo(b.Y));
            Point top = vertices[0];
            // Берем две последние точки после сортировки как опорные
            Point base1 = vertices[^2];
            Point base2 = vertices[^1];

            double angle1 = Math.Atan2(base1.Y - top.Y, base1.X - top.X) * 180 / Math.PI;
            double angle2 = Math.Atan2(base2.Y - top.Y, base2.X - top.X) * 180 / Math.PI;
            return Math.Abs(angle1 - angle2);
        }

        /// <summary>
        /// Визуализация результата детекции угла.
        /// Рисует контур, выделяет вершины и выводит текст с вычисленным углом.
        /// </summary>
        private static Bitmap VisualizeResult(Mat src, VectorOfPoint contour, Point[] vertices, double angle)
        {
            using (Mat result = src.Clone())
            {
                // Рисуем контур зеленым цветом толщиной 2 пикселя.
                CvInvoke.DrawContours(result, new VectorOfVectorOfPoint(contour), -1, new MCvScalar(0, 255, 0), 2);
                // Отмечаем каждую вершину красным кругом.
                foreach (Point p in vertices)
                    CvInvoke.Circle(result, p, 5, new MCvScalar(0, 0, 255), -1);

                // Выводим текст с вычисленным углом.
                CvInvoke.PutText(result, $"Угол: {angle:F1}", new Point(20, 50),
                    FontFace.HersheySimplex, 1.5, new MCvScalar(255, 0, 0), 3);

                // Кодирование результата в формат Bitmap и возврат.
                using (VectorOfByte vb = new VectorOfByte())
                {
                    CvInvoke.Imencode(".bmp", result, vb);
                    vb.Seek(0, System.IO.SeekOrigin.Begin);
                    return new Bitmap(vb);
                }
            }
        }
    }
}
