namespace PhialeGis.Library.Core.Models.Parsing
{
    internal class CoordsExpression : IExpression
    {
        public void Interpret(Context context)
        {
            var sigleCoord = new CoordExpresion();
            context.NextToken();
            while (context.NotEndOfExpression)
            {
                switch (context.CurrentToken.Type)
                {
                    case TokenType.Comma:
                        break;

                    case TokenType.Number:
                        sigleCoord.Interpret(context);
                        break;

                    default:
                        break;
                }
                context.NextToken();
            }
        }
    }
}