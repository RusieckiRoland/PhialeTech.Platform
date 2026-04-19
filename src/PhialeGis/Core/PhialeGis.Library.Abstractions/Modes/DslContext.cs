using System;
using System.Collections.Generic;
using System.Text;

namespace PhialeGis.Library.Abstractions.Modes
{
    public sealed class DslContext
    {
        public DslMode Mode { get; set; } = DslMode.Normal;
        public string ActionName { get; set; }   
        public Guid ActionId { get; set; }
        public object TargetDraw { get; set; }
        public string LanguageId { get; set; } = "en";
    }
}
