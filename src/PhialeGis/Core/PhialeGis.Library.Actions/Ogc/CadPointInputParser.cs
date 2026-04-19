using System;
using System.Globalization;

namespace PhialeGis.Library.Actions.Ogc
{
    /// <summary>
    /// Parses CAD-style point input for line actions.
    /// </summary>
    public static class CadPointInputParser
    {
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        public static bool IsUndo(string t)
        {
            if (string.IsNullOrWhiteSpace(t)) return false;
            var token = t.Trim();

            return string.Equals(token, "UNDO", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(token, "U", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(token, "COFNIJ", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(token, "CONFIJ", StringComparison.OrdinalIgnoreCase);
        }

        public static bool TryParse(string input, bool hasLast, double lastX, double lastY, out CadPointResult result)
        {
            result = default(CadPointResult);
            if (string.IsNullOrWhiteSpace(input)) return false;

            var t = input.Trim();
            if (IsUndo(t)) return false;

            if (t[0] == '@')
            {
                if (!hasLast) return false;
                return TryParseRelative(t, lastX, lastY, out result);
            }

            if (t[0] == '<')
            {
                if (!hasLast) return false;
                return TryParsePolar(t, lastX, lastY, out result);
            }

            return TryParseAbsolute(t, out result);
        }

        private static bool TryParseAbsolute(string t, out CadPointResult result)
        {
            result = default(CadPointResult);
            var parts = SplitNumbers(t);
            if (parts == null || parts.Length != 2) return false;

            if (!double.TryParse(parts[0], NumberStyles.Float, Inv, out var x)) return false;
            if (!double.TryParse(parts[1], NumberStyles.Float, Inv, out var y)) return false;

            result = new CadPointResult(x, y);
            return true;
        }

        private static bool TryParseRelative(string t, double lastX, double lastY, out CadPointResult result)
        {
            result = default(CadPointResult);
            var payload = t.Substring(1).Trim();
            var parts = SplitNumbers(payload);
            if (parts == null || parts.Length != 2) return false;

            if (!double.TryParse(parts[0], NumberStyles.Float, Inv, out var dx)) return false;
            if (!double.TryParse(parts[1], NumberStyles.Float, Inv, out var dy)) return false;

            result = new CadPointResult(lastX + dx, lastY + dy);
            return true;
        }

        private static bool TryParsePolar(string t, double lastX, double lastY, out CadPointResult result)
        {
            result = default(CadPointResult);
            var payload = t.Trim();
            if (payload.EndsWith(">", StringComparison.Ordinal))
                payload = payload.Substring(0, payload.Length - 1);
            if (payload.Length <= 1) return false;
            payload = payload.Substring(1).Trim();

            var parts = SplitNumbers(payload);
            if (parts == null || parts.Length != 2) return false;

            if (!double.TryParse(parts[0], NumberStyles.Float, Inv, out var angleDeg)) return false;
            if (!double.TryParse(parts[1], NumberStyles.Float, Inv, out var dist)) return false;

            var angleRad = angleDeg * Math.PI / 180.0;
            var dx = dist * Math.Cos(angleRad);
            var dy = dist * Math.Sin(angleRad);

            result = new CadPointResult(lastX + dx, lastY + dy);
            return true;
        }

        private static string[] SplitNumbers(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return null;
            var norm = input.Replace(',', ' ');
            return norm.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
        }

        public struct CadPointResult
        {
            public CadPointResult(double x, double y)
            {
                X = x;
                Y = y;
            }

            public double X { get; }
            public double Y { get; }
        }
    }
}
