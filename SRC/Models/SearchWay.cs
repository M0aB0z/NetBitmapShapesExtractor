namespace ShapesDetector.Models;

internal enum SearchWay
{
    Top,
    Right,
    Bottom,
    Left,
    TopRight,
    TopLeft,
    BottomRight,
    BottomLeft
}

internal record SearchWayDistance(SearchWay Way, IEnumerable<(int, int)> Points)
{

}

internal record PossibleMoveWay(SearchWay Way, PixelPoint[] Points);
