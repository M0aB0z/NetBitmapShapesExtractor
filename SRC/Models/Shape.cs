namespace ShapesDetector.Models;

public class Shape
{
    public int X { get; private set; }
    public int Y { get; private set; }
    public int Width { get; private set; }
    public int Height { get; private set; }
    public bool Completed { get; private set; }

    public bool Contains(PixelPoint point)
    {
        return point.X >= X && point.X <= X + Width
            && point.Y >= Y && point.Y <= Y + Height;
    }

    public Shape(IEnumerable<PixelPoint> pixels, bool completed)
    {
        var minX = pixels.Min(x => x.X);
        var maxX = pixels.Max(x => x.X);
        var minY = pixels.Min(x => x.Y);
        var maxY = pixels.Max(x => x.Y);

        X = minX;
        Y = minY;

        Width = Math.Max(maxX - minX, 1);
        Height = Math.Max(maxY - minY, 1);

        Completed = completed;
    }

    public override string ToString() => $"({X},{Y}) ({Width},{Height})";
}
