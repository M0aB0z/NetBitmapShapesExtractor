using System.Diagnostics.CodeAnalysis;

namespace ShapesDetector.Models;

public class Shape
{
    public int X { get; private set; }
    public int Y { get; private set; }
    public int Width { get; private set; }
    public int Height { get; private set; }
    public bool Valid { get; private set; }

    public bool Contains(PixelPoint point)
    {
        return point.X >= X && point.X <= X + Width
            && point.Y >= Y && point.Y <= Y + Height;
    }

    public Shape(List<PixelPoint> pixels, int tolerance, int minWidth, int minHeight)
    {
        var pointA = pixels[pixels.Count - 2].Clone();
        var pointB = pixels[pixels.Count - 1].Clone();

        var completeShape = pointA.Distance(pointB) < tolerance;

        var minX = pixels.Min(x => x.X);
        var maxX = pixels.Max(x => x.X);
        var minY = pixels.Min(x => x.Y);
        var maxY = pixels.Max(x => x.Y);

        X = minX;
        Y = minY;

        Width = Math.Max(maxX - minX, 1);
        Height = Math.Max(maxY - minY, 1);

        Valid = Width >= minWidth && Height >= minHeight && completeShape;
    }

    public override string ToString() => $"({X},{Y}) ({Width},{Height})";
}

public class ShapeComparer : IEqualityComparer<Shape>
{
    public static readonly ShapeComparer Instance = new ShapeComparer();

    public bool Equals(Shape a, Shape b)
    => a?.X == b?.X && a?.Y == b?.Y && a?.Width == b?.Width && a?.Height == b?.Height;
    public int GetHashCode([DisallowNull] Shape obj)
    => obj.X.GetHashCode() ^ obj.Y.GetHashCode() ^ obj.Width.GetHashCode() ^ obj.Height.GetHashCode();
}

