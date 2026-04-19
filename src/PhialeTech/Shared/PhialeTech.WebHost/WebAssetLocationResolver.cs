using System;
using System.IO;

namespace PhialeTech.WebHost
{
    /// <summary>
    /// Resolves the shared web-assets root in consuming applications.
    /// </summary>
    public static class WebAssetLocationResolver
    {
        public static string ResolveAssetsRoot()
        {
            return Path.Combine(AppContext.BaseDirectory, "Assets");
        }

        public static string ResolveAssetPath(string relativePath)
        {
            var normalized = (relativePath ?? string.Empty)
                .Replace('/', Path.DirectorySeparatorChar)
                .TrimStart(Path.DirectorySeparatorChar);

            return Path.Combine(ResolveAssetsRoot(), normalized);
        }
    }
}
