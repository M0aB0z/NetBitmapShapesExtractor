using System.Drawing;

namespace ShapesDetector
{
    public static class ShapesExtractor
    {
        private static double CompareTo(this Color e1, Color e2)
        {
            long rmean = (e1.R + e2.R) / 2;
            long r = e1.R - e2.R;
            long g = e1.G - e2.G;
            long b = e1.B - e2.B;
            var diff = Math.Sqrt((((512 + rmean) * r * r) >> 8) + 4 * g * g + (((767 - rmean) * b * b) >> 8));

            return diff;
        }
        private static bool IsContrast(this Color pixel, Color color) => pixel.CompareTo(color) > 50;
        private static bool IsBorder(this Bitmap img, int x, int y, Color baseColor)
        {
            for (var currentX = x - 1; currentX <= x + 1 && x > 0 && x < img.Width; currentX++)
                for (var currentY = y - 1; currentY <= y + 1 && y > 0 && y < img.Height; currentY++)
                {
                    if ((currentX != x || currentY != y) && !img.GetPixel(currentX, currentY).IsContrast(baseColor))
                        return true;
                }
            return false;
        }

        private static Color GetBackgroundColor(this Bitmap bmp)
        {
            var colors = new List<Color> { bmp.GetPixel(0, 0), bmp.GetPixel(0, bmp.Height - 1), bmp.GetPixel(bmp.Width - 1, 0), bmp.GetPixel(bmp.Width - 1, bmp.Height - 1) };
            return colors.GroupBy(x => x).OrderByDescending(y => y.Count()).First().Key;
        }

        private static RectangleF ToRectangle(this IEnumerable<(int, int)> groupsPoints)
        {
            var minX = groupsPoints.Min(x => x.Item1);
            var maxX = groupsPoints.Max(x => x.Item1);
            var minY = groupsPoints.Min(x => x.Item2);
            var maxY = groupsPoints.Max(x => x.Item2);

            return new RectangleF(minX, minY, Math.Max(maxX - minX, 1), Math.Max(maxY - minY, 1));
        }
        private static bool IsInZone(this (int, int) point, RectangleF rec)
        {
            int x = point.Item1, y = point.Item2;
            return x >= rec.X && x <= (rec.Width + rec.X)
                && y >= rec.Y && y <= (rec.Height + rec.Y);
        }
        private static bool HasTwoSides(this (int, int) point, IEnumerable<(int, int)> points)
        {
            int sides = 0;
            int x = point.Item1, y = point.Item2;
            if (points.Contains((x + 1, y)) && points.Contains((x + 2, y)) && points.Contains((x + 3, y)))
                sides++;
            if (points.Contains((x, y + 1)) && points.Contains((x, y + 2)) && points.Contains((x, y + 3)))
                sides++;
            if (points.Contains((x - 1, y + 1)) && points.Contains((x - 2, y + 1)) && points.Contains((x - 3, y + 1)))
                sides++;
            if (points.Contains((x + 1, y + 1)) && points.Contains((x + 2, y + 2)) && points.Contains((x + 3, y + 3)))
                sides++;
            if (points.Contains((x - 1, y + 1)) && points.Contains((x - 2, y + 2)) && points.Contains((x - 3, y + 3)))
                sides++;
            return sides >= 2;
        }
        private enum Way
        {
            Right = 0,
            Bottom = 1,
            BottomRight = 2,
            BottomLeft = 3,
            Left = 4,
        }
        private static Way GetSymetricMove(this Way way)
        {
            return way switch
            {
                Way.Left => Way.Right,
                Way.Right => Way.Left,
                Way.BottomLeft => Way.BottomRight,
                Way.BottomRight => Way.BottomLeft,
                _ => Way.Bottom
            };
        }
        private static double Distance(this (int, int) pointA, (int, int) pointB) => Math.Sqrt((Math.Pow(pointB.Item1 - pointA.Item1, 2)) + (Math.Pow(pointB.Item2 - pointA.Item2, 2)));
        private static (int, int) GetClothestPoint(this (int, int) point, Way way, IEnumerable<(int, int)> points)
        {
            int x = point.Item1, y = point.Item2;
            return way switch
            {
                Way.Left => points.Contains((x - 1, y)) ? (x - 1, y) : default,
                Way.Right => points.Contains((x + 1, y)) ? (x + 1, y) : default,
                Way.BottomLeft => points.Contains((x - 1, y + 1)) ? (x - 1, y + 1) : default,
                Way.BottomRight => points.Contains((x + 1, y + 1)) ? (x + 1, y + 1) : default,
                _ => points.Contains((x, y + 1)) ? (x, y + 1) : default
            };
        }
        private static IEnumerable<(int, int)> ExtractShape(this (int, int) startPoint, IEnumerable<(int, int)> points, int tolerance, int minHeightBlock = 15, int minWidthBlock = 40)
        {
            var shape = new List<(int, int)> { startPoint };
            (int, int) clothestPointA = startPoint, oppositePoint = startPoint;
            bool found;
            do
            {
                var ways =
                    clothestPointA.Item1 < oppositePoint.Item1
                    ? Enum.GetValues(typeof(Way)).Cast<Way>().OrderBy(x => (int)x).ToArray()
                    : Enum.GetValues(typeof(Way)).Cast<Way>().OrderByDescending(x => (int)x).ToArray();
                found = false;
                var wayIdx = 0;
                do
                {
                    var way = ways[wayIdx];
                    var tmpClothestPointA = clothestPointA.GetClothestPoint(way, points);
                    if (tmpClothestPointA != default && !shape.Contains(tmpClothestPointA))
                    {
                        oppositePoint = oppositePoint.GetClothestPoint(way.GetSymetricMove(), points);
                        if (oppositePoint != default)
                        {
                            var distA = clothestPointA.Distance(startPoint);
                            var distB = oppositePoint.Distance(startPoint);
                            var diff = Math.Abs(distA - distB);
                            //Console.WriteLine($"{clothestPointA} {oppositePoint} [{diff}]");
                            if (diff < 3)
                            {
                                clothestPointA = tmpClothestPointA;
                                shape.AddRange(new[] { clothestPointA, oppositePoint });
                                found = true;
                                //Console.ReadKey();
                            }
                        }
                    }
                } while (!found && ++wayIdx < ways.Length);

            } while (found && oppositePoint != clothestPointA);


            return shape;

        }

