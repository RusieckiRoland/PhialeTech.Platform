using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using PhialeTech.ActiveLayerSelector.Localization;

namespace PhialeTech.ActiveLayerSelector.Wpf.Controls
{
    public partial class ActiveLayerSelector : UserControl
    {
        private static readonly Uri DayThemeTokensUri = new Uri("pack://application:,,,/PhialeTech.Styles.Wpf;component/Themes/ThemeTokens.Day.xaml", UriKind.Absolute);
        private static readonly Uri NightThemeTokensUri = new Uri("pack://application:,,,/PhialeTech.Styles.Wpf;component/Themes/ThemeTokens.Night.xaml", UriKind.Absolute);
        private static readonly Uri HighContrastThemeTokensUri = new Uri("pack://application:,,,/PhialeTech.Styles.Wpf;component/Themes/ThemeTokens.HighContrast.xaml", UriKind.Absolute);
        public static readonly DependencyProperty StateProperty = DependencyProperty.Register(nameof(State), typeof(IActiveLayerSelectorState), typeof(ActiveLayerSelector), new PropertyMetadata(null, HandleSelectorPropertyChanged));
        public static readonly DependencyProperty LanguageCodeProperty = DependencyProperty.Register(nameof(LanguageCode), typeof(string), typeof(ActiveLayerSelector), new PropertyMetadata("en", HandleSelectorPropertyChanged));
        public static readonly DependencyProperty LanguageDirectoryProperty = DependencyProperty.Register(nameof(LanguageDirectory), typeof(string), typeof(ActiveLayerSelector), new PropertyMetadata(string.Empty, HandleSelectorPropertyChanged));
        public static readonly DependencyProperty InitialVisibleItemCountProperty = DependencyProperty.Register(nameof(InitialVisibleItemCount), typeof(int), typeof(ActiveLayerSelector), new PropertyMetadata(5, HandleSelectorPropertyChanged));
        public static readonly DependencyProperty IsNightModeProperty = DependencyProperty.Register(nameof(IsNightMode), typeof(bool), typeof(ActiveLayerSelector), new PropertyMetadata(false, HandleThemePropertyChanged));

        private ActiveLayerSelectorViewModel _viewModel;
        private ResourceDictionary _themeTokenDictionary;
        private bool _systemThemeSubscriptionActive;

        public ActiveLayerSelector()
        {
            InitializeComponent();
            ApplyThemeResources();
            _viewModel = new ActiveLayerSelectorViewModel();
            LayoutRoot.DataContext = _viewModel;
            Loaded += HandleLoaded;
            Unloaded += HandleUnloaded;
        }

        public IActiveLayerSelectorState State
        {
            get => (IActiveLayerSelectorState)GetValue(StateProperty);
            set => SetValue(StateProperty, value);
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

        public int InitialVisibleItemCount
        {
            get => (int)GetValue(InitialVisibleItemCountProperty);
            set => SetValue(InitialVisibleItemCountProperty, value);
        }

        public bool IsNightMode
        {
            get => (bool)GetValue(IsNightModeProperty);
            set => SetValue(IsNightModeProperty, value);
        }

        private static void HandleSelectorPropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            ((ActiveLayerSelector)dependencyObject).RefreshViewModel();
        }

        private static void HandleThemePropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            ((ActiveLayerSelector)dependencyObject).ApplyThemeResources();
        }

        private void HandleLoaded(object sender, RoutedEventArgs e)
        {
            if (!_systemThemeSubscriptionActive)
            {
                SystemParameters.StaticPropertyChanged += HandleSystemParametersChanged;
                _systemThemeSubscriptionActive = true;
            }

            ApplyThemeResources();
            RefreshViewModel();
        }

        private void HandleUnloaded(object sender, RoutedEventArgs e)
        {
            if (_systemThemeSubscriptionActive)
            {
                SystemParameters.StaticPropertyChanged -= HandleSystemParametersChanged;
                _systemThemeSubscriptionActive = false;
            }

            _viewModel.AttachState(null);
        }

        private void HandleExpandedPopupClosed(object sender, EventArgs e)
        {
            if (_viewModel != null && _viewModel.IsExpanded)
            {
                DispatchCommand(ActiveLayerSelectorCommandIds.ToggleExpanded);
            }
        }

        private void HandleExpandCollapseClick(object sender, RoutedEventArgs e)
        {
            DispatchCommand(ActiveLayerSelectorCommandIds.ToggleExpanded);
        }

        private void HandleHeaderChromeMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var source = e.OriginalSource as DependencyObject;
            if (FindVisualParent<ButtonBase>(source) != null)
            {
                return;
            }

