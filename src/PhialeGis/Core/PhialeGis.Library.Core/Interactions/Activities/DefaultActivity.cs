using PhialeGis.Library.Abstractions.Ui.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace PhialeGis.Library.Core.Interactions.Activities
{
    internal class DefaultActivity : BaseActivity
    {
        public override FsmBehavior Behavior => FsmBehavior.Idle;
    }
}