namespace PhialeGis.Library.Abstractions.Styling
{
    public enum FillStyleKind
    {
        Solid = 0,
        PatternTile = 1,
        Hatch = 2,
        Gradient = 3
    }

    public enum GradientDirection
    {
        LeftToRight = 0,
        TopToBottom = 1,
        DiagonalDown = 2,
        DiagonalUp = 3
    }

    public enum FillDirection
    {
        Horizontal = 0,
        Vertical = 1,
        Diagonal45 = 2,
        Diagonal135 = 3
    }
}
