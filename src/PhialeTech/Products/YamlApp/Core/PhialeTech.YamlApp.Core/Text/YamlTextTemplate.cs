using System;
using System.Collections.Generic;
using System.Text;

namespace PhialeTech.YamlApp.Core.Text
{
    public static class YamlTextTemplate
    {
        public static bool TryGetPlaceholders(string template, out IReadOnlyList<string> placeholders, out string error)
        {
            var result = new List<string>();
            error = null;

            if (string.IsNullOrEmpty(template))
            {
                placeholders = result;
                return true;
            }

            for (var index = 0; index < template.Length; index++)
            {
                var current = template[index];
                if (current == '{')
                {
                    if (index + 1 < template.Length && template[index + 1] == '{')
                    {
                        index++;
                        continue;
                    }

                    var endIndex = template.IndexOf('}', index + 1);
                    if (endIndex < 0)
                    {
                        placeholders = result;
                        error = "Text template contains an opening '{' without a matching closing '}'.";
                        return false;
                    }

                    var placeholder = template.Substring(index + 1, endIndex - index - 1).Trim();
                    if (!IsValidPlaceholder(placeholder))
                    {
                        placeholders = result;
                        error = string.Format("Text template contains invalid placeholder '{0}'.", placeholder);
                        return false;
                    }

                    if (!Contains(result, placeholder))
                    {
                        result.Add(placeholder);
                    }

                    index = endIndex;
                    continue;
                }

                if (current == '}')
                {
                    if (index + 1 < template.Length && template[index + 1] == '}')
                    {
                        index++;
                        continue;
                    }

                    placeholders = result;
                    error = "Text template contains a closing '}' without a matching opening '{'.";
                    return false;
                }
            }

            placeholders = result;
            return true;
        }

        public static string Format(string template, Func<string, object> valueResolver)
        {
            if (template == null)
            {
                return string.Empty;
            }

            if (valueResolver == null)
            {
                throw new ArgumentNullException(nameof(valueResolver));
            }

            var builder = new StringBuilder();
            for (var index = 0; index < template.Length; index++)
            {
                var current = template[index];
                if (current == '{')
                {
                    if (index + 1 < template.Length && template[index + 1] == '{')
                    {
                        builder.Append('{');
                        index++;
                        continue;
                    }

                    var endIndex = template.IndexOf('}', index + 1);
                    if (endIndex < 0)
                    {
                        throw new InvalidOperationException("Text template contains an opening '{' without a matching closing '}'.");
                    }

                    var placeholder = template.Substring(index + 1, endIndex - index - 1).Trim();
                    if (!IsValidPlaceholder(placeholder))
                    {
                        throw new InvalidOperationException(string.Format("Text template contains invalid placeholder '{0}'.", placeholder));
                    }

                    var value = valueResolver(placeholder);
                    if (value != null)
                    {
                        builder.Append(value);
                    }

                    index = endIndex;
                    continue;
                }

                if (current == '}')
                {
                    if (index + 1 < template.Length && template[index + 1] == '}')
                    {
                        builder.Append('}');
                        index++;
                        continue;
                    }

                    throw new InvalidOperationException("Text template contains a closing '}' without a matching opening '{'.");
                }

                builder.Append(current);
            }

            return builder.ToString();
        }

        private static bool Contains(List<string> placeholders, string value)
        {
            for (var index = 0; index < placeholders.Count; index++)
            {
                if (string.Equals(placeholders[index], value, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsValidPlaceholder(string placeholder)
        {
            if (string.IsNullOrWhiteSpace(placeholder))
            {
                return false;
            }

            for (var index = 0; index < placeholder.Length; index++)
            {
                var current = placeholder[index];
                if (char.IsLetterOrDigit(current) || current == '_' || current == '-' || current == '.')
                {
                    continue;
                }

                return false;
            }

            return true;
        }
    }
}
