using PhialeGis.Library.Core.Models.Geometry;
using System.Globalization;

namespace PhialeGis.Library.Core.Models.Parsing
{
    internal class CoordExpresion : IExpression
    {
        public void Interpret(Context context)
        {
            var point = new PhPoint();

            point.X = ParseAndMove(ref context);
            point.Y = ParseAndMove(ref context, (context.CoordType != CoordType.Flat));

            if (context.CoordType == CoordType.Elevated)
            {
                point.Z = ParseAndMove(ref context, false);
            }
            else if (context.CoordType == CoordType.Measured)
            {
                point.M = ParseAndMove(ref context, false);
            }
            else if (context.CoordType == CoordType.MeasuredElevated)
            {
                point.Z = ParseAndMove(ref context);

                point.M = ParseAndMove(ref context, false);
            }
            context.Points.Add(point);
        }

        private double ParseAndMove(ref Context context, bool move = true)
        {
            double.TryParse(context.CurrentToken.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out double value);

            if (move)
            { context.NextToken(); }
            return value;
        }
    }
}