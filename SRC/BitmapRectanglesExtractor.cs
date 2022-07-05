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
        private static bool IsContrast(this Color pixel, Color color) => pixel.CompareTo(color) > 50;
        private static Color GetBackgroundColor(this Bitmap bmp)
        {
            var colors = new List<Color> { bmp.GetPixel(0, 0), bmp.GetPixel(0, bmp.Height - 1), bmp.GetPixel(bmp.Width - 1, 0), bmp.GetPixel(bmp.Width - 1, bmp.Height - 1) };
            return colors.GroupBy(x => x).OrderByDescending(y => y.Count()).First().Key;
        }
        private static (int, int)? FindNearBlockPixelPosition(this Dictionary<(int, int), Guid> knownPixels, int fromX, int top, int tolerance)
        {
            for (var startLeftTolerance = 0; startLeftTolerance <= tolerance; startLeftTolerance++)
            {
                for (var topTolerance = 0; topTolerance <= tolerance; topTolerance++)
                {
                    var segmentKey = (fromX - startLeftTolerance, top - topTolerance);
                    if (knownPixels.ContainsKey(segmentKey))
                        return segmentKey;
                }
            }
            return null;
        }

        private static bool IsValidRightBorder(this Dictionary<(int, int), Guid> knownPoints, BorderSegment topBorder, int borderHeight, int tolerance)
        {
            var errors = 0;
            for (var y = topBorder.Y; y < topBorder.Y + borderHeight; y++)
            {
                var found = false;
                for (var x = topBorder.ToX; x >= topBorder.ToX - tolerance && !found; x--)
                {
                    if (knownPoints.ContainsKey((x, y)))
                        found = true;
                }
                if (!found && ++errors == tolerance)
                    return false;
            }
            return true;
        }
        private static bool IsValidBottomBorder(this Dictionary<(int, int), Guid> knownPoints, BorderSegment topBorder, int borderHeight, int tolerance)
        {
            var errors = 0;
            var endY = topBorder.Y + borderHeight;
            for (var x = topBorder.FromX; x < topBorder.ToX; x++)
            {
                var found = false;
                for (var y = endY; y >= endY - tolerance && !found; y--)
                {
                    if (knownPoints.ContainsKey((x, y)))
                        found = true;
                }
                if (!found && errors++ == tolerance)
                    return false;
            }
            return true;
        }
        private static RectangleF[] ExtractCompleteShapes(this Dictionary<(int, int), Guid> blocksIdsPerPositions, BorderSegment[] topBorders, Dictionary<Guid, int> leftBordersHeightPerBlocksIds, int tolerance)
        {
            var res = new List<RectangleF>();
            foreach (var top in topBorders)
            {
                if (!leftBordersHeightPerBlocksIds.ContainsKey(top.BlockId)) // Invalid left border
                    continue;
                if (!blocksIdsPerPositions.IsValidRightBorder(top, leftBordersHeightPerBlocksIds[top.BlockId], tolerance))
                    continue;
                if (!blocksIdsPerPositions.IsValidBottomBorder(top, leftBordersHeightPerBlocksIds[top.BlockId], tolerance))
                    continue;
                res.Add(new RectangleF(top.FromX, top.Y, top.ToX - top.FromX, leftBordersHeightPerBlocksIds[top.BlockId]));
            };
            return res.ToArray();
        }
        public static RectangleF[] ExtractRectangles(this Bitmap img, int tolerance = 3, int minHeightBlock = 15, int minWidthBlock = 40)
        {
            var baseColor = img.GetBackgroundColor();
 
            int currentTop = 0;
            var topBordersPerPosition = new Dictionary<Guid, BorderSegment>();
            var borderHeightsPerBlocksIds = new Dictionary<Guid, int>();
            var blockIdsPerPixels = new Dictionary<(int, int), Guid>();
            var currentStartSegmentX = -1;
            while (currentTop < img.Height)
            {
                var currentLeft = 0;
                while (currentLeft < img.Width)
                {
                    if (img.GetPixel(currentLeft, currentTop).IsContrast(baseColor)) // Contrast detected
                    {
                        var knownBlockPixelPos = blockIdsPerPixels.FindNearBlockPixelPosition(currentLeft, currentTop, tolerance);

                        if (knownBlockPixelPos != null)
                        {
                            blockIdsPerPixels[(currentLeft, currentTop)] = blockIdsPerPixels[knownBlockPixelPos.Value];
                            var prevLineKey = (currentLeft, currentTop - 1);
                            if (blockIdsPerPixels.ContainsKey(prevLineKey) && topBordersPerPosition[blockIdsPerPixels[prevLineKey]].FromX == currentLeft)
                            {
                                borderHeightsPerBlocksIds[blockIdsPerPixels[prevLineKey]]++;
                            }
                        }
                        else if (currentStartSegmentX == -1)// this is a start of a new borders
                        {
                            currentStartSegmentX = currentLeft;
                        }
                    }
                    else
                    {
                        if (currentStartSegmentX > -1 && currentLeft - currentStartSegmentX >= minWidthBlock) // this is an end of a previously detected border 
                        {
                            var prevStartBlockKey = (currentStartSegmentX, currentTop - 1);
                            if (!blockIdsPerPixels.ContainsKey(prevStartBlockKey))
                            {
                                var topBorder = new BorderSegment(currentStartSegmentX, currentLeft, currentTop, Guid.NewGuid());
                                topBordersPerPosition[topBorder.BlockId] = topBorder;
                                blockIdsPerPixels[(currentStartSegmentX, currentTop)] = topBorder.BlockId;
                                borderHeightsPerBlocksIds[topBorder.BlockId] = 1;
                            }
                            else
                            {
                                blockIdsPerPixels[(currentStartSegmentX, currentTop)] = blockIdsPerPixels[prevStartBlockKey];
                            }
                        }
                        currentStartSegmentX = -1;
                    }
                    currentLeft++;
                }

                currentStartSegmentX = -1;
                currentTop++;
            }

            return blockIdsPerPixels.ExtractCompleteShapes(topBordersPerPosition.Values.ToArray(), borderHeightsPerBlocksIds
                                    .Where(x => x.Value >= minHeightBlock && topBordersPerPosition.ContainsKey(x.Key))
                                    .ToDictionary(x => x.Key, y => y.Value), tolerance);
        }

    }
}
