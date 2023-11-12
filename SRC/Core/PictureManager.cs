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

        private PossibleMoveWay GetNextPossibleMove(PixelPoint pointA, PixelPoint pointB, PixelColor baseColor, IEnumerable<PixelPoint> shapePoints, SearchWay previousWay)
        {
            var res = new List<PossibleMoveWay>();

            List<SearchWayDistance> distances = (previousWay switch
            {
                SearchWay.Right => new List<SearchWayDistance>
               {
                   new SearchWayDistance(SearchWay.Bottom, pointA.FindLastVerticalSymetricContrast(pointB, img, baseColor, shapePoints, tolerance, false)),
                   new SearchWayDistance(SearchWay.Top, pointA.FindLastVerticalSymetricContrast(pointB, img, baseColor, shapePoints, tolerance, true)),
                   new SearchWayDistance(SearchWay.BottomRight, pointA.FindSymetricConstrastedCorner(pointB, img, baseColor, shapePoints, tolerance, false, false)),
                   new SearchWayDistance(SearchWay.TopRight, pointA.FindSymetricConstrastedCorner(pointB, img, baseColor, shapePoints, tolerance, true, false))
               },
                SearchWay.Left => new List<SearchWayDistance>
               {
                   new SearchWayDistance(SearchWay.Bottom, pointA.FindLastVerticalSymetricContrast(pointB, img, baseColor, shapePoints, tolerance, false)),
                   new SearchWayDistance(SearchWay.Top, pointA.FindLastVerticalSymetricContrast(pointB, img, baseColor, shapePoints, tolerance, true)),
                   new SearchWayDistance(SearchWay.BottomRight, pointA.FindSymetricConstrastedCorner(pointB, img, baseColor, shapePoints, tolerance, false, false)),
                   new SearchWayDistance(SearchWay.TopRight, pointA.FindSymetricConstrastedCorner(pointB, img, baseColor, shapePoints, tolerance, true, false))
               },
                SearchWay.BottomRight => new List<SearchWayDistance>
               {
                   new SearchWayDistance(SearchWay.Bottom, pointA.FindLastVerticalSymetricContrast(pointB, img, baseColor, shapePoints, tolerance, false)),
                   new SearchWayDistance(SearchWay.BottomRight, pointA.FindSymetricConstrastedCorner(pointB, img, baseColor, shapePoints, tolerance, false, false)),
                   new SearchWayDistance(SearchWay.Left,  pointA.FindLastHorizontalSymetricContrast(pointB, img, baseColor, shapePoints, tolerance, true)),
               },
                SearchWay.TopRight => new List<SearchWayDistance>
               {
                   new SearchWayDistance(SearchWay.Top, pointA.FindLastVerticalSymetricContrast(pointB, img, baseColor, shapePoints, tolerance, true)),
                   new SearchWayDistance(SearchWay.TopRight, pointA.FindSymetricConstrastedCorner(pointB, img, baseColor, shapePoints, tolerance, true, false)),
                   new SearchWayDistance(SearchWay.Right,  pointA.FindLastHorizontalSymetricContrast(pointB, img, baseColor, shapePoints, tolerance, false)),
               },
                SearchWay.Bottom => new List<SearchWayDistance>
               {
                   new SearchWayDistance(SearchWay.BottomRight, pointA.FindSymetricConstrastedCorner(pointB, img, baseColor, shapePoints, tolerance, false, false)),
                   new SearchWayDistance(SearchWay.Left,  pointA.FindLastHorizontalSymetricContrast(pointB, img, baseColor, shapePoints, tolerance, true)),
               },
                SearchWay.Top => new List<SearchWayDistance>
               {
                   new SearchWayDistance(SearchWay.TopRight, pointA.FindSymetricConstrastedCorner(pointB, img, baseColor, shapePoints, tolerance, true, false)),
                   new SearchWayDistance(SearchWay.Left,  pointA.FindLastHorizontalSymetricContrast(pointB, img, baseColor, shapePoints, tolerance, true)),
                   new SearchWayDistance(SearchWay.Right,  pointA.FindLastHorizontalSymetricContrast(pointB, img, baseColor, shapePoints, tolerance, false)),
               },
            }).Where(x => x.Points.Any()).ToList();

            var bestMove = distances.MaxBy(x => x.Points.Count());
            return bestMove != default
                ? new PossibleMoveWay(bestMove.Way, bestMove.Points.Select(x => new PixelPoint(x.Item1, x.Item2)).ToArray())
                : default;
        }

        internal List<PixelPoint> FindEndSegmentPoint(PixelPoint point)
        {
            var points = new List<PixelPoint>();

            var segmentWidth = 1;
            while (point.X + segmentWidth < img.Width - 1 && img.GetPixel(point.X + segmentWidth, point.Y).Color.IsContrast(baseColor))
            {
                points.Add(new PixelPoint((point.X + segmentWidth), point.Y));
                segmentWidth++;
            }

            while (points.Count < 2)
                points.Add(point.Clone());

            points.Add(point.Clone());

            return points;
        }

        internal List<PixelPoint> FindStartPoints(PixelPoint point)
        {
            var points = new List<PixelPoint>();

            var segmentWidth = 1;
            while (point.X + segmentWidth < img.Width - 1 && img.GetPixel(point.X + segmentWidth + 1, point.Y).Color.IsContrast(baseColor))
                segmentWidth++;

            var middlePoint = new PixelPoint(point.X + segmentWidth / 2, point.Y);
            points.Add(middlePoint);

            points.Add(segmentWidth % 2 == 0
                ? middlePoint.Clone()
                : new PixelPoint(middlePoint.X + 1, middlePoint.Y));

            return points;
        }

        internal Shape[] DetectShapes(List<PixelPoint> currentShapePoints, SearchWay previousWay)
        {
            var res = new List<Shape>();
            var pointA = currentShapePoints[currentShapePoints.Count - 2].Clone();
            var pointB = currentShapePoints[currentShapePoints.Count - 1].Clone();

            var move = GetNextPossibleMove(pointA, pointB, baseColor, currentShapePoints, previousWay);
            if (move == default)
            {
                var completeShape = pointA.Distance(pointB) < tolerance;
                var shape = new Shape(currentShapePoints, completeShape);
                ////DEBUG
                //res.Add(shape);
                //return res.ToArray();
                ////DEBUG
                if (completeShape && shape.Width > minWidthBlock && shape.Height > minHeightBlock)
                {
                    res.Add(shape);
                    return res.ToArray();
                }
            }
            else
            {
                var newA = move.Points[move.Points.Count() - 2];
                var newB = move.Points[move.Points.Count() - 1];
                //Console.WriteLine($"[{pointA} | {pointB}]\t[{move.Way}]\t [{newA}|{newB}]{move.Points.Length}");
                //Console.ReadLine();
                var completeShape = newA.Distance(newB) < tolerance;
                if (completeShape)
                {
                    //Console.WriteLine("COMPLETED");
                    var subShapePoints = new List<PixelPoint>(currentShapePoints);
                    subShapePoints.AddRange(move.Points);
                    var shape = new Shape(subShapePoints, completeShape);
                    if (shape.Width > minWidthBlock && shape.Height > minHeightBlock)
                    {
                        res.Add(shape);
                        return res.ToArray();
                    }
                }

                var tmpPath = new List<PixelPoint>(currentShapePoints);
                tmpPath.AddRange(move.Points);
                res.AddRange(DetectShapes(tmpPath, move.Way));
            }

            return res.ToArray();
            //return res.Where(x => !res.Any(y => y.Area() > x.Area() && y.IntersectsWith(x))).ToArray();
        }
    }
}
