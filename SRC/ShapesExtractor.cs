using ShapesDetector.Core;
using ShapesDetector.Models;

namespace ShapesDetector;

public static class ShapesExtractor
{
    public static Shape[] ExtractShapes(this Picture img, int tolerance = 3, int minHeightBlock = 15, int minWidthBlock = 40)
    {
        var pictureManager = new PictureManager(img, tolerance, minHeightBlock, minWidthBlock);
        var shapes = new List<Shape>();
        var validPoints = new List<PixelPoint>();
        var baseColor = img.GetBackgroundColor();
        var borderPoints = new Dictionary<(int, int), Pixel>();
        for (int currentTop = 0; currentTop < img.Height; currentTop++)
        {
            for (int currentLeft = 0; currentLeft < img.Width; currentLeft++)
            {
                var pixel = img.GetPixel(currentLeft, currentTop);
                if (pixel.Color.IsContrast(baseColor))
                {
                    if (img.IsBorderPoint(currentLeft, currentTop, baseColor))
                        borderPoints.Add((currentLeft, currentTop), pixel);
                }
            }
        }
        img.SetBorderPoints(borderPoints);
        //return shapes.ToArray();
        var pointsPerLines = borderPoints.GroupBy(x => x.Key.Item2).ToDictionary(x => x.Key, x => x.Select(y => y.Value).ToList());
        foreach (var pixelsRow in pointsPerLines.OrderBy(x => x.Key))
        {
            foreach(var pixel in pixelsRow.Value)
            {
                // if pixel is in a known shape, skip it
                if (shapes.Any(shape => shape.Contains(pixel.Point)))
                    continue;

                var startPoints = pictureManager.FindStartPoints(pixel.Point);
                var newShape = pictureManager.DetectShape(startPoints, SearchWay.Bottom);
                if (newShape != default)
                {
                    shapes.Add(newShape);
                    //return shapes.ToArray();
                }
            }
        }
        return shapes.ToArray();
    }

}
