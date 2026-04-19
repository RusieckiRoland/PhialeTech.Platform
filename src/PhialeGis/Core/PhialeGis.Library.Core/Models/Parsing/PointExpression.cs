namespace PhialeGis.Library.Core.Models.Parsing
{
    internal class PointExpression : IExpression
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

                    case TokenType.Number:
                        var coordExpression = new CoordExpresion();
                        coordExpression.Interpret(context);
                        break;
                }
                context.NextToken();
            }
        }
    }
}