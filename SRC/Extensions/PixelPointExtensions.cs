using ShapesDetector.Models;

namespace ShapesDetector.Extensions;

internal static class PixelPointExtensions
{
    internal static IEnumerable<(int, int)> FindSymetricConstrastedCorner(this PixelPoint pointA,
PixelPoint pointB,
Picture img,
PixelColor baseColor, IEnumerable<PixelPoint> currentShape, int tolerance, bool wayTop, bool wayLeft)
    {
        var found = false;
        var matches = new List<(int, int)>();
        var possiblesA = new List<PixelPoint>();
        for (int decalX = 0, xA = pointA.X, xB = pointB.X; decalX <= tolerance && !found; decalX++, xA += (wayLeft ? decalX * -1 : decalX), xB += (wayLeft ? decalX : decalX * -1))
        {
            for (int decalY = 0, y = pointA.Y; decalY <= tolerance && !found; decalY++, y += (wayTop ? decalY * -1 : decalY))
            {
                var testA = new PixelPoint(xA, y);
                if (!testA.InShape(currentShape) && img.IsKnownBorderPoint(testA))
                {
                    possiblesA.Add(testA);
                }
                var testB = new PixelPoint(xB, y);
                if (!testB.InShape(currentShape) && img.IsKnownBorderPoint(testB))
                {
                    var matchedA = possiblesA.FirstOrDefault(pt => Math.Abs(pt.Y - y) <= tolerance);
                    if (matchedA != null)
                    {
                        matches.Add((matchedA.X, matchedA.Y));
                        matches.Add((testB.X, testB.Y));
                        found = true;
                    }
                }
            }
        }
        //matches.AddRange(pointA.FindLastHorizontalSymetricContrast(pointB, img, baseColor, currentShape, tolerance, false));
        return matches.ToArray();
    }

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
                   img.IsKnownBorderPoint(currentXA, pointA.Y)
                && img.IsKnownBorderPoint(currentXB, pointB.Y)
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
                img.IsKnownBorderPoint(pointA.X, currentY) 
                && img.IsKnownBorderPoint(pointB.X, currentY)
              )
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
