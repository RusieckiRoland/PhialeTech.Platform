using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;

namespace PhialeTech.Components.Wpf.Controls
{
    public partial class YamlTextBoxPureWpf : UserControl, INotifyPropertyChanged
    {
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register(nameof(Text), typeof(string), typeof(YamlTextBoxPureWpf), new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnVisualPropertyChanged));

        public static readonly DependencyProperty CaptionProperty =
            DependencyProperty.Register(nameof(Caption), typeof(string), typeof(YamlTextBoxPureWpf), new FrameworkPropertyMetadata(string.Empty, OnVisualPropertyChanged));

        public static readonly DependencyProperty PlaceholderProperty =
            DependencyProperty.Register(nameof(Placeholder), typeof(string), typeof(YamlTextBoxPureWpf), new FrameworkPropertyMetadata(string.Empty, OnVisualPropertyChanged));

        public static readonly DependencyProperty OldValueProperty =
            DependencyProperty.Register(nameof(OldValue), typeof(string), typeof(YamlTextBoxPureWpf), new FrameworkPropertyMetadata(string.Empty, OnVisualPropertyChanged));

        public static readonly DependencyProperty ErrorMessageProperty =
            DependencyProperty.Register(nameof(ErrorMessage), typeof(string), typeof(YamlTextBoxPureWpf), new FrameworkPropertyMetadata(string.Empty, OnVisualPropertyChanged));

        public static readonly DependencyProperty IsFieldRequiredProperty =
            DependencyProperty.Register(nameof(IsFieldRequired), typeof(bool), typeof(YamlTextBoxPureWpf), new FrameworkPropertyMetadata(false, OnVisualPropertyChanged));

        public static readonly DependencyProperty ShowOldValueRestoreButtonProperty =
            DependencyProperty.Register(nameof(ShowOldValueRestoreButton), typeof(bool), typeof(YamlTextBoxPureWpf), new FrameworkPropertyMetadata(false, OnVisualPropertyChanged));

        private bool _isInputFocused;

        public YamlTextBoxPureWpf()
        {
            InitializeComponent();
            IsEnabledChanged += HandleIsEnabledChanged;
            Loaded += HandleLoaded;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public string Caption
        {
            get => (string)GetValue(CaptionProperty);
            set => SetValue(CaptionProperty, value);
        }

        public string Placeholder
        {
            get => (string)GetValue(PlaceholderProperty);
            set => SetValue(PlaceholderProperty, value);
        }

        public string OldValue
        {
            get => (string)GetValue(OldValueProperty);
            set => SetValue(OldValueProperty, value);
        }

        public string ErrorMessage
        {
            get => (string)GetValue(ErrorMessageProperty);
            set => SetValue(ErrorMessageProperty, value);
        }

        public bool IsFieldRequired
        {
            get => (bool)GetValue(IsFieldRequiredProperty);
            set => SetValue(IsFieldRequiredProperty, value);
        }

        public bool ShowOldValueRestoreButton
        {
            get => (bool)GetValue(ShowOldValueRestoreButtonProperty);
            set => SetValue(ShowOldValueRestoreButtonProperty, value);
        }

        public bool IsInputFocused
        {
            get => _isInputFocused;
            private set
            {
                if (_isInputFocused == value)
                {
                    return;
                }

                _isInputFocused = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(PlaceholderVisibility));
            }
        }

        public bool ShowError => IsFieldRequired && string.IsNullOrWhiteSpace(Text) && !string.IsNullOrWhiteSpace(ErrorMessage);

        public bool ShowClearButton => IsEnabled && !string.IsNullOrWhiteSpace(Text);

        public bool ShowRestoreButton => IsEnabled && string.IsNullOrWhiteSpace(Text) && ShowOldValueRestoreButton && !string.IsNullOrWhiteSpace(OldValue);

        public Visibility TrailingActionVisibility => ShowClearButton || ShowRestoreButton ? Visibility.Visible : Visibility.Collapsed;

        public Visibility PlaceholderVisibility => string.IsNullOrEmpty(Text) && !string.IsNullOrEmpty(Placeholder) && !IsInputFocused
            ? Visibility.Visible
            : Visibility.Collapsed;

        private static void OnVisualPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((YamlTextBoxPureWpf)d).RaiseStatePropertiesChanged();
        }

        private void HandleLoaded(object sender, RoutedEventArgs e)
        {
            RaiseStatePropertiesChanged();
        }

        private void HandleIsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            RaiseStatePropertiesChanged();
        }

        private void HandleEditorGotKeyboardFocus(object sender, System.Windows.Input.KeyboardFocusChangedEventArgs e)
        {
            IsInputFocused = true;
        }

        private void HandleEditorLostKeyboardFocus(object sender, System.Windows.Input.KeyboardFocusChangedEventArgs e)
        {
            IsInputFocused = false;
        }

        private void HandleClearButtonClick(object sender, RoutedEventArgs e)
        {
            Text = ShowRestoreButton ? (OldValue ?? string.Empty) : string.Empty;
            Editor.Focus();
            Editor.CaretIndex = Editor.Text == null ? 0 : Editor.Text.Length;
            RaiseStatePropertiesChanged();
        }

        private void RaiseStatePropertiesChanged()
        {
            OnPropertyChanged(nameof(ShowError));
            OnPropertyChanged(nameof(ShowClearButton));
            OnPropertyChanged(nameof(ShowRestoreButton));
            OnPropertyChanged(nameof(TrailingActionVisibility));
            OnPropertyChanged(nameof(PlaceholderVisibility));
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
