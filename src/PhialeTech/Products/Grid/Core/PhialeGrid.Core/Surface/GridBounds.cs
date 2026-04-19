namespace PhialeGrid.Core.Surface
{
    /// <summary>
    /// Reprezentacja prostokąta w platonic coordinate space.
    /// Używana zamiast System.Windows.Rect bo to Core nie może zależeć od WPF.
    /// </summary>
    public struct GridBounds
    {
        public GridBounds(double x, double y, double width, double height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }

        public double Left => X;
        public double Top => Y;
        public double Right => X + Width;
        public double Bottom => Y + Height;

        /// <summary>
        /// Czy punkt jest wewnątrz tego prostokąta.
        /// </summary>
        public bool Contains(double px, double py)
        {
            return px >= X && px <= Right && py >= Y && py <= Bottom;
        }

        public static GridBounds Empty => new GridBounds(0, 0, 0, 0);

        public override string ToString() => $"({X}, {Y}, {Width}x{Height})";
    }
}
