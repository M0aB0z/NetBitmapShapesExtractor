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

    public bool InShape(IEnumerable<PixelPoint> points)
    => points.Contains(this, PixelPointComparer.Instance);

    public double Distance(PixelPoint other)
        => Math.Sqrt(Math.Pow(other.X - X, 2) + Math.Pow(other.Y - Y, 2));

    public PixelPoint Clone() => new(X, Y);

    public bool Equals(PixelPoint other)
    => other?.X == X && other?.Y == Y;

    public override string ToString() => $"({X},{Y})";
}

public class PixelPointComparer : IEqualityComparer<PixelPoint>
{
    public static readonly PixelPointComparer Instance = new PixelPointComparer();

    public bool Equals(PixelPoint a, PixelPoint b)
    => a?.X == b?.X && a?.Y == b?.Y;
    public int GetHashCode([DisallowNull] PixelPoint obj)
    => obj.X.GetHashCode() ^ obj.Y.GetHashCode();
}


