using PhialeGis.Library.Core.Models.Geometry;
using System;

namespace PhialeGis.Library.Core.Graphics
{
    internal class RedrawTriggerBase
    {
        private readonly EventHandler<RedrawEventArgs> _redrawRequested;

        public RedrawTriggerBase(EventHandler<RedrawEventArgs> redrawRequested)
        {
            _redrawRequested = redrawRequested;
        }

        public void TriggerRedraw(PhRect? rect)
        {
            var args = new RedrawEventArgs(rect);
            _redrawRequested?.Invoke(this, args);
        }

        public void TriggerRedraw()
        {
            var args = new RedrawEventArgs();
            _redrawRequested?.Invoke(this, args);
        }
    }
}