            DispatchCommand(ActiveLayerSelectorCommandIds.ToggleExpanded);
        }

        private void HandleShowMoreClick(object sender, RoutedEventArgs e)
        {
            DispatchCommand(ActiveLayerSelectorCommandIds.ShowMore);
        }

        private void HandlePickerPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape && _viewModel != null && _viewModel.IsExpanded)
            {
                DispatchCommand(ActiveLayerSelectorCommandIds.ToggleExpanded);
                e.Handled = true;
                return;
            }

            if (e.Key != Key.Enter)
            {
                return;
            }

            var focusedElement = Keyboard.FocusedElement as DependencyObject;
            var layer = ResolveLayerFromVisual(focusedElement);
            if (layer == null || !layer.CanSetActive)
            {
                return;
            }

            DispatchCommand(
                ActiveLayerSelectorCommandIds.SetActive,
                new Dictionary<string, string>
                {
                    ["layerId"] = layer.LayerId,
                });
            e.Handled = true;
        }

        private void HandleSetActiveClick(object sender, RoutedEventArgs e)
        {
            var layer = (sender as FrameworkElement)?.DataContext as ActiveLayerSelectorLayerViewModel;
            if (layer == null)
            {
                return;
            }

            DispatchCommand(
                ActiveLayerSelectorCommandIds.SetActive,
                new Dictionary<string, string>
                {
                    ["layerId"] = layer.LayerId,
                });
        }

        private void HandleCapabilityClick(object sender, RoutedEventArgs e)
        {
            var capability = (sender as FrameworkElement)?.DataContext as ActiveLayerSelectorCapabilityViewModel;
            if (capability == null)
            {
                return;
            }

            DispatchCommand(
                ActiveLayerSelectorCommandIds.ToggleCapability,
                new Dictionary<string, string>
                {
                    ["layerId"] = capability.LayerId,
                    ["capability"] = capability.Kind.ToString(),
                });
        }

        private void RefreshViewModel()
        {
            if (!IsLoaded)
            {
                ApplyThemeResources();
                return;
            }

            _viewModel.Dispose();
            _viewModel = new ActiveLayerSelectorViewModel(LoadCatalog(LanguageDirectory), LanguageCode, InitialVisibleItemCount);
            _viewModel.AttachState(State);
            LayoutRoot.DataContext = _viewModel;
            ApplyThemeResources();
        }

        private static ActiveLayerSelectorLocalizationCatalog LoadCatalog(string languageDirectory)
        {
            try
            {
                return string.IsNullOrWhiteSpace(languageDirectory) || !Directory.Exists(languageDirectory)
                    ? ActiveLayerSelectorLocalizationCatalog.LoadDefault()
                    : ActiveLayerSelectorLocalizationCatalog.LoadFromDirectory(languageDirectory);
            }
            catch
            {
                return ActiveLayerSelectorLocalizationCatalog.Empty;
            }
        }

        private void DispatchCommand(string commandId, IDictionary<string, string> arguments = null)
        {
            if (_viewModel == null)
            {
                return;
            }

            _viewModel.HandleCommand(ActiveLayerSelectorUniversalInputAdapter.CreateCommand(commandId, Keyboard.Modifiers, arguments));
        }

        private void ApplyThemeResources()
        {
            var themeDictionary = EnsureThemeTokenDictionary();
            var themeUri = ResolveThemeTokenDictionaryUri();
            if (themeDictionary.Source == null || !Uri.Equals(themeDictionary.Source, themeUri))
            {
                themeDictionary.Source = themeUri;
            }
        }

        private ResourceDictionary EnsureThemeTokenDictionary()
        {
            if (_themeTokenDictionary != null)
            {
                return _themeTokenDictionary;
            }

            foreach (var dictionary in Resources.MergedDictionaries)
            {
                if (dictionary.Source != null &&
                    dictionary.Source.OriginalString.IndexOf("ThemeTokens.", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    _themeTokenDictionary = dictionary;
                    break;
                }
            }
            if (_themeTokenDictionary == null)
            {
                _themeTokenDictionary = new ResourceDictionary { Source = DayThemeTokensUri };
                Resources.MergedDictionaries.Insert(0, _themeTokenDictionary);
            }

            return _themeTokenDictionary;
        }

        private Uri ResolveThemeTokenDictionaryUri()
        {
            if (SystemParameters.HighContrast)
            {
                return HighContrastThemeTokensUri;
            }

            return IsNightMode ? NightThemeTokensUri : DayThemeTokensUri;
        }

        private void HandleSystemParametersChanged(object sender, PropertyChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(e.PropertyName) || string.Equals(e.PropertyName, nameof(SystemParameters.HighContrast), StringComparison.Ordinal))
            {
                ApplyThemeResources();
            }
        }

        private static ActiveLayerSelectorLayerViewModel ResolveLayerFromVisual(DependencyObject dependencyObject)
        {
            var current = dependencyObject;
            while (current != null)
            {
                if (current is FrameworkElement frameworkElement && frameworkElement.DataContext is ActiveLayerSelectorLayerViewModel layer)
                {
                    return layer;
                }

                current = VisualTreeHelper.GetParent(current) ?? (current as FrameworkElement)?.Parent;
            }

            return null;
        }

        private static T FindVisualParent<T>(DependencyObject dependencyObject) where T : DependencyObject
        {
            var current = dependencyObject;
            while (current != null)
            {
                if (current is T matching)
                {
                    return matching;
                }

                current = VisualTreeHelper.GetParent(current) ?? (current as FrameworkElement)?.Parent;
            }

            return null;
        }
    }
}
