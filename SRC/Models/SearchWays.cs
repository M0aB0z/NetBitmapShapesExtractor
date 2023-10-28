namespace ShapesDetector.Models;

internal enum SearchWays
{
    Top,
    Right,
    Bottom,
    Left,
}

internal record SearchWayDistance(SearchWays Way, IEnumerable<(int, int)> Points)
{
    public PixelPoint Transform(PixelPoint p)
    {
        var distance = Points.Count() / 2; // Symetric points

        return Way switch
        {
            SearchWays.Top => new PixelPoint(p.X, p.Y - distance),
            SearchWays.Right => new PixelPoint(p.X + distance, p.Y),
            SearchWays.Bottom => new PixelPoint(p.X, p.Y + distance),
            SearchWays.Left => new PixelPoint(p.X - distance, p.Y),
            _ => throw new NotImplementedException(),
        };
    }
}
