using System;
using System.Globalization;
using Antlr4.Runtime.Misc;
using PhialeGis.Library.Dsl.Requests;

namespace PhialeGis.Library.Dsl.Visitors
{
    /// <summary>
    /// Converts PhialeDSL parse tree nodes into request DTOs consumed by the app layer.
    /// C# 7.3 compatible (no modern pattern/range features).
    /// </summary>
    public sealed class PhialeVisitor : PhialeDslParserBaseVisitor<object>
    {
        /// <summary>
        /// Visits the ZoomIn command context and creates a corresponding request DTO.
        /// </summary>
        /// <param name="context">The parse tree context for the ZoomIn command.</param>
        /// <returns>A ZoomInRequest object representing the command.</returns>
        public override object VisitZoomIn([NotNull] PhialeDslParser.ZoomInContext context)
        {
            return new ZoomInRequest();
        }

        /// <summary>
        /// Visits the ZoomOut command context and creates a corresponding request DTO.
        /// </summary>
        /// <param name="context">The parse tree context for the ZoomOut command.</param>
        /// <returns>A ZoomOutRequest object representing the command.</returns>
        public override object VisitZoomOut([NotNull] PhialeDslParser.ZoomOutContext context)
        {
            return new ZoomOutRequest();
        }

        /// <summary>
        /// Visits the Zoom command context, extracts the numeric factor, and creates a corresponding request DTO.
        /// </summary>
        /// <param name="context">The parse tree context for the Zoom command, including the factor.</param>
        /// <returns>A ZoomRequest object with the parsed factor.</returns>
        public override object VisitZoom([NotNull] PhialeDslParser.ZoomContext context)
        {
            // Retrieve the NUMBER token from the context
            var numTok = context.GetToken(PhialeDslParser.NUMBER, 0);
            if (numTok == null)
            {
                throw new InvalidOperationException("ZOOM command requires a numeric factor.");
            }

            // Parse the numeric factor using invariant culture for consistency
            double factor;
            if (!double.TryParse(numTok.GetText(), NumberStyles.Any, CultureInfo.InvariantCulture, out factor))
            {
                throw new FormatException("Invalid numeric factor in ZOOM command.");
            }

            // Additional validation could be added here, e.g., factor > 0

            return new ZoomRequest(factor);
        }

        public override object VisitAddLineStart([NotNull] PhialeDslParser.AddLineStartContext context)
        {
            return new AddLinestringRequest();
        }
    }
}
