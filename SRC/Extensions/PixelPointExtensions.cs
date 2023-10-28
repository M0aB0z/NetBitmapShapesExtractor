using ShapesDetector.Models;

namespace ShapesDetector.Extensions;

internal static class PixelPointExtensions
{
    internal static IEnumerable<(int, int)> FindLastHorizontalSymetricContrast(this PixelPoint pointA,
        PixelPoint pointB,
        Picture img,
        PixelColor baseColor,
        IEnumerable<PixelPoint> currentShape, int tolerance, bool wayLeft)
    {
        var tries = 0;
        var matches = new List<(int, int)>();
        for (int currentX = wayLeft ? pointA.X - 1 : pointA.X + 1; (wayLeft ? currentX > 0 : currentX < img.Width) && tries < tolerance; currentX += (wayLeft ? -1 : 1))
        {
            if (new PixelPoint(currentX, pointA.Y).InShape(currentShape))
                return matches;
            if (
                   img.IsContrastPoint(currentX, pointA.Y, baseColor)
                && img.IsContrastPoint(currentX, pointB.Y, baseColor))
            {
                matches.Add((currentX, pointA.Y));
                matches.Add((currentX, pointB.Y));
                tries--;
            }
            else
                tries++;
        }
        return matches;
    }
    internal static IEnumerable<(int,int)> FindLastVerticalSymetricContrast(this PixelPoint pointA,
        PixelPoint pointB,
        Picture img,
        PixelColor baseColor,
        IEnumerable<PixelPoint> currentShape, int tolerance, bool wayTop)
    {
        var tries = 0;
        var matches = new List<(int, int)>();
        for (int currentY = wayTop ? pointA.Y - 1 : pointA.Y + 1; (wayTop ? currentY > 0 : currentY < img.Height) && tries < tolerance; currentY += (wayTop ? -1 : 1))
        {
            if (new PixelPoint(pointA.X, currentY).InShape(currentShape))
                return matches;
            if (
                   img.IsContrastPoint(pointA.X, currentY, baseColor)
                && img.IsContrastPoint(pointB.X, currentY, baseColor))
            {
                matches.Add((pointA.X, currentY));
                matches.Add((pointB.X, currentY));
                tries--;
            }
            else
                tries++;
        }
        return matches;
    }
    internal static PixelPoint SearchDiagonalPoint(this PixelPoint point, Picture img, PixelColor baseColor, IEnumerable<PixelPoint> currentShape, int tolerance, bool wayTop, bool wayRight)
    {
        int x = point.X, y = point.Y;
        var isEndYLoop = (int currentY) => wayTop ? currentY >= y - tolerance : currentY <= y + tolerance;
        for (int currentY = y; isEndYLoop(currentY); currentY += (wayTop ? -1 : 1))
            for (int currentX = x, triesX = 0; triesX <= tolerance; currentX += (wayRight ? 1 : -1), triesX++)
                if ((img.GetPixel(currentX, currentY)?.Color?.IsContrast(baseColor) ?? false) && new PixelPoint(currentX, currentY).InShape(currentShape))
                    return new PixelPoint(currentX, currentY);
        return null;
    }
}
