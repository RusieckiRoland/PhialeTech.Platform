using System.Linq;

namespace PhialeTech.DocumentEditor.Abstractions
{
    public sealed class DocumentEditorToolbarConfig
    {
        public DocumentEditorToolbarItem[] Items { get; set; } = new DocumentEditorToolbarItem[0];

        public DocumentEditorToolbarConfig Clone()
        {
            return new DocumentEditorToolbarConfig
            {
                Items = (Items ?? new DocumentEditorToolbarItem[0])
                    .Select(item => new DocumentEditorToolbarItem
                    {
                        Command = item.Command,
                        IsVisible = item.IsVisible,
                        Order = item.Order,
                    })
                    .ToArray()
            };
        }

        public static DocumentEditorToolbarConfig CreateDefault()
        {
            var commands = new[]
            {
                DocumentEditorCommand.Undo,
                DocumentEditorCommand.Redo,
                DocumentEditorCommand.Paragraph,
                DocumentEditorCommand.Heading1,
                DocumentEditorCommand.Heading2,
                DocumentEditorCommand.Heading3,
                DocumentEditorCommand.FontFamily,
                DocumentEditorCommand.FontSize,
                DocumentEditorCommand.LineHeight,
                DocumentEditorCommand.Bold,
                DocumentEditorCommand.Italic,
                DocumentEditorCommand.Underline,
                DocumentEditorCommand.Strike,
                DocumentEditorCommand.BulletList,
                DocumentEditorCommand.OrderedList,
                DocumentEditorCommand.Blockquote,
                DocumentEditorCommand.HorizontalRule,
                DocumentEditorCommand.ClearFormatting,
                DocumentEditorCommand.TextColor,
                DocumentEditorCommand.HighlightColor,
                DocumentEditorCommand.AlignLeft,
                DocumentEditorCommand.AlignCenter,
                DocumentEditorCommand.AlignRight,
                DocumentEditorCommand.AlignJustify,
                DocumentEditorCommand.ExportHtml,
                DocumentEditorCommand.ExportMarkdown,
                DocumentEditorCommand.SaveJson,
                DocumentEditorCommand.LoadJson,
            };

            return new DocumentEditorToolbarConfig
            {
                Items = commands
                    .Select((command, index) => new DocumentEditorToolbarItem
                    {
                        Command = command,
                        IsVisible = true,
                        Order = index,
                    })
                    .ToArray()
            };
        }
    }
}
