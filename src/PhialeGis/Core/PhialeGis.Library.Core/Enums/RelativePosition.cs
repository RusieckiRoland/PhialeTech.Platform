namespace PhialeGis.Library.Core.Enums
{
    internal enum RelativePosition
    {
        None = 0,
        Left = 1 << 0,   // 1
        Bottom = 1 << 1, // 2
        Right = 1 << 2,  // 4
        Top = 1 << 3,    // 8
        InFront = 1 << 4,// 16
        Behind = 1 << 5  // 32
    }
}