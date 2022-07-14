using System.Drawing;

namespace ShapesDetector
{
    public static class BitmapRectanglesExtractor
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
        private static (int, int)[] FindLeftBlocks(this IEnumerable<(int, int)> knownBlocs, (int, int) current)
        {
            var res = new List<(int, int)>();
            int top = current.Item2, left = current.Item1, min = knownBlocs.Min(x => x.Item1);
            while (--left >= min && knownBlocs.Contains((left, top)))
                res.Add((left, top));
            return res.ToArray();
        }
        private static (int, int)[] FindRightBlocks(this IEnumerable<(int, int)> knownBlocs, (int, int) current)
        {
            var res = new List<(int, int)>();
            int top = current.Item2, left = current.Item1, max = knownBlocs.Max(x => x.Item1);
            while (++left <= max && knownBlocs.Contains((left, top)))
                res.Add((left, top));
            return res.ToArray();
        }
        private static (int, int)[] FindBottomBlocks(this IEnumerable<(int, int)> knownBlocs, (int, int) current)
        {
            var res = new List<(int, int)>();
            int top = current.Item2, left = current.Item1, max = knownBlocs.Max(x => x.Item2);
            while (++top <= max && knownBlocs.Contains((left, top)))
                res.Add((left, top));
            return res.ToArray();
        }
        private static (int, int)[] FindTopBlocks(this IEnumerable<(int, int)> knownBlocs, (int, int) current)
        {
            var res = new List<(int, int)>();
            int top = current.Item2, left = current.Item1, min = knownBlocs.Min(x => x.Item2);
            while (--top >= min && knownBlocs.Contains((left, top)))
                res.Add((left, top));
            return res.ToArray();
        }

        private static RectangleF[] ExtractCompleteShapes(this Dictionary<(int, int), Guid> blocksIdsPerPositions, int tolerance)
        {
            var res = new List<RectangleF>();

            var groupPoints = blocksIdsPerPositions.GroupBy(x => x.Value).ToArray();
            foreach (var pointsPerGroupId in blocksIdsPerPositions.GroupBy(x => x.Value))
            {
                var points = pointsPerGroupId.Select(x => x.Key).ToArray();
                var minX = points.Min(x => x.Item1);
                var maxX = points.Max(x => x.Item1);
                var minY = points.Min(x => x.Item2);
                var maxY = points.Max(x => x.Item2);
                var rect = new RectangleF(minX, minY, maxX - minX, maxY - minY);
                if (rect.Width < 100 && rect.Height < 100)
                    res.Add(rect);
            }
            return res.Where(block => !res.Any(x => x.Size.Height > block.Height && x.IntersectsWith(block))).ToArray();
        }

        private enum Way // Need 1 diff between opposites
        {
            Right = 1,
            Left = 2,
            Top = 4,
            Bottom = 5,
        };

        private static bool IsValidPath(this Way way, Way lastWay) => lastWay != way && Math.Abs((int)lastWay - (int)way) != 1;
        private static RectangleF ToRectangle(this IEnumerable<(int, int)> groupsPoints)
        {
            var minX = groupsPoints.Min(x => x.Item1);
            var maxX = groupsPoints.Max(x => x.Item1);
            var minY = groupsPoints.Min(x => x.Item2);
            var maxY = groupsPoints.Max(x => x.Item2);

            return new RectangleF(minX, minY, maxX - minX, maxY - minY);
        }
        private static bool IsInZone(this (int, int) point, RectangleF rec)
        {
            int x = point.Item1, y = point.Item2;
            return x >= rec.X && x <= (rec.Width + rec.X)
                && y >= rec.Y && y <= (rec.Height + rec.Y);
        }

        private static RectangleF[] ExtractShapes(this List<(int, int)> points, int tolerance, int minHeightBlock = 15, int minWidthBlock = 40)
        {
            if (!points.Any())
                return Array.Empty<RectangleF>();

            var res = new List<RectangleF>();

            var pointsGroups = new Dictionary<Guid, (int, int)[]>();

            while (points.Count > 0)
            {
                var groupId = Guid.NewGuid();
                var startPoint = points.OrderBy(x => x.Item2).ThenBy(x => x.Item1).First(); // From top, first left
                var currentGroup = new Dictionary<(int, int), Guid> { { startPoint, groupId } };
                var currentPoint = (startPoint.Item1, startPoint.Item2);
                var newPoints = new List<(int, int)>();
                var path = new List<Way> { Way.Left };

                do
                {
                    var lastWay = path.Last();
                    newPoints.Clear();

                    foreach (var way in ((Way[])Enum.GetValues(typeof(Way))).OrderBy(x => Math.Abs((int)lastWay - (int)x)))
                    {
                        if (way.IsValidPath(lastWay))
                        {
                            var wayPoints = way switch
                            {
                                Way.Bottom => points.FindBottomBlocks(currentPoint),
                                Way.Top => points.FindTopBlocks(currentPoint),
                                Way.Left => points.FindLeftBlocks(currentPoint),
                                _ => points.FindRightBlocks(currentPoint),
                            };
                            if (wayPoints.Any() && !wayPoints.Any(point => currentGroup.ContainsKey(point)))
                            {
                                //Console.WriteLine($"[{way}]\t{string.Join(',', wayPoints.Select(pt => "(" + pt.Item1 + "," + pt.Item2 + ")"))}");
                                path.Add(way);
                                newPoints.AddRange(wayPoints);
                                currentPoint = way switch
                                {
                                    Way.Bottom => newPoints.MaxBy(x => x.Item2),
                                    Way.Top => newPoints.MinBy(x => x.Item2),
                                    Way.Left => newPoints.MinBy(x => x.Item1),
                                    _ => newPoints.MaxBy(x => x.Item1),
                                };
                            }
                            else
                            {
                                newPoints.AddRange(wayPoints.Where(point => !currentGroup.ContainsKey(point)));
                            }
                        }
                    }

                    newPoints.ForEach(point => currentGroup[point] = groupId);
                } while (newPoints.Any());
                var rect = currentGroup.Keys.ToRectangle();
                points = points.Where(pt => !pt.IsInZone(rect)).ToList();
                pointsGroups[groupId] = currentGroup.Keys.ToArray();
                if(rect.Width >= minWidthBlock && rect.Height > minHeightBlock)
                {
                    res.Add(rect);
                }
            }

            return res.ToArray();

        }
        public static RectangleF[] ExtractRectangles(this Bitmap img, int tolerance = 3, int minHeightBlock = 15, int minWidthBlock = 40)
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
            return coloredPoints.ExtractShapes(tolerance);
        }

    }
}
