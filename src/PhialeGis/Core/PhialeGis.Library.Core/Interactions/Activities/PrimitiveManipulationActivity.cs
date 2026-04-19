using System;
using System.Collections.Generic;
using System.Text;

namespace PhialeGis.Library.Core.Interactions.Activities
{
    internal class PrimitiveManipulationActivity : DefaultActivity
    {
        protected int TouchingCount { get; set; }

        public PrimitiveManipulationActivity StartActivity { get; set; }

        public PrimitiveManipulationActivity(PrimitiveManipulationActivity activity = null)
        {
            TouchingCount = 0;
            StartActivity = null;
        }
    }
}