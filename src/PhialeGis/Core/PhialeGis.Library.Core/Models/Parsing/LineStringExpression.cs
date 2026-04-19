namespace PhialeGis.Library.Core.Models.Parsing
{
    internal class LineStringExpression : IExpression
    {
        public void Interpret(Context context)
        {
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
                        var pointsExpr = new CoordsExpression();
                        pointsExpr.Interpret(context);
                        break;
                }
                context.NextToken();
            }
        }
    }
}