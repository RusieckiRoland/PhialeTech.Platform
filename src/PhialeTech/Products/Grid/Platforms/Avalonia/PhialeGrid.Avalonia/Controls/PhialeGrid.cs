using Avalonia;
using Avalonia.Controls.Primitives;
using PhialeGrid.Localization;

namespace PhialeGrid.Avalonia.Controls
{
    public class PhialeGrid : TemplatedControl
    {
        public static readonly StyledProperty<string> LanguageCodeProperty =
            AvaloniaProperty.Register<PhialeGrid, string>(nameof(LanguageCode), "en");

        public static readonly StyledProperty<string> LanguageDirectoryProperty =
            AvaloniaProperty.Register<PhialeGrid, string>(nameof(LanguageDirectory));

        public static readonly StyledProperty<GridLocalizationCatalog> LocalizationCatalogProperty =
            AvaloniaProperty.Register<PhialeGrid, GridLocalizationCatalog>(nameof(LocalizationCatalog), GridLocalizationCatalog.Empty);

        static PhialeGrid()
        {
            LanguageDirectoryProperty.Changed.AddClassHandler<PhialeGrid>((grid, _) => grid.ReloadLocalization());
        }

        public PhialeGrid()
        {
            ReloadLocalization();
        }

        public string LanguageCode
        {
            get => GetValue(LanguageCodeProperty);
            set => SetValue(LanguageCodeProperty, value);
        }

        public string LanguageDirectory
        {
            get => GetValue(LanguageDirectoryProperty);
            set => SetValue(LanguageDirectoryProperty, value);
        }

        public GridLocalizationCatalog LocalizationCatalog
        {
            get => GetValue(LocalizationCatalogProperty);
            private set => SetValue(LocalizationCatalogProperty, value);
        }

        public string GetText(string key)
        {
            return LocalizationCatalog.GetText(LanguageCode, key);
        }

        private void ReloadLocalization()
        {
            LocalizationCatalog = LoadCatalog(LanguageDirectory);
        }

        private static GridLocalizationCatalog LoadCatalog(string languageDirectory)
        {
            if (!string.IsNullOrWhiteSpace(languageDirectory))
            {
                try
                {
                    return GridLocalizationCatalog.LoadFromDirectory(languageDirectory);
                }
                catch
                {
                }
            }

            try
            {
                return GridLocalizationCatalog.LoadDefault();
            }
            catch
            {
                return GridLocalizationCatalog.Empty;
            }
        }
    }
}
