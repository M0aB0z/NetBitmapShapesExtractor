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

        private PossibleMoveWay[] GetNextPossibleMoves(PixelPoint pointA, PixelPoint pointB, PixelColor baseColor, IEnumerable<PixelPoint> shapePoints)
        {
            var res = new List<PossibleMoveWay>();

            var distances = new List<SearchWayDistance>
            {
                new SearchWayDistance(SearchWay.Bottom, pointA.FindLastVerticalSymetricContrast(pointB, img, baseColor, shapePoints, tolerance, false)),
                new SearchWayDistance(SearchWay.Top, pointA.FindLastVerticalSymetricContrast(pointB, img, baseColor, shapePoints, tolerance, true)),
                new SearchWayDistance(SearchWay.Left, pointA.FindLastHorizontalSymetricContrast(pointB, img, baseColor, shapePoints, tolerance, true)),
                new SearchWayDistance(SearchWay.Right, pointA.FindLastHorizontalSymetricContrast(pointB, img, baseColor, shapePoints, tolerance, false))
            }.Where(x => x.Points.Any()).ToList();

            return distances.Where(x => x.Points.Any())
                .OrderBy(x => x.Points.Count())
                .Select(x => new PossibleMoveWay(x.Way, x.Points.Select(x => new PixelPoint(x.Item1, x.Item2)).ToArray()))
                .ToArray();
        }

        internal List<PixelPoint> FindEndSegmentPoint(PixelPoint point)
        {
            var points = new List<PixelPoint>();

            var segmentWidth = 1;
            while (point.X + segmentWidth < img.Width - 1 && img.GetPixel(point.X + segmentWidth, point.Y).Color.IsContrast(baseColor))
            {
                segmentWidth++;
                points.Add(new PixelPoint((point.X + segmentWidth), point.Y));
            }

            while(points.Count < 2)
                points.Add(point.Clone());

            points.Add(point.Clone());

            return points;
        }

        //todo: ensure borders segments
        internal Shape[] DetectShapes(List<PixelPoint> currentShapePoints)
        {
            var res = new List<Shape>();
            var pointA = currentShapePoints[currentShapePoints.Count - 2].Clone();
            var pointB = currentShapePoints[currentShapePoints.Count - 1].Clone();

            var possibleMoves = GetNextPossibleMoves(pointA, pointB, baseColor, currentShapePoints);
            Console.WriteLine($"[{string.Join(",", possibleMoves.Select(x => x.Way))}]\t");
            foreach (var move in possibleMoves)
            {
                Console.WriteLine($"[{move.Way}]\t {move.Points.Length}");
                //Console.ReadLine();
                var completeShape = move.Points[move.Points.Count() - 2].Distance(move.Points[move.Points.Count() - 1]) < tolerance;
                if (completeShape)
                {
                    //Console.WriteLine("COMPLETED");
                    var shape = new Shape(move.Points, true);
                    if (shape.Width > minWidthBlock && shape.Height > minHeightBlock)
                    {
                        res.Add(shape);
                        return res.ToArray();
                    }
                }

                var tmpPath = new List<PixelPoint>(currentShapePoints);
                tmpPath.AddRange(move.Points);
                res.AddRange(DetectShapes(tmpPath));
            }

            return res.ToArray();
            //return res.Where(x => !res.Any(y => y.Area() > x.Area() && y.IntersectsWith(x))).ToArray();
        }
    }
}
