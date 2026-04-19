using System.Windows;
using System.Windows.Controls;
using PhialeTech.YamlApp.Abstractions.Enums;

namespace PhialeTech.YamlApp.Wpf.Controls.Actions
{
    public sealed class YamlDocumentActionButton : Button
    {
        public static readonly DependencyProperty SemanticProperty =
            DependencyProperty.Register(
                nameof(Semantic),
                typeof(ActionSemantic?),
                typeof(YamlDocumentActionButton),
                new FrameworkPropertyMetadata(null));

        public static readonly DependencyProperty IsPrimaryActionProperty =
            DependencyProperty.Register(
                nameof(IsPrimaryAction),
                typeof(bool),
                typeof(YamlDocumentActionButton),
                new FrameworkPropertyMetadata(false));

        static YamlDocumentActionButton()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(YamlDocumentActionButton), new FrameworkPropertyMetadata(typeof(YamlDocumentActionButton)));
        }

        public ActionSemantic? Semantic
        {
            get => (ActionSemantic?)GetValue(SemanticProperty);
            set => SetValue(SemanticProperty, value);
        }

        public bool IsPrimaryAction
        {
            get => (bool)GetValue(IsPrimaryActionProperty);
            set => SetValue(IsPrimaryActionProperty, value);
        }
    }
}
