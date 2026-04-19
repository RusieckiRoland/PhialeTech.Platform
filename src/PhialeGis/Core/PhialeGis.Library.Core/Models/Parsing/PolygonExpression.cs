namespace PhialeGis.Library.Core.Models.Parsing
{
    internal class PolygonExpression : IExpression
    {
        public void Interpret(Context context)
        {
            var ringExpression = new CoordsExpression();

            context.NextToken();
            while (context.NotEndOfExpression)
            {
                switch (context.CurrentToken.Type)
                {
                    case TokenType.Mcoord:
                        context.CoordType = CoordType.Flat;
                        break;

                    case TokenType.Zcoord:
                        context.CoordType = CoordType.Elevated;
                        break;

                    case TokenType.Zmcoord:
                        context.CoordType = CoordType.MeasuredElevated;
                        break;

                    case TokenType.LeftParen:
                        context.Parts.Add(context.Points.Count);
                        ringExpression.Interpret(context);
                        break;
                }
                context.NextToken();
            }
        }
    }
}