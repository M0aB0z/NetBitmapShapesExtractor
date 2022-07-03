using System.Drawing;

namespace ShapesDetector
{
    public static class BitmapRectanglesExtractor
    {
        private static RectangleF[] ConvertSegmentsToRectangle(this Dictionary<(int, int), List<BorderSegment>> segments, int stepIdx, int minHeight, int minWidth, int tolerance)
        {
            var blocks = new List<RectangleF>();
            foreach (var blockGroup in segments.Values.SelectMany(x => x).GroupBy(x => x.BlockId))
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

                var anyCompletedBottomSegment = blockGroup.Where(block => block.Y >= maxY - tolerance).Any(block => block.Width() >= blockWidth - tolerance);
                if (!anyCompletedBottomSegment)
                    continue;

                var anyCompletedTopSegment = blockGroup.Where(block => block.Y <= minY + tolerance).Any(segment => segment.Width() >= blockWidth - tolerance);
                if (!anyCompletedBottomSegment)
                    continue;

                var anyCompletedLeftSegment = blockGroup.Where(block => block.FromX <= minX + tolerance)
                    .GroupBy(x => x.FromX).Any(segmentsForX => segmentsForX.Count() * stepIdx >= blockHeight - tolerance);
                if (!anyCompletedLeftSegment)
                    continue;

                blocks.Add(new RectangleF(minX, minY, blockWidth, blockHeight));
            }

            return blocks.ToArray();
        }

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
        private static Guid? FindClothestBlockId(this Dictionary<(int, int), List<BorderSegment>> segments, int fromX, int top, int tolerance)
        {
            for (var startLeftTolerance = 0; startLeftTolerance <= tolerance; startLeftTolerance++)
            {
                for (var topTolerance = 1; topTolerance <= tolerance; topTolerance++)
                {
                    var segmentKey = (fromX - startLeftTolerance, top - topTolerance);
                    if (segments.ContainsKey(segmentKey))
                        return segments[segmentKey].First().BlockId;
                }
            }
            return null;
        }
        private static int Width(this BorderSegment segment) => segment.ToX - segment.FromX;
        public static RectangleF[] ExtractRectangles(this Bitmap img, int tolerance = 5, int minHeightBlock = 30, int minWidthBlock = 50)
        {
            const int stepIdx = 1;
            var baseColor = img.GetBackgroundColor();
            int currentTop = 0;
            var horizontalSegmentsPerXY = new Dictionary<(int, int), List<BorderSegment>>();
            var currentStartSegmentX = -1;
            while (currentTop < img.Height - stepIdx)
            {
                var currentLeft = 0;
                while (currentLeft < img.Width - stepIdx)
                {
                    var dist = img.GetPointColorDist(currentLeft, currentTop, stepIdx, stepIdx, stepIdx, baseColor);
                    if (dist < 50)
                    {
                        if (currentStartSegmentX > -1) // this is an end of a previously detected border 
                        {
                            var blockId = horizontalSegmentsPerXY.FindClothestBlockId(currentStartSegmentX, currentTop, tolerance) ?? Guid.NewGuid();
                            var blockKey = (currentStartSegmentX, currentTop);
                            var segment = new BorderSegment(currentStartSegmentX, currentLeft, currentTop, blockId);

                            if (!horizontalSegmentsPerXY.ContainsKey(blockKey))
                                horizontalSegmentsPerXY[blockKey] = new List<BorderSegment> { segment };
                            else
                                horizontalSegmentsPerXY[blockKey].Add(segment);

                            currentStartSegmentX = -1;
                        }
                    }
                    else if (currentStartSegmentX == -1) // this is a start of a new border
                    {
                        currentStartSegmentX = currentLeft;
                    }
                    currentLeft += stepIdx;
                }
                currentStartSegmentX = -1;
                currentTop += stepIdx;
            }

            return horizontalSegmentsPerXY.ConvertSegmentsToRectangle(stepIdx, minHeightBlock, minWidthBlock, tolerance);
        }
    }
}
