using System;

namespace PhialeGis.Library.Core.Enums
{
    [Flags]
    internal enum ClipCodes
    {
        ccFirst = 1 << 0,       // 1
        ccSecond = 1 << 1,      // 2
        ccNotVisible = 1 << 2,  // 4
        ccVisible = 1 << 3      // 8
    }
}