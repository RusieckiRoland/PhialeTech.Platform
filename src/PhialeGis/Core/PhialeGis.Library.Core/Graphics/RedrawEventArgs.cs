using PhialeGis.Library.Core.Models.Geometry;
using System;

namespace PhialeGis.Library.Core.Graphics
{
    internal class RedrawEventArgs : EventArgs
    {
        public RedrawEventArgs(PhRect? phRect)
        {
            RectToRedraw = phRect;
        }

        public RedrawEventArgs()
        {
        }

        public PhRect? RectToRedraw { get; set; }
    }
}