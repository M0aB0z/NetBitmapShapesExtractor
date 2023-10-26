using System.Diagnostics.CodeAnalysis;

namespace ShapesDetector.Models;


public class Pixel : IEqualityComparer<Pixel>
{
    public PixelPoint Point { get; private set; }
    public PixelColor Color { get; private set; }

    public Pixel(int x, int y, int r, int g, int b)
    {
        Point = new PixelPoint(x, y);
        Color = new PixelColor(r, g, b);
    }

    public bool Equals(Pixel a, Pixel b)
    => a?.Point?.X == b?.Point?.X 
    && a?.Point?.Y == b?.Point?.Y
    && a?.Color?.R == b?.Color?.R
    && a?.Color?.G == b?.Color?.G
    && a?.Color?.B == b?.Color?.B;

    public int GetHashCode([DisallowNull] Pixel obj)
    => obj.Point.X.GetHashCode() ^ obj.Point.Y.GetHashCode()
      ^obj.Color.R.GetHashCode() ^ obj.Color.R.GetHashCode()
      ^obj.Color.G.GetHashCode() ^ obj.Color.G.GetHashCode()
      ^obj.Color.B.GetHashCode() ^ obj.Color.B.GetHashCode();
}
