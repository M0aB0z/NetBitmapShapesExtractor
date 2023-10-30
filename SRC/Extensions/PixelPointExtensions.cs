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
        int tries = 0, distanceWithContrast = 0;
        var matches = new List<(int, int)>();
        for (
            int currentXA = wayLeft ? pointA.X - 1 : pointA.X + 1,
            currentXB = wayLeft ? pointB.X + 1 : pointB.X - 1
            ; (wayLeft ? currentXA > 0 : currentXA < img.Width) && tries < tolerance;
            currentXA += (wayLeft ? -1 : 1), currentXB += (wayLeft ? 1 : -1))
        {
            if (new PixelPoint(currentXA, pointA.Y).InShape(currentShape))
                return matches;
            if (
                   img.IsContrastPoint(currentXA, pointA.Y, baseColor)
                && img.IsContrastPoint(currentXB, pointB.Y, baseColor)
                && (!img.IsContrastPoint(currentXA, pointA.Y - 1, baseColor)
                || img.IsContrastPoint(currentXA, pointA.Y + 1, baseColor)
                || img.IsContrastPoint(currentXA + (wayLeft ? -2 : 2), pointA.Y - 1, baseColor)
                || img.IsContrastPoint(currentXA + (wayLeft ? -2 : 2), pointA.Y + 1, baseColor)
                )
                && (!img.IsContrastPoint(currentXB, pointB.Y - 1, baseColor)
                || img.IsContrastPoint(currentXB, pointB.Y + 1, baseColor)
                || img.IsContrastPoint(currentXB + (wayLeft ? -2 : 2), pointB.Y - 1, baseColor)
                || img.IsContrastPoint(currentXB + (wayLeft ? -2 : 2), pointB.Y + 1, baseColor)
                )
                )
            {
                matches.Add((currentXA, pointA.Y));
                matches.Add((currentXB, pointB.Y));
                distanceWithContrast++;
                if (distanceWithContrast > tolerance)
                    tries = 0;
            }
            else
                tries++;
        }
        return matches;
    }
    internal static IEnumerable<(int, int)> FindLastVerticalSymetricContrast(this PixelPoint pointA,
        PixelPoint pointB,
        Picture img,
        PixelColor baseColor,
        IEnumerable<PixelPoint> currentShape, int tolerance, bool wayTop)
    {
        int tries = 0, distanceWithContrast = 0;
        var matches = new List<(int, int)>();
        for (int currentY = wayTop ? pointA.Y - 1 : pointA.Y + 1; (wayTop ? currentY > 0 : currentY < img.Height) && tries < tolerance; currentY += (wayTop ? -1 : 1))
        {
            if (new PixelPoint(pointA.X, currentY).InShape(currentShape))
                return matches;
            if (
                   img.IsContrastPoint(pointA.X, currentY, baseColor)
                && img.IsContrastPoint(pointB.X, currentY, baseColor)
                && (!img.IsContrastPoint(pointA.X - 1, currentY, baseColor)
                    || !img.IsContrastPoint(pointA.X + 1, currentY, baseColor)
                    || !img.IsContrastPoint(pointA.X, currentY + (wayTop ? 2 : -2), baseColor) // round corner
                    )
                && (!img.IsContrastPoint(pointB.X - 1, currentY, baseColor)
                    || !img.IsContrastPoint(pointB.X + 1, currentY, baseColor)
                    || !img.IsContrastPoint(pointB.X, currentY + (wayTop ? 2 : -2), baseColor) // round corner
                    ))
            {
                matches.Add((pointA.X, currentY));
                matches.Add((pointB.X, currentY));
                distanceWithContrast++;
                if (distanceWithContrast > tolerance)
                    tries = 0;
            }
            else
                tries++;
        }
        return matches;
    }
}
