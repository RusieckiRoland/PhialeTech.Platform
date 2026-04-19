using PhialeTech.Components.Shared.Core;

namespace PhialeTech.Components.Shared.ViewModels
{
    public sealed class DemoColumnChooserItemViewModel : BindableBase
    {
        private bool _isVisible;

        public DemoColumnChooserItemViewModel(string columnId, string header, bool isVisible)
        {
            ColumnId = columnId ?? string.Empty;
            Header = header ?? string.Empty;
            _isVisible = isVisible;
        }

        public string ColumnId { get; }

        public string Header { get; }

        public bool IsVisible
        {
            get => _isVisible;
            set => SetProperty(ref _isVisible, value);
        }
    }
}
