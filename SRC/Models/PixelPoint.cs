using System.Diagnostics.CodeAnalysis;

namespace ShapesDetector.Models;


public class PixelPoint : IEquatable<PixelPoint>
{
    public int X { get; private set; }
    public int Y { get; private set; }

    public PixelPoint(int x, int y)
    {
        X = x;
        Y = y;
    }

    public bool NotInShape(IEnumerable<PixelPoint> points)
    {
        var notInShape = !points.Contains(this, PixelPointComparer.Instance);
        return notInShape;
    }

    public double Distance(PixelPoint other)
        => Math.Sqrt(Math.Pow(other.X - X, 2) + Math.Pow(other.Y - Y, 2));

    public bool Equals(PixelPoint other)
    => other?.X == X && other?.Y == Y;
}

public class PixelPointComparer : IEqualityComparer<PixelPoint>
{
    public static readonly PixelPointComparer Instance = new PixelPointComparer();

    public bool Equals(PixelPoint a, PixelPoint b)
    => a?.X == b?.X && a?.Y == b?.Y;
    public int GetHashCode([DisallowNull] PixelPoint obj)
    => obj.X.GetHashCode() ^ obj.Y.GetHashCode();
}


