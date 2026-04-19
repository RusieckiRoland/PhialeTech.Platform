using System;

namespace PhialeGis.Library.Abstractions.Styling
{
    public sealed class StylePreviewImage
    {
        public int WidthPx { get; set; }

        public int HeightPx { get; set; }

        public byte[] PngBytes { get; set; } = Array.Empty<byte>();
    }
}