        private static RectangleF[] ExtractShapes(this List<(int, int)> points, int tolerance, int minHeightBlock = 15, int minWidthBlock = 40)
        {
            if (!points.Any())
                return Array.Empty<RectangleF>();

            var res = new List<RectangleF>();

            var pointsPerGroups = new Dictionary<Guid, (int, int)[]>();

            (int, int) currentPoint = default;

            do
            {
                //currentPoint = points.OrderBy(x => x.Item2).ThenBy(x => x.Item1).FirstOrDefault();
                currentPoint = (215, 36);
                if (currentPoint != default)
                {
                    var shape = currentPoint.ExtractShape(points, tolerance).Select(x => new RectangleF(x.Item1, x.Item2, 1, 1)).ToArray();
                    return shape;
                }
            } while (currentPoint != default);

            return res.ToArray();

        }
        public static RectangleF[] ExtractShapes(this Bitmap img, int tolerance = 3, int minHeightBlock = 15, int minWidthBlock = 40)
        {
            var baseColor = img.GetBackgroundColor();
            var coloredPoints = new List<(int, int)>();
            for (int currentTop = 0; currentTop < img.Height; currentTop++)
            {
                for (int currentLeft = 0; currentLeft < img.Width; currentLeft++)
                {
                    var pixel = img.GetPixel(currentLeft, currentTop);
                    if (pixel.IsContrast(baseColor) && img.IsBorder(currentLeft, currentTop, baseColor)) // Contrast detected
                        coloredPoints.Add((currentLeft, currentTop));
                }
            }
            //return coloredPoints.Select(x => new RectangleF(x.Item1, x.Item2, 1, 1)).ToArray();
            return coloredPoints.ExtractShapes(tolerance);
        }

    }
}
