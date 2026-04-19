using System;

namespace PhialeTech.PdfViewer.Abstractions
{
    public sealed class PdfViewerZoomChangedEventArgs : EventArgs
    {
        public PdfViewerZoomChangedEventArgs(double scaleFactor, string scaleValue)
        {
            ScaleFactor = scaleFactor;
            ScaleValue = scaleValue ?? string.Empty;
        }

        public double ScaleFactor { get; }

        public string ScaleValue { get; }
    }
}
