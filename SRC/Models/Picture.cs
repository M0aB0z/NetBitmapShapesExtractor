using System.Drawing;

namespace ShapesDetector.Models;

public abstract class Picture
{
    public abstract int Width { get; }
    public abstract int Height { get; }
    public abstract Pixel GetPixel(int x, int y);
    public abstract void Save(string path);

    public bool IsBorder(int x, int y, PixelColor color)
    {
        for (var currentX = x - 1; currentX <= x + 1 && x > 0 && x < Width; currentX++)
            for (var currentY = y - 1; currentY <= y + 1 && y > 0 && y < Height; currentY++)
            {
                if ((currentX != x || currentY != y) && !GetPixel(currentX, currentY).Color.IsContrast(color))
                    return true;
            }
        return false;
    }

    public PixelColor GetBackgroundColor()
    {
        var colors = new List<PixelColor> { 
            GetPixel(0, 0).Color, GetPixel(0, Height - 1).Color, GetPixel(Width - 1, 0).Color, GetPixel(Width - 1, Height - 1).Color };
        return colors.GroupBy(x => x).OrderByDescending(y => y.Count()).First().Key;
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
