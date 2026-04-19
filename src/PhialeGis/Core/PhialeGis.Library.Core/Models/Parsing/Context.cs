using PhialeGis.Library.Core.Models.Geometry;
using System.Collections.Generic;

namespace PhialeGis.Library.Core.Models.Parsing
{
    internal class Context
    {
        internal WktType CurrentGeometryType { get; set; }
        internal CoordType CoordType { get; set; }
        internal List<TokenStruct> Tokens { get; }
        internal List<PhPoint> Points { get; set; } = new List<PhPoint>();
        internal List<int> Parts { get; set; } = new List<int>();
        internal int Position { get; set; } = 0;

        internal bool NotEndOfExpression => GetNotRightParentYet();

        internal Context(List<TokenStruct> tokens)
        {
            Tokens = tokens;
        }

        internal TokenStruct CurrentToken => Tokens[Position];

        internal TokenStruct? NextToken()
        {
            var testPositon = Position + 1;

            if (testPositon < Tokens.Count)
            {
                Position++;
                return Tokens[Position];
            }
            else
            {
                return null;
            }
        }

        internal bool HasMoreTokens()
        {
            return Position + 1 < Tokens.Count;
        }

        private bool GetNotRightParentYet()
        {
            return (this.CurrentToken.Type != TokenType.RightParen) && HasMoreTokens();
        }
    }
}