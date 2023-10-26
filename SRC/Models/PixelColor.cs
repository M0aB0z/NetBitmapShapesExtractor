namespace ShapesDetector.Models;

public class PixelColor
{
    public int R { get; private set; }
    public int G { get; private set; }
    public int B { get; private set; }

    private double Compare(PixelColor other)
    {
        long rmean = (R + other.R) / 2;
        long r = R - other.R;
        long g = G - other.G;
        long b = B - other.B;
        var diff = Math.Sqrt(((512 + rmean) * r * r >> 8) + 4 * g * g + ((767 - rmean) * b * b >> 8));

        return diff;
    }

    public bool IsContrast(PixelColor color) => Compare(color) > 50;


    public PixelColor(int r, int g, int b)
    {
        R = r;
        G = g;
        B = b;
    }
}
