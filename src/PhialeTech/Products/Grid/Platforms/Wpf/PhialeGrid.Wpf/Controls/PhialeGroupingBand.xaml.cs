using System;
using System.Collections;
using System.Windows;
using System.Windows.Controls;

namespace PhialeTech.PhialeGrid.Wpf.Controls
{
    public sealed partial class PhialeGroupingBand : UserControl
    {
        public static readonly DependencyProperty BandLabelTextProperty = DependencyProperty.Register(
            nameof(BandLabelText),
            typeof(string),
            typeof(PhialeGroupingBand),
            new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty DropTextProperty = DependencyProperty.Register(
            nameof(DropText),
            typeof(string),
            typeof(PhialeGroupingBand),
            new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty ExpandAllTextProperty = DependencyProperty.Register(
            nameof(ExpandAllText),
            typeof(string),
            typeof(PhialeGroupingBand),
            new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty CollapseAllTextProperty = DependencyProperty.Register(
            nameof(CollapseAllText),
            typeof(string),
            typeof(PhialeGroupingBand),
            new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty GroupChipsProperty = DependencyProperty.Register(
            nameof(GroupChips),
            typeof(IEnumerable),
            typeof(PhialeGroupingBand),
            new PropertyMetadata(null));

        public static readonly DependencyProperty HasGroupsProperty = DependencyProperty.Register(
            nameof(HasGroups),
            typeof(bool),
            typeof(PhialeGroupingBand),
            new PropertyMetadata(false));

        public static readonly DependencyProperty HasNoGroupsProperty = DependencyProperty.Register(
            nameof(HasNoGroups),
            typeof(bool),
            typeof(PhialeGroupingBand),
            new PropertyMetadata(true));

        public static readonly DependencyProperty AllowsGroupingDragProperty = DependencyProperty.Register(
            nameof(AllowsGroupingDrag),
            typeof(bool),
            typeof(PhialeGroupingBand),
            new PropertyMetadata(false));

        public static readonly DependencyProperty DragDataFormatProperty = DependencyProperty.Register(
            nameof(DragDataFormat),
            typeof(string),
            typeof(PhialeGroupingBand),
            new PropertyMetadata(string.Empty));

        public static readonly RoutedEvent RemoveGroupRequestedEvent = EventManager.RegisterRoutedEvent(
            nameof(RemoveGroupRequested),
            RoutingStrategy.Bubble,
            typeof(EventHandler<PhialeGroupingBandColumnEventArgs>),
            typeof(PhialeGroupingBand));

        public static readonly RoutedEvent ToggleDirectionRequestedEvent = EventManager.RegisterRoutedEvent(
            nameof(ToggleDirectionRequested),
            RoutingStrategy.Bubble,
            typeof(EventHandler<PhialeGroupingBandColumnEventArgs>),
            typeof(PhialeGroupingBand));

        public static readonly RoutedEvent ColumnDroppedEvent = EventManager.RegisterRoutedEvent(
            nameof(ColumnDropped),
            RoutingStrategy.Bubble,
            typeof(EventHandler<PhialeGroupingBandColumnEventArgs>),
            typeof(PhialeGroupingBand));

        public static readonly RoutedEvent ExpandAllGroupsRequestedEvent = EventManager.RegisterRoutedEvent(
            nameof(ExpandAllGroupsRequested),
            RoutingStrategy.Bubble,
            typeof(RoutedEventHandler),
            typeof(PhialeGroupingBand));

        public static readonly RoutedEvent CollapseAllGroupsRequestedEvent = EventManager.RegisterRoutedEvent(
            nameof(CollapseAllGroupsRequested),
            RoutingStrategy.Bubble,
            typeof(RoutedEventHandler),
            typeof(PhialeGroupingBand));

        public PhialeGroupingBand()
        {
            InitializeComponent();
        }

        public event EventHandler<PhialeGroupingBandColumnEventArgs> RemoveGroupRequested
        {
            add => AddHandler(RemoveGroupRequestedEvent, value);
            remove => RemoveHandler(RemoveGroupRequestedEvent, value);
        }

        public event EventHandler<PhialeGroupingBandColumnEventArgs> ToggleDirectionRequested
        {
            add => AddHandler(ToggleDirectionRequestedEvent, value);
            remove => RemoveHandler(ToggleDirectionRequestedEvent, value);
        }

        public event EventHandler<PhialeGroupingBandColumnEventArgs> ColumnDropped
        {
            add => AddHandler(ColumnDroppedEvent, value);
            remove => RemoveHandler(ColumnDroppedEvent, value);
        }

        public event RoutedEventHandler ExpandAllGroupsRequested
        {
            add => AddHandler(ExpandAllGroupsRequestedEvent, value);
            remove => RemoveHandler(ExpandAllGroupsRequestedEvent, value);
        }

        public event RoutedEventHandler CollapseAllGroupsRequested
        {
            add => AddHandler(CollapseAllGroupsRequestedEvent, value);
            remove => RemoveHandler(CollapseAllGroupsRequestedEvent, value);
        }

        public string BandLabelText
        {
            get => (string)GetValue(BandLabelTextProperty);
            set => SetValue(BandLabelTextProperty, value);
        }

        public string DropText
        {
            get => (string)GetValue(DropTextProperty);
            set => SetValue(DropTextProperty, value);
        }

        public string ExpandAllText
        {
            get => (string)GetValue(ExpandAllTextProperty);
            set => SetValue(ExpandAllTextProperty, value);
        }

        public string CollapseAllText
        {
            get => (string)GetValue(CollapseAllTextProperty);
            set => SetValue(CollapseAllTextProperty, value);
        }

        public IEnumerable GroupChips
        {
            get => (IEnumerable)GetValue(GroupChipsProperty);
            set => SetValue(GroupChipsProperty, value);
        }

        public bool HasGroups
        {
            get => (bool)GetValue(HasGroupsProperty);
            set => SetValue(HasGroupsProperty, value);
        }

        public bool HasNoGroups
        {
            get => (bool)GetValue(HasNoGroupsProperty);
            set => SetValue(HasNoGroupsProperty, value);
        }

        public bool AllowsGroupingDrag
        {
            get => (bool)GetValue(AllowsGroupingDragProperty);
            set => SetValue(AllowsGroupingDragProperty, value);
        }

        public string DragDataFormat
        {
            get => (string)GetValue(DragDataFormatProperty);
            set => SetValue(DragDataFormatProperty, value);
        }

        private void HandleRemoveGroupClick(object sender, RoutedEventArgs e)
        {
            var columnId = (sender as FrameworkElement)?.Tag as string;
            RaiseColumnEvent(RemoveGroupRequestedEvent, columnId);
        }

        private void HandleToggleDirectionClick(object sender, RoutedEventArgs e)
        {
            var columnId = (sender as FrameworkElement)?.Tag as string;
            RaiseColumnEvent(ToggleDirectionRequestedEvent, columnId);
        }

        private void HandleExpandAllGroupsClick(object sender, RoutedEventArgs e)
        {
            RaiseEvent(new RoutedEventArgs(ExpandAllGroupsRequestedEvent, this));
        }

        private void HandleCollapseAllGroupsClick(object sender, RoutedEventArgs e)
        {
            RaiseEvent(new RoutedEventArgs(CollapseAllGroupsRequestedEvent, this));
        }

        private void HandleGroupingDragEnter(object sender, DragEventArgs e)
        {
            ApplyDragEffect(e);
        }

        private void HandleGroupingDragLeave(object sender, DragEventArgs e)
        {
            e.Handled = true;
        }

        private void HandleGroupingDragOver(object sender, DragEventArgs e)
        {
            ApplyDragEffect(e);
        }

        private void HandleGroupingDrop(object sender, DragEventArgs e)
        {
            if (!AllowsGroupingDrag || string.IsNullOrWhiteSpace(DragDataFormat))
            {
                e.Handled = true;
                return;
            }

            var columnId = e.Data.GetData(DragDataFormat) as string;
            if (!string.IsNullOrWhiteSpace(columnId))
            {
                RaiseColumnEvent(ColumnDroppedEvent, columnId);
            }

            e.Handled = true;
        }

        private void ApplyDragEffect(DragEventArgs e)
        {
            if (!AllowsGroupingDrag || string.IsNullOrWhiteSpace(DragDataFormat))
            {
                e.Effects = DragDropEffects.None;
                e.Handled = true;
                return;
            }

            e.Effects = e.Data.GetDataPresent(DragDataFormat) ? DragDropEffects.Move : DragDropEffects.None;
            e.Handled = true;
        }

        private void RaiseColumnEvent(RoutedEvent routedEvent, string columnId)
        {
            if (string.IsNullOrWhiteSpace(columnId))
            {
                return;
            }

            RaiseEvent(new PhialeGroupingBandColumnEventArgs(routedEvent, this, columnId));
        }
    }

    public sealed class PhialeGroupingBandColumnEventArgs : RoutedEventArgs
    {
        public PhialeGroupingBandColumnEventArgs(RoutedEvent routedEvent, object source, string columnId)
            : base(routedEvent, source)
        {
            ColumnId = columnId ?? string.Empty;
        }

        public string ColumnId { get; }
    }
}
