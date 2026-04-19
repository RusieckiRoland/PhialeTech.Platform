using System;

namespace PhialeGis.Library.Core.Enums
{
    [Flags]
    internal enum LineRelations
    {
        None = 0,
        Parallel = 1,
        DivToRight = 2,
        DivToLeft = 4,
        OffDivStart = 8,
        AtDivStart = 16,
        OffDivEnd = 32,
        AtDivEnd = 64,
        BetweenDiv = 128,
        ParToRight = 256,
        ParToLeft = 512,
        OffParStart = 1024,
        AtParStart = 2048,
        OffParEnd = 4096,
        AtParEnd = 8192,
        BetweenPar = 16384
    }
}