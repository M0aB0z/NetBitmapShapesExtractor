using System.Drawing;

namespace ShapesDetector.Models;

public abstract class Picture
{
    public Dictionary<(int, int), Pixel> BorderPoints { get; private set; }

    public abstract int Width { get; }
    public abstract int Height { get; }
    public abstract Pixel GetPixel(int x, int y);
    public bool IsContrastPoint(int x, int y, PixelColor baseColor)
        => GetPixel(x, y)?.Color?.IsContrast(baseColor) ?? false;
    public abstract void Save(string path);

    public bool IsBorderPoint(int x, int y, PixelColor baseColor)
    {
        return (
                !IsContrastPoint(x - 1, y, baseColor) ||
                !IsContrastPoint(x + 1, y, baseColor) ||
                !IsContrastPoint(x, y - 1, baseColor) ||
                !IsContrastPoint(x, y + 1, baseColor));
    }

    public bool IsKnownBorderPoint(int x, int y) => BorderPoints.ContainsKey((x, y));
    public bool IsKnownBorderPoint(PixelPoint point) => BorderPoints.ContainsKey((point.X, point.Y));

    public PixelColor GetBackgroundColor()
    {
        var colors = new List<PixelColor> {
            GetPixel(0, 0).Color, GetPixel(0, Height - 1).Color, GetPixel(Width - 1, 0).Color, GetPixel(Width - 1, Height - 1).Color };
        return colors.GroupBy(x => x).OrderByDescending(y => y.Count()).First().Key;
    }

    internal void SetBorderPoints(Dictionary<(int, int), Pixel> borderPoints)
    {
        this.BorderPoints = borderPoints;
    }
}

public class BitmapPicture : Picture
{
    public readonly Bitmap bmp;
    public override int Width => bmp.Width;
    public override int Height => bmp.Height;

    public BitmapPicture(string path)
    {
        bmp = new Bitmap(path);
    }

    public override void Save(string path) => bmp.Save(path);
    public override Pixel GetPixel(int x, int y)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height)
            return null;

        var color = bmp.GetPixel(x, y);
        return new Pixel(x, y, color.R, color.G, color.B);
    }
}
