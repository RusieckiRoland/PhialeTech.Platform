using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using PhialeGrid.Localization;

namespace PhialeGrid.WinUI.Controls
{
    public class PhialeGrid : Control
    {
        public static readonly DependencyProperty LanguageCodeProperty =
            DependencyProperty.Register(
                nameof(LanguageCode),
                typeof(string),
                typeof(PhialeGrid),
                new PropertyMetadata("en"));

        public static readonly DependencyProperty LanguageDirectoryProperty =
            DependencyProperty.Register(
                nameof(LanguageDirectory),
                typeof(string),
                typeof(PhialeGrid),
                new PropertyMetadata(null, HandleLocalizationSourceChanged));

        public static readonly DependencyProperty LocalizationCatalogProperty =
            DependencyProperty.Register(
                nameof(LocalizationCatalog),
                typeof(GridLocalizationCatalog),
                typeof(PhialeGrid),
                new PropertyMetadata(GridLocalizationCatalog.Empty));

        public PhialeGrid()
        {
            DefaultStyleKey = typeof(PhialeGrid);
            ReloadLocalization();
        }

        public string LanguageCode
        {
            get => (string)GetValue(LanguageCodeProperty);
            set => SetValue(LanguageCodeProperty, value);
        }

        public string LanguageDirectory
        {
            get => (string)GetValue(LanguageDirectoryProperty);
            set => SetValue(LanguageDirectoryProperty, value);
        }

        public GridLocalizationCatalog LocalizationCatalog
        {
            get => (GridLocalizationCatalog)GetValue(LocalizationCatalogProperty);
            private set => SetValue(LocalizationCatalogProperty, value);
        }

        public string GetText(string key)
        {
            return LocalizationCatalog.GetText(LanguageCode, key);
        }

        private static void HandleLocalizationSourceChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
        {
            ((PhialeGrid)dependencyObject).ReloadLocalization();
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
