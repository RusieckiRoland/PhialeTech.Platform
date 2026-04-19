using System.Globalization;
using System.Linq;

namespace PhialeTech.YamlApp.Core.Controls.IntegerBox
{
    public static class YamlIntegerInputRules
    {
        public static bool IsCandidateValid(string candidateText, int? minValue, int? maxValue)
        {
            if (string.IsNullOrEmpty(candidateText))
            {
                return true;
            }

            if (candidateText == "-")
            {
                return AllowsNegative(minValue);
            }

            if (candidateText.Count(character => character == '-') > 1)
            {
                return false;
            }

            if (candidateText.IndexOf('-') > 0)
            {
                return false;
            }

            for (var index = 0; index < candidateText.Length; index++)
            {
                var character = candidateText[index];
                if (character == '-' && index == 0)
                {
                    if (!AllowsNegative(minValue))
                    {
                        return false;
                    }

                    continue;
                }

                if (!char.IsDigit(character))
                {
                    return false;
                }
            }

            if (!int.TryParse(candidateText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
            {
                return false;
            }

            if (minValue.HasValue && parsed < minValue.Value)
            {
                return false;
            }

            if (maxValue.HasValue && parsed > maxValue.Value)
            {
                return false;
            }

            return true;
        }

        public static string BuildCandidate(string currentText, int selectionStart, int selectionLength, string insertedText)
        {
            var safeCurrent = currentText ?? string.Empty;
            var safeInsert = insertedText ?? string.Empty;
            var safeStart = selectionStart < 0 ? 0 : selectionStart;
            if (safeStart > safeCurrent.Length)
            {
                safeStart = safeCurrent.Length;
            }

            var safeLength = selectionLength < 0 ? 0 : selectionLength;
            if (safeStart + safeLength > safeCurrent.Length)
            {
                safeLength = safeCurrent.Length - safeStart;
            }

            return safeCurrent.Remove(safeStart, safeLength).Insert(safeStart, safeInsert);
        }

        private static bool AllowsNegative(int? minValue)
        {
            return minValue.HasValue && minValue.Value < 0;
        }
    }
}
