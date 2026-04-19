using System;
using System.Text;

namespace PhialeGis.Library.Core.Web
{
    internal static class WebComponentHtmlContent
    {
        public static string ApplyBaseUrl(string html, string baseUrl)
        {
            string content = html ?? string.Empty;
            if (string.IsNullOrWhiteSpace(baseUrl))
                return content;

            string baseTag = "<base href=\"" + HtmlAttributeEncode(baseUrl) + "\" />";
            int headIndex = content.IndexOf("<head", StringComparison.OrdinalIgnoreCase);
            if (headIndex >= 0)
            {
                int headClose = content.IndexOf('>', headIndex);
                if (headClose >= 0)
                    return content.Insert(headClose + 1, baseTag);
            }

            return "<head>" + baseTag + "</head>" + content;
        }

        public static string ToDataUri(string html)
        {
            string content = html ?? string.Empty;
            string base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(content));
            return "data:text/html;base64," + base64;
        }

        private static string HtmlAttributeEncode(string value)
        {
            return (value ?? string.Empty)
                .Replace("&", "&amp;")
                .Replace("\"", "&quot;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;");
        }
    }
}
