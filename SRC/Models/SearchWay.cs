namespace ShapesDetector.Models;

internal enum SearchWay
{
    Top,
    Right,
    Bottom,
    Left,
}

internal record SearchWayDistance(SearchWay Way, IEnumerable<(int, int)> Points)
{
    public PixelPoint Transform(PixelPoint p)
    {
        var distance = Points.Count() / 2; // Symetric points

        return Way switch
        {
            SearchWay.Top => new PixelPoint(p.X, p.Y - distance),
            SearchWay.Right => new PixelPoint(p.X + distance, p.Y),
            SearchWay.Bottom => new PixelPoint(p.X, p.Y + distance),
            SearchWay.Left => new PixelPoint(p.X - distance, p.Y),
            _ => throw new NotImplementedException(),
        };
    }
}

internal record PossibleMoveWay(SearchWay Way, PixelPoint[] Points);
