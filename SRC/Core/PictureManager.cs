using ShapesDetector.Extensions;
using ShapesDetector.Models;
using System.Drawing;

namespace ShapesDetector.Core
{
    internal class PictureManager
    {
        internal readonly Picture img;
        internal readonly PixelColor baseColor;
        internal readonly int tolerance;
        internal readonly int minHeightBlock;
        internal readonly int minWidthBlock;

        public PictureManager(Picture img, int tolerance, int minHeightBlock, int minWidthBlock)
        {
            this.img = img;
            this.tolerance = tolerance;
            this.minHeightBlock = minHeightBlock;
            this.minWidthBlock = minWidthBlock;
            baseColor = img.GetBackgroundColor();
        }

        internal PixelPoint[] GetNextMoves(PixelPoint pointA, PixelPoint pointB, PixelColor baseColor, IEnumerable<PixelPoint> shapePoints)
        {
            var res = new List<(PixelPoint, PixelPoint)>();

            var distances = new List<SearchWayDistance>
            {
                new SearchWayDistance(SearchWays.Bottom, pointA.FindLastVerticalSymetricContrast(pointB, img, baseColor, shapePoints, tolerance, false)),
                new SearchWayDistance(SearchWays.Top, pointA.FindLastVerticalSymetricContrast(pointB, img, baseColor, shapePoints, tolerance, true)),
                new SearchWayDistance(SearchWays.Left, pointA.FindLastHorizontalSymetricContrast(pointB, img, baseColor, shapePoints, tolerance, true)),
                new SearchWayDistance(SearchWays.Right, pointA.FindLastHorizontalSymetricContrast(pointB, img, baseColor, shapePoints, tolerance, false))
            };

            var bestDistance = distances.MaxBy(x => x.Points.Count());
            var newA = bestDistance.Transform(pointA);
            var newB = bestDistance.Transform(pointB);
            //Console.WriteLine($"[{bestDistance.Way}] {bestDistance.Distance} [{newA} | {newB}]");
            //Console.ReadLine();
            return bestDistance.Points.Select(point => new PixelPoint(point.Item1, point.Item2)).ToArray();
        }

        internal PixelPoint FindEndSegmentPoint(PixelPoint point)
        {
            var segmentWidth = 1;
            while (point.X + segmentWidth < img.Width - 1 && img.GetPixel(point.X + segmentWidth, point.Y).Color.IsContrast(baseColor))
                segmentWidth++;

            return new PixelPoint(point.X + segmentWidth, point.Y);
        }

        internal Shape[] DetectShapes(List<PixelPoint> currentShapePoints)
        {
            var res = new List<Shape>();
            var stop = false;
            var completeShape = false;
            var pointA = currentShapePoints[currentShapePoints.Count - 2].Clone();
            var pointB = currentShapePoints[currentShapePoints.Count - 1].Clone();
            while (!stop && !completeShape)
            {
                var nextMovesPoint = GetNextMoves(pointA, pointB, baseColor, currentShapePoints);
                if (nextMovesPoint.Count() == 0) // no more symetric points found
                    stop = true;
                else
                {
                    pointA =  nextMovesPoint[currentShapePoints.Count - 2].Clone();
                    pointB = nextMovesPoint[currentShapePoints.Count - 1].Clone();
                    currentShapePoints.AddRange(nextMovesPoint);
                    completeShape = pointA.Distance(pointB) < tolerance;
                }
            }
            res.Add(new Shape(currentShapePoints, completeShape));
            return res.ToArray();
            //return res.Where(x => !res.Any(y => y.Area() > x.Area() && y.IntersectsWith(x))).ToArray();
        }
    }
}
