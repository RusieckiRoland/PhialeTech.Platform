using System.Runtime.InteropServices;

namespace PhialeGis.Library.Core.Models.RenderSpace
{
    /// <summary>
    /// Lightweight rectangle structure for canvas (device) coordinate space.
    /// Fully Skia-independent and compatible with .NET Standard 2.0.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct CanvasRect
    {
        public float Left;
        public float Top;
        public float Right;
        public float Bottom;

        public CanvasRect(float left, float top, float right, float bottom)
        {
            Left = left;
            Top = top;
            Right = right;
            Bottom = bottom;
        }

        public float Width
        {
            get { return Right - Left; }
        }

        public float Height
        {
            get { return Bottom - Top; }
        }

        public bool IsEmpty
        {
            get { return Width == 0f && Height == 0f; }
        }

        public CanvasPoint Center
        {
            get
            {
                return new CanvasPoint(
                    Left + Width / 2f,
                    Top + Height / 2f
                );
            }
        }

        public override string ToString()
        {
            return "{Left=" + Left + ", Top=" + Top + ", Right=" + Right + ", Bottom=" + Bottom + "}";
        }
    }
}
