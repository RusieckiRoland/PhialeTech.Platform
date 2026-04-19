using System;

namespace PhialeGis.Library.Core.Styling
{
    public sealed class DefaultStyleContrastPolicy : IStyleContrastPolicy
    {
        private readonly double _minimumContrastRatio;

        public DefaultStyleContrastPolicy(double minimumContrastRatio = 3d)
        {
            _minimumContrastRatio = minimumContrastRatio > 1d ? minimumContrastRatio : 3d;
        }

        public bool ShouldApplyHalo(int foregroundArgb, int backgroundArgb)
        {
            var foreground = CompositeOverBackground(foregroundArgb, backgroundArgb);
            return ComputeContrastRatio(foreground, backgroundArgb) < _minimumContrastRatio;
        }

        public int GetHaloColorArgb(int backgroundArgb)
        {
            return IsDark(backgroundArgb)
                ? unchecked((int)0xE6FFFFFFu)
                : unchecked((int)0xCC000000u);
        }

        public int GetBorderColorArgb(int backgroundArgb)
        {
            return IsDark(backgroundArgb)
                ? unchecked((int)0xFFCBD7E6u)
                : unchecked((int)0xFF163046u);
        }

        private static bool IsDark(int argb)
        {
            return GetRelativeLuminance(argb) < 0.5d;
        }

        private static int CompositeOverBackground(int foregroundArgb, int backgroundArgb)
        {
            var fg = unchecked((uint)foregroundArgb);
            var bg = unchecked((uint)backgroundArgb);

            var alpha = ((fg >> 24) & 0xFF) / 255d;
            if (alpha >= 1d)
                return foregroundArgb;

            var bgAlpha = ((bg >> 24) & 0xFF) / 255d;
            var outAlpha = alpha + (bgAlpha * (1d - alpha));
            if (outAlpha <= double.Epsilon)
                return 0;

            var r = BlendChannel((fg >> 16) & 0xFF, (bg >> 16) & 0xFF, alpha, bgAlpha, outAlpha);
            var g = BlendChannel((fg >> 8) & 0xFF, (bg >> 8) & 0xFF, alpha, bgAlpha, outAlpha);
            var b = BlendChannel(fg & 0xFF, bg & 0xFF, alpha, bgAlpha, outAlpha);
            var a = (byte)Math.Round(outAlpha * 255d);

            return unchecked((int)(((uint)a << 24) | ((uint)r << 16) | ((uint)g << 8) | b));
        }

        private static byte BlendChannel(uint fg, uint bg, double fgAlpha, double bgAlpha, double outAlpha)
        {
            var value = ((fg * fgAlpha) + (bg * bgAlpha * (1d - fgAlpha))) / outAlpha;
            return (byte)Math.Round(Math.Max(0d, Math.Min(255d, value)));
        }

        private static double ComputeContrastRatio(int firstArgb, int secondArgb)
        {
            var first = GetRelativeLuminance(firstArgb);
            var second = GetRelativeLuminance(secondArgb);
            var lighter = Math.Max(first, second);
            var darker = Math.Min(first, second);
            return (lighter + 0.05d) / (darker + 0.05d);
        }

        private static double GetRelativeLuminance(int argb)
        {
            var value = unchecked((uint)argb);
            var r = Linearize((byte)((value >> 16) & 0xFF));
            var g = Linearize((byte)((value >> 8) & 0xFF));
            var b = Linearize((byte)(value & 0xFF));
            return (0.2126d * r) + (0.7152d * g) + (0.0722d * b);
        }

        private static double Linearize(byte channel)
        {
            var srgb = channel / 255d;
            if (srgb <= 0.03928d)
                return srgb / 12.92d;

            return Math.Pow((srgb + 0.055d) / 1.055d, 2.4d);
        }
    }
}
