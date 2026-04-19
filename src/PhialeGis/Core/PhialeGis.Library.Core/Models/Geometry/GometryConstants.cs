namespace PhialeGis.Library.Core.Models.Geometry
{
    internal class GometryConstants
    {
        internal static PhMatrix IdentityMatrix = new PhMatrix(new double[,] { { 1, 0, 0 }, { 0, 1, 0 }, { 0, 0, 1 } });
        internal const double FuzzValue = 1.0E-8;
        internal const double DOUBLE_EPSILON = 1.0E-10;
        internal const double MinDrawLimit = 1.0E-2;
        internal const double MaxCoord = 1.0E+100;
        internal const double MinCoord = -1.0E+100;
        internal static readonly PhRect RecEmpty = new PhRect(0, 0, 0, 0);
        internal static readonly PhRect BasicRect = new PhRect(0, 0, 100, 100);

        internal static readonly PhRect InvalidRect =
        new PhRect(new PhPoint(MaxCoord, MaxCoord),
                   new PhPoint(MinCoord, MinCoord));
    }
}