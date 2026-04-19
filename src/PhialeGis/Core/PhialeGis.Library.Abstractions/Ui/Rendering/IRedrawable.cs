using System;
using System.Collections.Generic;
using System.Text;

namespace PhialeGis.Library.Abstractions.Ui.Rendering
{
    public interface IRedrawable
    {
        void Invalidate();
    }
}
