using System.Drawing;

namespace ShapesDetector
{
    public static class BitmapRectanglesExtractor
    {
        private record BorderSegment(int FromX, int ToX, int Y, Guid BlockId);
        public static double CompareTo(this Color e1, Color e2)
        {
            long rmean = (e1.R + e2.R) / 2;
            long r = e1.R - e2.R;
            long g = e1.G - e2.G;
            long b = e1.B - e2.B;
            var diff = Math.Sqrt((((512 + rmean) * r * r) >> 8) + 4 * g * g + (((767 - rmean) * b * b) >> 8));

            return diff;
        }
        private static double GetPointColorDist(this Bitmap bmp, int fromX, int fromY, int pointWidth, int pointHeight, int stepSize, Color color)
        {
            var vals = new List<double>();
            for (var x = fromX; x < fromX + pointWidth; x++)
            {
                for (var y = fromY; y < fromY + pointHeight; y++)
                {
                    if (x <= bmp.Width - stepSize && y <= bmp.Height - stepSize)
                    {
                        var px = bmp.GetPixel(x, y);
                        vals.Add(px.CompareTo(color));
                    }
                    else return vals.Any() ? vals.Average() : double.MaxValue;
                }
            }
            return vals.Any() ? vals.Average() : double.MaxValue;
        }
        private static Color GetBackgroundColor(this Bitmap bmp)
        {
            var colors = new List<Color> { bmp.GetPixel(0, 0), bmp.GetPixel(0, bmp.Height - 1), bmp.GetPixel(bmp.Width - 1, 0), bmp.GetPixel(bmp.Width - 1, bmp.Height - 1) };
            return colors.GroupBy(x => x).OrderByDescending(y => y.Count()).First().Key;
        }
        private static Guid? FindClothestBlockId(this Dictionary<(int, int), BorderSegment> segments, int fromX, int top, int tolerance)
        {
            for (var startLeftTolerance = 0; startLeftTolerance <= tolerance; startLeftTolerance++)
            {
                for (var topTolerance = 0; topTolerance <= tolerance; topTolerance++)
                {
                    var segmentKey = (fromX - startLeftTolerance, top - topTolerance);
                    if (segments.ContainsKey(segmentKey))
                        return segments[segmentKey].BlockId;
                }
            }
            return null;
        }
        private static int Width(this BorderSegment segment) => segment.ToX - segment.FromX;
        private static bool AnyContinousHorizontalBorder(this Dictionary<(int, int), BorderSegment> segmentsPerStartX, int y, int startX, int toX, int tolerance, bool fromTop)
        {
            var minWidth = toX - startX - tolerance;
            for (var decalLeft = 0; decalLeft < tolerance; decalLeft++)
            {
                for (var decalTop = 0; decalTop < tolerance; decalTop++)
                {
                    var lineKey = (startX + decalLeft, fromTop ? y + decalTop : y - decalTop);
                    if (segmentsPerStartX.ContainsKey(lineKey))
                    {
                        var lineSegment = segmentsPerStartX[lineKey];
                        if (lineSegment.Width() >= minWidth)
                            return true;
                    }
                }
            }
            return false;
        }
        private static bool AnyContinousVerticalBorder(this Dictionary<(int, int), BorderSegment> segmentsPerMaxX, int x, int startY, int toY, int tolerance, bool fromLeft)
        {
            for (var yIndex = startY; yIndex < toY; yIndex++)
            {
                var found = false;
                for (var decalLeft = 0; decalLeft < tolerance && !found; decalLeft++)
                {
                    for (var decalTop = 0; decalTop < tolerance && !found; decalTop++)
                    {
                        var lineKey = (fromLeft ? x + decalLeft : x - decalLeft, yIndex + decalTop);
                        if (segmentsPerMaxX.ContainsKey(lineKey))
                            found = true;
                    }
                }
                if (!found)
                    return false;
            }
            return true;
        }
        private static RectangleF[] ConvertSegmentsToRectangle(this Dictionary<(int, int), BorderSegment> horizontalSegments, int stepIdx, int minHeight, int minWidth, int tolerance)
        {
            var blocks = new List<RectangleF>();
            foreach (var blockGroup in horizontalSegments.Values.GroupBy(x => x.BlockId))
            {
                var minY = blockGroup.Min(block => block.Y);
                var maxY = blockGroup.Max(block => block.Y);
                var blockHeight = maxY - minY;
                if (blockHeight < minHeight)
                    continue;

                var minX = blockGroup.Min(block => block.FromX);
                var maxX = blockGroup.Max(block => block.ToX);
                var blockWidth = maxX - minX;
                if (blockWidth < minWidth)
                    continue;

                if (!horizontalSegments.AnyContinousHorizontalBorder(minY, minX, maxX, tolerance, true)) // check top border
                    continue;
                if (!horizontalSegments.AnyContinousHorizontalBorder(maxY, minX, maxX, tolerance, false)) // check top border
                    continue;
                if (!horizontalSegments.AnyContinousVerticalBorder(minX, minY, maxY, tolerance, true)) // check left border
                    continue;

                var verticalSegments = blockGroup.ToDictionary(x => (x.ToX, x.Y));
                if (!verticalSegments.AnyContinousVerticalBorder(maxX, minY, maxY, tolerance, false)) // check right border
                    continue;

                blocks.Add(new RectangleF(minX, minY, blockWidth, blockHeight));
            }

            return blocks.ToArray();
        }
        public static RectangleF[] ExtractRectangles(this Bitmap img, int tolerance = 5, int minHeightBlock = 15, int minWidthBlock = 40)
        {
            var baseColor = img.GetBackgroundColor();
            int currentTop = 0;
            var horizontalSegmentsPerXY = new Dictionary<(int, int), BorderSegment>();
            var currentStartSegmentX = -1;
            while (currentTop < img.Height)
            {
                var currentLeft = 0;
                while (currentLeft < img.Width)
                {
                    var dist = img.GetPointColorDist(currentLeft, currentTop, 1, 1, 1, baseColor);
                    if (dist < 50)
                    {
                        if (currentStartSegmentX > -1) // this is an end of a previously detected border 
                        {
                            var blockId = horizontalSegmentsPerXY.FindClothestBlockId(currentStartSegmentX, currentTop, tolerance) ?? Guid.NewGuid();
                            var blockKey = (currentStartSegmentX, currentTop);
                            var segment = new BorderSegment(currentStartSegmentX, currentLeft, currentTop, blockId);

                            horizontalSegmentsPerXY[blockKey] = segment;

                            currentStartSegmentX = -1;
                        }
                    }
                    else if (currentStartSegmentX == -1) // this is a start of a new border
                    {
                        currentStartSegmentX = currentLeft;
                    }
                    currentLeft++;
                }
                //while (currentLeft > 0)
                //{
                //    var dist = img.GetPointColorDist(currentLeft, currentTop, 1, 1, 1, baseColor);
                //    if (dist < 50)
                //    {
                //        if (currentStartSegmentX > -1) // this is an end of a previously detected border 
                //        {
                //            var blockId = horizontalSegmentsPerXY.FindClothestBlockId(currentStartSegmentX, currentTop, tolerance) ?? Guid.NewGuid();
                //            var blockKey = (currentStartSegmentX, currentTop);
                //            var segment = new BorderSegment(currentStartSegmentX, currentLeft, currentTop, blockId);

                //            horizontalSegmentsPerXY[blockKey] = segment;

                //            currentStartSegmentX = -1;
                //        }
                //    }
                //    else if (currentStartSegmentX == -1) // this is a start of a new border
                //    {
                //        currentStartSegmentX = currentLeft;
                //    }
                //    currentLeft --;
                //}
                currentStartSegmentX = -1;
                currentTop ++;
            }

            return horizontalSegmentsPerXY.ConvertSegmentsToRectangle(1, minHeightBlock, minWidthBlock, tolerance);
        }

    }
}
