using ShapesDetector.Models;

namespace ShapesDetector;

public static class ShapesExtractor
{
    private static PixelPoint SearchLeftBottomPoint(this PixelPoint point, Picture img, PixelColor baseColor, IEnumerable<PixelPoint> currentShape, int tolerance)
    {
        int x = point.X, y = point.Y;
        for (int currentY = y; currentY <= y + tolerance; currentY++)
            for (int currentX = x, triesX = 0; triesX <= tolerance; currentX--, triesX++)
                if ((img.GetPixel(currentX, currentY)?.Color?.IsContrast(baseColor) ?? false) && new PixelPoint(currentX, currentY).NotInShape(currentShape))
                    return new PixelPoint(currentX, currentY);
        return null;
    }

    private static PixelPoint SearchRightBottomPoint(this PixelPoint point, Picture img, PixelColor baseColor, IEnumerable<PixelPoint> currentShape, int tolerance)
    {
        int x = point.X, y = point.Y;
        for (int currentY = y; currentY <= y + tolerance; currentY++)
            for (int currentX = x, triesX = 0; triesX <= tolerance; currentX++, triesX++)
                if ((img.GetPixel(currentX, currentY)?.Color?.IsContrast(baseColor) ?? false) && new PixelPoint(currentX, currentY).NotInShape(currentShape))
                    return new PixelPoint(currentX, currentY);
        return null;
    }

    private static PixelPoint SearchLeftTopPoint(this PixelPoint point, Picture img, PixelColor baseColor, IEnumerable<PixelPoint> currentShape, int tolerance)
    {
        int x = point.X, y = point.Y;
        for (int currentY = y; currentY >= y - tolerance; currentY--)
            for (int currentX = x, triesX = 0; triesX <= tolerance; currentX--, triesX++)
                if ((img.GetPixel(currentX, currentY)?.Color?.IsContrast(baseColor) ?? false) && new PixelPoint(currentX, currentY).NotInShape(currentShape))
                    return new PixelPoint(currentX, currentY);
        return null;
    }

    private static PixelPoint SearchRightTopPoint(this PixelPoint point, Picture img, PixelColor baseColor, IEnumerable<PixelPoint> currentShape, int tolerance)
    {
        int x = point.X, y = point.Y;
        for (int currentY = y; currentY >= y - tolerance; currentY--)
            for (int currentX = x, triesX = 0; triesX <= tolerance; currentX++, triesX++)
                if ((img.GetPixel(currentX, currentY)?.Color?.IsContrast(baseColor) ?? false) && new PixelPoint(currentX, currentY).NotInShape(currentShape))
                    return new PixelPoint(currentX, currentY);
        return null;
    }

    private static Shape[] DetectShapes(this PixelPoint point, Picture img, PixelColor baseColor, int tolerance, int minHeightBlock, int minWidthBlock)
    {
        var res = new List<Shape>();
        var topWidth = 1;
        while (point.X + topWidth < img.Width - 1 && img.GetPixel(point.X + topWidth, point.Y).Color.IsContrast(baseColor))
            topWidth++;

        var pointA = new PixelPoint(point.X + topWidth / 2, point.Y);
        var pointB = new PixelPoint(pointA.X, point.Y);
        var shapePoints = new List<PixelPoint> { new PixelPoint(pointA.X, point.Y) };
        var stop = false;
        var completeShape = false;
        var registerSymetricPoints = (PixelPoint a, PixelPoint b) =>
        {
            shapePoints.Add(a);
            shapePoints.Add(b);
            if (Math.Abs(a.X - b.X) < tolerance - 1 && Math.Abs(a.Y - b.Y) < tolerance - 1)
            {
                stop = true;
                completeShape = true;
            }
            else
            {
                pointA = a;
                pointB = b;
            }
        };
        while (!stop && !completeShape)
        {
            var leftBottomPointA = pointA.SearchLeftBottomPoint(img, baseColor, shapePoints, tolerance);
            if (leftBottomPointA != null)
            {
                var rightBottomPointB = pointB.SearchRightBottomPoint(img, baseColor, shapePoints, tolerance);
                if (rightBottomPointB != null && Math.Abs(leftBottomPointA.Y - rightBottomPointB.Y) < tolerance)
                {
                    registerSymetricPoints(leftBottomPointA, rightBottomPointB);
                    continue;
                }
            }
            var leftTopPointA = pointA.SearchLeftTopPoint(img, baseColor, shapePoints, tolerance); ;
            if (leftTopPointA != null)
            {
                var rightTopPointB = pointB.SearchRightTopPoint(img, baseColor, shapePoints, tolerance);
                if (rightTopPointB != null && Math.Abs(leftTopPointA.Y - rightTopPointB.Y) < tolerance)
                {
                    registerSymetricPoints(leftTopPointA, rightTopPointB);
                    continue;
                }
            }

            var rightBottomPointA = pointA.SearchRightBottomPoint(img, baseColor, shapePoints, tolerance);
            if (rightBottomPointA != null)
            {
                var leftBottomPointB = pointB.SearchLeftBottomPoint(img, baseColor, shapePoints, tolerance);
                if (leftBottomPointB != null && Math.Abs(rightBottomPointA.Y - leftBottomPointB.Y) < tolerance)
                {
                    registerSymetricPoints(rightBottomPointA, leftBottomPointB);
                    continue;
                }
            }

            var rightTopPointA = pointA.SearchRightTopPoint(img, baseColor, shapePoints, tolerance); ;
            if (rightTopPointA != null)
            {
                var leftTopPointB = pointB.SearchLeftTopPoint(img, baseColor, shapePoints, tolerance);
                if (leftTopPointB != null && Math.Abs(rightTopPointA.Y - leftTopPointB.Y) < tolerance)
                {
                    registerSymetricPoints(rightTopPointA, leftTopPointB);
                    continue;
                }
            }

            stop = true; // no more symetric points found
        }


        var shape = new Shape(shapePoints);
        if (shape.Width >= minWidthBlock && shape.Height >= minHeightBlock)
        {
            res.Add(shape);
            return res.ToArray();
        }
        return res.ToArray();
        //return res.Where(x => !res.Any(y => y.Area() > x.Area() && y.IntersectsWith(x))).ToArray();
    }

    public static Shape[] ExtractShapes(this Picture img, int tolerance = 3, int minHeightBlock = 15, int minWidthBlock = 40)
    {
        var shapes = new List<Shape>();
        var baseColor = img.GetBackgroundColor();
        for (int currentTop = 0; currentTop < img.Height; currentTop++)
        {
            for (int currentLeft = 0; currentLeft < img.Width; currentLeft++)
            {
                // if pixel is in a known shape, skip it

                var pixel = img.GetPixel(currentLeft, currentTop);
                if (pixel.Color.IsContrast(baseColor) && img.IsBorder(currentLeft, currentTop, baseColor)) // Contrast detected
                {
                    shapes.AddRange(pixel.Point.DetectShapes(img, baseColor, tolerance, minHeightBlock, minWidthBlock));
                    //return shapes.ToArray();
                }
            }
        }
        return shapes.ToArray();
    }

}
