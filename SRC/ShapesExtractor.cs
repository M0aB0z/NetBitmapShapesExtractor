using ShapesDetector.Core;
using ShapesDetector.Models;

namespace ShapesDetector;

public static class ShapesExtractor
{


    public static Shape[] ExtractShapes(this Picture img, int tolerance = 3, int minHeightBlock = 15, int minWidthBlock = 40)
    {
        var pictureManager = new PictureManager(img, tolerance, minHeightBlock, minWidthBlock);
        var shapes = new List<Shape>();
        var baseColor = img.GetBackgroundColor();
        for (int currentTop = 0; currentTop < img.Height; currentTop++)
        {
            for (int currentLeft = 0; currentLeft < img.Width; currentLeft++)
            {
                // if pixel is in a known shape, skip it
                if (shapes.Any(shape => shape.Contains(new PixelPoint(currentLeft, currentTop))))
                    continue;

                var pixel = img.GetPixel(currentLeft, currentTop);
                if (pixel.Color.IsContrast(baseColor) && img.IsBorder(currentLeft, currentTop, baseColor)) // Contrast detected
                {
                    var endSegmentPoint = pictureManager.FindEndSegmentPoint(pixel.Point);
                    var newShapes = pictureManager.DetectShapes(new List<PixelPoint> { pixel.Point.Clone(), endSegmentPoint });
                    if (newShapes.Length > 0)
                    {
                        currentLeft += newShapes[0].Width;
                        shapes.AddRange(newShapes);
                        return shapes.ToArray();
                    }
                    //return shapes.ToArray();
                }
            }
        }
        return shapes.ToArray();
    }

}
