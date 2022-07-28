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
        private enum Way
        {
            Left = 0,
            Right = 1,
        }

        private static double Distance(this (int, int) pointA, (int, int) pointB) => Math.Sqrt((Math.Pow(pointB.Item1 - pointA.Item1, 2)) + (Math.Pow(pointB.Item2 - pointA.Item2, 2)));
        private static (int, int) SearchLeftPoint(this (int, int) point, IEnumerable<(int, int)> allPoints, IEnumerable<(int, int)> currentShape, int tolerance, bool fromPreviousFailure = false)
        {
            int x = point.Item1, y = point.Item2;

            for (int currentY = y; currentY <= y + tolerance; currentY++)
                for (int currentX = x, triesX = 0; triesX <= tolerance; currentX--, triesX++)
                    if (allPoints.Contains((currentX, currentY)) && !currentShape.Contains((currentX, currentY)))
                        return (currentX, currentY);
            return fromPreviousFailure ? default : point.SearchRightPoint(allPoints, currentShape, tolerance, true);
        }

        private static (int, int) SearchRightPoint(this (int, int) point, IEnumerable<(int, int)> allPoints, IEnumerable<(int, int)> currentShape, int tolerance, bool fromPreviousFailure = false)
        {
            int x = point.Item1, y = point.Item2;
            for (int currentY = y; currentY <= y + tolerance; currentY++)
                for (int currentX = x, triesX = 0; triesX <= tolerance; currentX++, triesX++)
                    if (allPoints.Contains((currentX, currentY)) && !currentShape.Contains((currentX, currentY)))
                        return (currentX, currentY);
            return fromPreviousFailure ? default : point.SearchLeftPoint(allPoints, currentShape, tolerance, true);
        }

        private static IEnumerable<(int, int)> ExtractShape(this (int, int) startPoint, IEnumerable<(int, int)> points, int tolerance, int minHeightBlock = 15, int minWidthBlock = 40)
        {
            var shape = new List<(int, int)> { startPoint };
            var startEndSegment = startPoint.Item1;
            while (points.Contains((++startEndSegment, startPoint.Item2)))
                shape.Add((startEndSegment, startPoint.Item2));
            var ways = Enum.GetValues<Way>();
            var reversedWays = ways.OrderByDescending(x => x).ToArray();
            (int, int) clothestPointA = startPoint, oppositePoint = (startEndSegment - 1, startPoint.Item2), middlePoint = ((startPoint.Item1 + (startEndSegment - startPoint.Item1) / 2), startPoint.Item2);
            for (var i = 0; i < tolerance && (clothestPointA == default || oppositePoint == default || clothestPointA == oppositePoint); i++)
            {
                clothestPointA = clothestPointA.SearchLeftPoint(points, shape, tolerance);
                oppositePoint = oppositePoint.SearchRightPoint(points, shape, tolerance);
                if (clothestPointA != default && oppositePoint != default && clothestPointA != oppositePoint)
                {
                    shape.Add(clothestPointA);
                    shape.Add(oppositePoint);
                }
            }
            bool found;
            do
            {
                found = false;
                var tmpLeftPoint = clothestPointA.Distance(middlePoint) > 5 ? clothestPointA.SearchRightPoint(points, shape, tolerance) : clothestPointA.SearchLeftPoint(points, shape, tolerance);
                if (tmpLeftPoint != default && !shape.Contains(tmpLeftPoint))
                {
                    var tmpRightPoint = clothestPointA.Distance(middlePoint) > 5 ? oppositePoint.SearchLeftPoint(points, shape, tolerance) : oppositePoint.SearchRightPoint(points, shape, tolerance);
                    if (tmpRightPoint != default)
                    {
                        var middlePointDistance = Math.Abs(tmpLeftPoint.Distance(middlePoint) - tmpRightPoint.Distance(middlePoint));
                        if (middlePointDistance < tolerance)
                        {
                            var symetricDistance = Math.Abs(tmpLeftPoint.Distance(tmpLeftPoint) - tmpRightPoint.Distance(tmpRightPoint));

                            clothestPointA = tmpLeftPoint;
                            oppositePoint = tmpRightPoint;
                            shape.AddRange(new[] { clothestPointA, oppositePoint });

                            found = true;
                        }

                    }
                }

            } while (found && oppositePoint != clothestPointA);

            //return shape;
            return oppositePoint.Distance(clothestPointA) <= tolerance ? shape : Array.Empty<(int, int)>();

        }
        private static double Area(this RectangleF rec) => rec.Width * rec.Height;
        private static RectangleF[] ExtractShapes(this List<(int, int)> points, int tolerance, int minHeightBlock = 15, int minWidthBlock = 40)
        {
            if (!points.Any())
                return Array.Empty<RectangleF>();

            //return (240, 54).ExtractShape(points, tolerance, minHeightBlock, minWidthBlock).Select(x => new RectangleF(x.Item1, x.Item2, 1, 1)).ToArray();
            var res = new List<RectangleF>();
            var usedPoints = new List<(int, int)>();
            foreach (var currentPoint in points.OrderBy(x => x.Item2).ThenBy(x => x.Item1))
            {
                if (usedPoints.Contains(currentPoint))
                    continue;

                var shapePoints = currentPoint.ExtractShape(points, tolerance);
                if (shapePoints.Any())
                {
                    var rect = shapePoints.ToRectangle();
                    if (rect.Width >= minWidthBlock && rect.Height >= minHeightBlock)
                    {
                        usedPoints.AddRange(shapePoints);
                        res.Add(rect);
                    }
                }
            }

            return res.Where(x => !res.Any(y => y.Area() > x.Area() && y.IntersectsWith(x))).ToArray();
        }

        public static Guid GetGroupId(this (int, int) point, Dictionary<(int, int), Guid> groupedPoints, int tolerance)
        {
            for (var left = point.Item1 - tolerance; left < point.Item1; left++)
            {
                for (var top = point.Item2 - tolerance; top < point.Item2; top++)
                    if (groupedPoints.ContainsKey((left, top)))
                        return groupedPoints[(left, top)];
            }
            return Guid.NewGuid();
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
                    {
                        var point = (currentLeft, currentTop);
                        coloredPoints.Add(point);
                    }
                }
            }
            //return coloredPoints.Select(x => new RectangleF(x.Item1, x.Item2, 1, 1)).ToArray();
            return coloredPoints.ExtractShapes(tolerance);
        }

    }
}
