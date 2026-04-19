using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using PhialeTech.YamlApp.Core.Controls.IntegerBox;
using PhialeTech.YamlApp.Wpf.Controls.TextBox;

namespace PhialeTech.YamlApp.Wpf.Controls.IntegerBox
{
    [ToolboxItem(true)]
    [TemplatePart(Name = PartEditor, Type = typeof(System.Windows.Controls.TextBox))]
    public class YamlIntegerBox : YamlTextBox
    {
        private const string PartEditor = "PART_Editor";

        private System.Windows.Controls.TextBox _editor;

        public static readonly DependencyProperty MinValueProperty =
            DependencyProperty.Register(nameof(MinValue), typeof(int?), typeof(YamlIntegerBox), new FrameworkPropertyMetadata(null));

        public static readonly DependencyProperty MaxValueProperty =
            DependencyProperty.Register(nameof(MaxValue), typeof(int?), typeof(YamlIntegerBox), new FrameworkPropertyMetadata(null));

        static YamlIntegerBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(YamlIntegerBox), new FrameworkPropertyMetadata(typeof(YamlIntegerBox)));
        }

        public int? MinValue
        {
            get => (int?)GetValue(MinValueProperty);
            set => SetValue(MinValueProperty, value);
        }

        public int? MaxValue
        {
            get => (int?)GetValue(MaxValueProperty);
            set => SetValue(MaxValueProperty, value);
        }

        public override void OnApplyTemplate()
        {
            DetachEditorHandlers();
            base.OnApplyTemplate();

            _editor = GetTemplateChild(PartEditor) as System.Windows.Controls.TextBox;
            if (_editor == null)
            {
                return;
            }

            InputMethod.SetIsInputMethodEnabled(_editor, false);
            DataObject.RemovePastingHandler(_editor, OnEditorPaste);
            DataObject.AddPastingHandler(_editor, OnEditorPaste);
            _editor.PreviewTextInput += OnEditorPreviewTextInput;
            _editor.PreviewKeyDown += OnEditorPreviewKeyDown;
        }

        protected override void OnPreviewTextInput(TextCompositionEventArgs e)
        {
            if (!e.Handled && e.OriginalSource is System.Windows.Controls.TextBox textBox && ReferenceEquals(textBox, _editor))
            {
                e.Handled = !IsInputAllowed(textBox, e.Text);
            }

            base.OnPreviewTextInput(e);
        }

        private void OnEditorPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (sender is System.Windows.Controls.TextBox textBox)
            {
                e.Handled = !IsInputAllowed(textBox, e.Text);
            }
        }

        private void OnEditorPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
            {
                e.Handled = true;
            }
        }

        private void OnEditorPaste(object sender, DataObjectPastingEventArgs e)
        {
            if (!(sender is System.Windows.Controls.TextBox textBox))
            {
                return;
            }

            if (!e.DataObject.GetDataPresent(DataFormats.UnicodeText))
            {
                e.CancelCommand();
                return;
            }

            var pastedText = e.DataObject.GetData(DataFormats.UnicodeText) as string;
            if (!IsInputAllowed(textBox, pastedText))
            {
                e.CancelCommand();
            }
        }

        private bool IsInputAllowed(System.Windows.Controls.TextBox textBox, string insertedText)
        {
            var candidate = YamlIntegerInputRules.BuildCandidate(
                textBox?.Text,
                textBox == null ? 0 : textBox.SelectionStart,
                textBox == null ? 0 : textBox.SelectionLength,
                insertedText);

            return YamlIntegerInputRules.IsCandidateValid(candidate, MinValue, MaxValue);
        }

        private void DetachEditorHandlers()
        {
            if (_editor == null)
            {
                return;
            }

            DataObject.RemovePastingHandler(_editor, OnEditorPaste);
            _editor.PreviewTextInput -= OnEditorPreviewTextInput;
            _editor.PreviewKeyDown -= OnEditorPreviewKeyDown;
            _editor = null;
        }
    }
}
