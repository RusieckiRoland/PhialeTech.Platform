import {
  BASIC_COLOR_SWATCHES,
  BORDER_STYLE_OPTIONS,
  BOX_SPACING_UNITS,
  DISPLAY_OPTIONS,
  FONT_FAMILY_OPTIONS,
  FONT_STYLE_OPTIONS,
  FONT_WEIGHT_OPTIONS,
  SIZE_UNITS,
  TEXT_ALIGN_OPTIONS,
  TEXT_DECORATION_OPTIONS,
  composeBorderValue,
  composeBoxSpacingValue,
  composeLengthValue,
  getDimensionOptions,
  normalizeHexColor,
  parseBorderValue,
  parseBoxSpacingValue,
  parseLengthValue,
  toColorInputValue,
} from "./report-designer-style-controls.js";
import {
  REPORT_SECTION_KEYS,
  SPECIAL_FIELD_KINDS,
  clampColumnCount,
  formatFieldListText,
  formatTableColumnsText,
  getSectionKeyFromTargetId,
  getSectionTargetId,
  isSectionTargetId,
  isSelectionTargetSelected,
  parseFieldListText,
  parseTableColumnsText,
  resolveActionTarget,
  resolveSpecialFieldValue,
  shouldRenderPageSection,
} from "./report-designer-definition-helpers.js";

const shellElement = typeof document !== "undefined"
  ? document.getElementById("shell")
  : null;

const BLOCK_TYPES = [
  "Container",
  "Columns",
  "FieldList",
  "SpecialField",
  "Text",
  "Image",
  "Table",
  "Repeater",
  "PageBreak",
  "Barcode",
  "QrCode",
];

const FORMAT_KINDS = ["text", "date", "number", "currency"];
const BARCODE_TYPES = ["Code128", "EAN13", "EAN8", "Code39"];
const QR_ERROR_LEVELS = ["L", "M", "Q", "H"];
const COLUMN_COUNT_OPTIONS = [2, 3];
const MIN_PANEL_SIZES = {
  left: 220,
  right: 280,
  center: 460,
  splitter: 12,
};

const SELECTED_PAGE_ID = "__page__";
const SECTION_DEFINITIONS = [
  { key: "reportHeader", labelKey: "Section.ReportHeader", copyKey: "Section.Copy.ReportHeader" },
  { key: "pageHeader", labelKey: "Section.PageHeader", copyKey: "Section.Copy.PageHeader", pageSection: true },
  { key: "body", labelKey: "Section.Body", copyKey: "Section.Copy.Body" },
  { key: "pageFooter", labelKey: "Section.PageFooter", copyKey: "Section.Copy.PageFooter", pageSection: true },
  { key: "reportFooter", labelKey: "Section.ReportFooter", copyKey: "Section.Copy.ReportFooter" },
];

const STYLE_FIELDS = [
  "margin",
  "padding",
  "fontFamily",
  "fontSize",
  "fontWeight",
  "fontStyle",
  "textDecoration",
  "textAlign",
  "color",
  "backgroundColor",
  "border",
  "borderRadius",
  "width",
  "minWidth",
  "maxWidth",
  "height",
  "display",
];

const SHELL_FIELD_SELECTOR = "[data-page-field],[data-section-field],[data-block-field],[data-style-field],[data-format-field],[data-columns-field],[data-fields-field],[data-border-field],[data-length-field],[data-spacing-field],[data-color-picker]";

const TRANSLATIONS = {
  en: {
    "App.Title": "PhialeTech ReportDesigner",
    "Panel.Blocks.Title": "Blocks",
    "Panel.Tab.Blocks": "Blocks",
    "Panel.Tab.Schema": "Data schema",
    "Panel.Blocks.Copy": "Add controlled PhialeTech report blocks. When a container or repeater is selected, new blocks are appended there.",
    "Panel.Blocks.HelpTitle": "How blocks work",
    "Panel.Blocks.HelpBody": "Palette buttons add a root block or append a new block to the currently selected container, repeater or column.",
    "Panel.Blocks.HelpExample": "Example: select Header left column, then click Field list to append invoice details there.",
    "Panel.Schema.Title": "Data schema",
    "Panel.Schema.Copy": "Bindings stay intentionally simple: single field paths and one collection level for tables or repeaters.",
    "Panel.Schema.HelpTitle": "How data schema works",
    "Panel.Schema.HelpBody": "Bindings use simple field paths. Tables and repeaters work with one collection level so the model stays predictable.",
    "Panel.Schema.HelpExample": "Example: InvoiceNumber, Buyer.Name and Items are valid bindings. Table can use Items, and a row can use Name or UnitPrice.",
    "Panel.Schema.Empty": "Set a schema from .NET to expose available fields and collections.",
    "Schema.CopyPath": "Copy path",
    "Schema.Copied": "Copied",
    "Panel.Inspector.Title": "Inspector",
    "Panel.Inspector.Copy": "Edit page settings, binding paths and the small controlled styling surface for the selected block.",
    "Section.ReportHeader": "Report header",
    "Section.Body": "Body",
    "Section.ReportFooter": "Report footer",
    "Section.PageHeader": "Page header",
    "Section.PageFooter": "Page footer",
    "Section.Copy.ReportHeader": "Printed once at the beginning of the report.",
    "Section.Copy.Body": "Main report content that flows across pages.",
    "Section.Copy.ReportFooter": "Printed once at the end of the report.",
    "Section.Copy.PageHeader": "Printed at the top of each page.",
    "Section.Copy.PageFooter": "Printed at the bottom of each page.",
    "Inspector.Section.SelectedNote": "Selected area: {0}. Add blocks here or change page-section rules.",
    "Inspector.Section.Options": "Area settings",
    "Inspector.Section.SkipFirstPage": "Skip first page",
    "Inspector.Section.SkipLastPage": "Skip last page",
    "Surface.Title.Design": "Design surface",
    "Surface.Title.Preview": "Preview surface",
    "Surface.Subtitle.Design": "Design stays limited to controlled report blocks so ReportDefinition remains the single public source of truth.",
    "Surface.Subtitle.Preview": "Preview uses report data first and falls back to sample data. Printing always comes from this paged preview.",
    "Surface.PageSummary": "{0} | {1} | margin {2}",
    "Surface.Empty.Design": "No report blocks yet. Start with Text, Container or Table from the left palette.",
    "Surface.Empty.Preview": "Preview is empty. Add at least one visible report block in design mode.",
    "Inspector.SelectedNote": "Selected block type: {0}. Changes are mapped back to the neutral ReportDefinition model.",
    "Inspector.Empty": "Select a block in the design surface to edit its binding, formatting and style.",
    "Inspector.Page.SelectedNote": "Selected object: page. Click a report block to switch back to block settings.",
    "Inspector.Page.Title": "Page",
    "Inspector.Format.Title": "Format",
    "Inspector.Columns.Title": "Columns",
    "Inspector.Columns.Copy": "One line per column: Header|Binding|Kind|Pattern|Currency|Decimals|Width|Align",
    "Inspector.FieldList.Title": "Field list",
    "Inspector.FieldList.Copy": "One line per field: Label|Binding|Kind|Pattern|Currency|Decimals",
    "Inspector.ColumnsBlock.Title": "Columns layout",
    "Inspector.ColumnsBlock.Count": "Column count",
    "Inspector.ColumnsBlock.Gap": "Column gap",
    "Inspector.ColumnsBlock.Hint": "Columns stay controlled: select an inner column container and append report blocks there.",
    "Inspector.SpecialField.Title": "Automatic field",
    "Inspector.SpecialField.Kind": "Field kind",
    "Inspector.Pagination.Title": "Pagination",
    "Inspector.Pagination.PageBreakBefore": "Start on a new page",
    "Inspector.Pagination.KeepTogether": "Keep block together",
    "Inspector.Pagination.RepeatHeader": "Repeat table header on each page",
    "Inspector.Style.Title": "Style",
    "Inspector.Barcode.Title": "Barcode",
    "Inspector.QrCode.Title": "QR code",
    "Inspector.Page.Size": "Size",
    "Inspector.Page.Orientation": "Orientation",
    "Inspector.Page.Margin": "Margin",
    "Inspector.Page.Orientation.Portrait": "Portrait",
    "Inspector.Page.Orientation.Landscape": "Landscape",
    "Inspector.Block.Name": "Block name",
    "Inspector.Block.Text": "Static text",
    "Inspector.Block.Binding": "Binding",
    "Inspector.Block.ImageSource": "Image source",
    "Inspector.Block.ItemsSource": "Items source",
    "Inspector.Format.Kind": "Format kind",
    "Inspector.Format.Pattern": "Pattern",
    "Inspector.Format.Currency": "Currency",
    "Inspector.Format.Decimals": "Decimals",
    "Inspector.Barcode.Type": "Barcode type",
    "Inspector.Barcode.ShowText": "Show human-readable text",
    "Inspector.QrCode.Size": "Size",
    "Inspector.QrCode.ErrorCorrection": "Error correction level",
    "Inspector.Style.Margin": "Margin",
    "Inspector.Style.Padding": "Padding",
    "Inspector.Style.FontFamily": "Font family",
    "Inspector.Style.FontSize": "Font size",
    "Inspector.Style.FontWeight": "Font weight",
    "Inspector.Style.FontStyle": "Font style",
    "Inspector.Style.TextDecoration": "Text decoration",
    "Inspector.Style.TextAlign": "Text align",
    "Inspector.Style.Color": "Color",
    "Inspector.Style.BackgroundColor": "Background",
    "Inspector.Style.Border": "Border",
    "Inspector.Style.BorderRadius": "Border radius",
    "Inspector.Style.Width": "Width",
    "Inspector.Style.MinWidth": "Min width",
    "Inspector.Style.MaxWidth": "Max width",
    "Inspector.Style.Height": "Height",
    "Inspector.Style.Display": "Display",
    "Inspector.Style.SpacingUnit": "Unit",
    "Inspector.Style.BorderSummary": "Current border",
    "Inspector.Style.BorderWidth": "Border width",
    "Inspector.Style.BorderStyle": "Border style",
    "Inspector.Style.BorderColor": "Border color",
    "Inspector.Style.Border.Enabled": "Border enabled",
    "Inspector.Style.Side.Top": "Top",
    "Inspector.Style.Side.Right": "Right",
    "Inspector.Style.Side.Bottom": "Bottom",
    "Inspector.Style.Side.Left": "Left",
    "Placeholder.ImageSource": "URL or data URI",
    "Placeholder.PageMargin": "20mm",
    "Placeholder.DatePattern": "yyyy-MM-dd",
    "Placeholder.Currency": "PLN",
    "Placeholder.Size": "140px",
    "Placeholder.BorderColor": "#0f766e",
    "Action.MoveUp": "Up",
    "Action.MoveDown": "Down",
    "Action.Delete": "Delete",
    "Action.ClearSelection": "Clear selection",
    "Action.Help": "Help",
    "Action.CloseHelp": "Close help",
    "Help.ExampleLabel": "Mini example",
    "Help.Column.Design": "In design",
    "Help.Column.Preview": "In preview",
    "Help.Blocks.Design.Container": "Container or Columns",
    "Help.Blocks.Design.Container.Detail": "Use these when you want to place other blocks inside a controlled layout.",
    "Help.Blocks.Preview.Container": "Becomes a layout wrapper",
    "Help.Blocks.Preview.Container.Detail": "It does not print as a special widget. It only arranges child content on the page.",
    "Help.Blocks.Design.FieldList": "Field list",
    "Help.Blocks.Design.FieldList.Detail": "Good for invoice details, seller and buyer sections or payment summary.",
    "Help.Blocks.Preview.FieldList": "Renders label-value rows",
    "Help.Blocks.Preview.FieldList.Detail": "Each configured field becomes one visible row in the printed document.",
    "Help.Blocks.Design.Table": "Table or Repeater",
    "Help.Blocks.Design.Table.Detail": "Bind them to a collection such as Items or Notes.",
    "Help.Blocks.Preview.Table": "Repeats data rows",
    "Help.Blocks.Preview.Table.Detail": "Preview shows one row or repeated block per item from the selected collection.",
    "Help.Blocks.Design.Code": "Barcode or QR code",
    "Help.Blocks.Design.Code.Detail": "Pick a binding that contains the value you want to encode.",
    "Help.Blocks.Preview.Code": "Draws a scannable code",
    "Help.Blocks.Preview.Code.Detail": "Preview and print render the code image from the current bound value.",
    "Help.Schema.Design.Paths": "Binding path",
    "Help.Schema.Design.Paths.Detail": "Choose simple paths such as InvoiceNumber, Buyer.Name or Items.",
    "Help.Schema.Preview.Paths": "Resolved value",
    "Help.Schema.Preview.Paths.Detail": "Preview reads the value from JSON and shows it in text, field list, barcode or QR blocks.",
    "Help.Schema.Design.Collection": "Collection binding",
    "Help.Schema.Design.Collection.Detail": "Tables and repeaters can point to one collection level, for example Items.",
    "Help.Schema.Preview.Collection": "Rendered list",
    "Help.Schema.Preview.Collection.Detail": "Each item from the collection becomes one row or repeated card in preview and print.",
    "Action.AddRootHint": "Palette buttons add a root block or append to the selected container, repeater or column.",
    "Preview.EmptyRows": "No rows available.",
    "Preview.EmptyRepeater": "No items available for repeater.",
    "Preview.EmptyImage": "Image source is empty.",
    "Preview.Barcode.Empty": "Barcode value is empty.",
    "Preview.Barcode.Invalid": "Invalid barcode value for {0}.",
    "Preview.QrCode.Empty": "QR code value is empty.",
    "Preview.QrCode.Invalid": "Failed to render QR code.",
    "Meta.Binding": "Binding",
    "Meta.Text": "Text",
    "Meta.Source": "Source",
    "Meta.ItemsSource": "Items source",
    "Meta.Columns": "Columns",
    "Meta.ColumnCount": "Columns",
    "Meta.Children": "Children",
    "Meta.Fields": "Fields",
    "Meta.SpecialField": "Automatic field",
    "Meta.PageBreak": "Hard page split in preview and print.",
    "Meta.Controlled": "Controlled block with neutral ReportDefinition mapping.",
    "Schema.Collection": "collection",
    "Option.Default": "Use default",
    "Option.FontWeight.Normal": "Normal (400)",
    "Option.FontWeight.Medium": "Medium (500)",
    "Option.FontWeight.Semibold": "Semibold (600)",
    "Option.FontWeight.Bold": "Bold (700)",
    "Option.FontStyle.Normal": "Normal",
    "Option.FontStyle.Italic": "Italic",
    "Option.FontStyle.Oblique": "Oblique",
    "Option.TextDecoration.None": "None",
    "Option.TextDecoration.Underline": "Underline",
    "Option.TextDecoration.LineThrough": "Line through",
    "Option.TextAlign.Left": "Left",
    "Option.TextAlign.Center": "Center",
    "Option.TextAlign.Right": "Right",
    "Option.TextAlign.Justify": "Justify",
    "Option.Display.Block": "Block",
    "Option.Display.InlineBlock": "Inline block",
    "Option.Display.Flex": "Flex",
    "Option.Display.Grid": "Grid",
    "Option.BorderStyle.Solid": "Solid",
    "Option.BorderStyle.Dashed": "Dashed",
    "Option.BorderStyle.Dotted": "Dotted",
    "Option.BorderStyle.Double": "Double",
    "Option.Dimension.Auto": "Auto",
    "Option.Dimension.FullWidth": "Full width",
    "Option.Dimension.None": "None",
    "Option.ColumnCount.Two": "2 columns",
    "Option.ColumnCount.Three": "3 columns",
    "Option.TableAlign.Left": "Left",
    "Option.TableAlign.Center": "Center",
    "Option.TableAlign.Right": "Right",
    "Option.SpecialField.CurrentDate": "Current date",
    "Option.SpecialField.PageNumber": "Page number",
    "Option.SpecialField.TotalPages": "Total pages",
    "Option.SpecialField.PageNumberOfTotalPages": "Page X of Y",
    "Format.text": "Text",
    "Format.date": "Date",
    "Format.number": "Number",
    "Format.currency": "Currency",
    "BlockType.Container": "Container",
    "BlockType.Columns": "Columns",
    "BlockType.FieldList": "Field list",
    "BlockType.SpecialField": "Automatic field",
    "BlockType.Text": "Text",
    "BlockType.Image": "Image",
    "BlockType.Table": "Table",
    "BlockType.Repeater": "Repeater",
    "BlockType.PageBreak": "Page break",
    "BlockType.Barcode": "Barcode",
    "BlockType.QrCode": "QR code",
    "Default.BlockName.Container": "Container block",
    "Default.BlockName.Columns": "Columns block",
    "Default.BlockName.FieldList": "Field list block",
    "Default.BlockName.SpecialField": "Automatic field",
    "Default.BlockName.Text": "Text block",
    "Default.BlockName.Image": "Image block",
    "Default.BlockName.Table": "Table block",
    "Default.BlockName.Repeater": "Repeater block",
    "Default.BlockName.PageBreak": "Page break",
    "Default.BlockName.Barcode": "Barcode block",
    "Default.BlockName.QrCode": "QR code block",
    "Default.Text": "Editable report text",
    "Default.Table.Name": "Name",
    "Default.Table.Quantity": "Quantity",
    "Default.Table.LineTotal": "Line total",
    "Default.FieldList.InvoiceNumber": "Invoice number",
    "Default.FieldList.InvoiceDate": "Invoice date",
    "Default.ColumnContainer": "Column {0}",
    "Default.RepeaterItemText": "Repeater item text",
    "Error.Bridge": "ReportDesigner bridge failed.",
    "Error.Unexpected": "Unexpected error in ReportDesigner.",
  },
  pl: {
    "App.Title": "PhialeTech ReportDesigner",
    "Panel.Blocks.Title": "Bloki",
    "Panel.Tab.Blocks": "Bloki",
    "Panel.Tab.Schema": "Schemat danych",
    "Panel.Blocks.Copy": "Dodawaj kontrolowane bloki raportowe PhialeTech. Gdy zaznaczony jest kontener albo repeater, nowe bloki są dopinane właśnie tam.",
    "Panel.Blocks.HelpTitle": "Jak działają bloki",
    "Panel.Blocks.HelpBody": "Przyciski palety dodają blok główny albo dopinają nowy blok do aktualnie zaznaczonego kontenera, repeatera albo kolumny.",
    "Panel.Blocks.HelpExample": "Przykład: zaznacz Header left column, a potem kliknij Lista pól, aby dopiąć tam dane faktury.",
    "Panel.Schema.Title": "Schemat danych",
    "Panel.Schema.Copy": "Binding pozostaje celowo prosty: pojedyncze ścieżki pól i jeden poziom kolekcji dla tabel albo repeaterów.",
    "Panel.Schema.HelpTitle": "Jak działa schemat danych",
    "Panel.Schema.HelpBody": "Binding używa prostych ścieżek pól. Tabele i repeatery pracują na jednym poziomie kolekcji, dzięki czemu model pozostaje przewidywalny.",
    "Panel.Schema.HelpExample": "Przykład: InvoiceNumber, Buyer.Name i Items są poprawnymi ścieżkami. Tabela może używać Items, a wiersz może używać Name lub UnitPrice.",
    "Panel.Schema.Empty": "Ustaw schemat z .NET, aby pokazać dostępne pola i kolekcje.",
    "Schema.CopyPath": "Kopiuj ścieżkę",
    "Schema.Copied": "Skopiowano",
    "Panel.Inspector.Title": "Inspektor",
    "Panel.Inspector.Copy": "Edytuj ustawienia strony, ścieżki bindingów i kontrolowany zestaw stylów dla zaznaczonego bloku.",
    "Section.ReportHeader": "Nagłówek raportu",
    "Section.Body": "Treść główna",
    "Section.ReportFooter": "Stopka raportu",
    "Section.PageHeader": "Nagłówek strony",
    "Section.PageFooter": "Stopka strony",
    "Section.Copy.ReportHeader": "Drukowany raz, na początku całego raportu.",
    "Section.Copy.Body": "Główna treść raportu, która przechodzi przez kolejne strony.",
    "Section.Copy.ReportFooter": "Drukowany raz, na końcu całego raportu.",
    "Section.Copy.PageHeader": "Drukowany na górze każdej strony.",
    "Section.Copy.PageFooter": "Drukowany na dole każdej strony.",
    "Inspector.Section.SelectedNote": "Zaznaczony obszar: {0}. Dodawaj tu bloki albo zmieniaj reguły sekcji strony.",
    "Inspector.Section.Options": "Ustawienia obszaru",
    "Inspector.Section.SkipFirstPage": "Pomijaj pierwszą stronę",
    "Inspector.Section.SkipLastPage": "Pomijaj ostatnią stronę",
    "Surface.Title.Design": "Powierzchnia projektowania",
    "Surface.Title.Preview": "Powierzchnia podglądu",
    "Surface.Subtitle.Design": "Projektowanie pozostaje ograniczone do kontrolowanych bloków raportu, dzięki czemu ReportDefinition nadal jest jedynym publicznym source of truth.",
    "Surface.Subtitle.Preview": "Podgląd używa najpierw danych raportowych, a potem danych przykładowych. Wydruk zawsze wychodzi z tego samego podglądu stron.",
    "Surface.PageSummary": "{0} | {1} | margines {2}",
    "Surface.Empty.Design": "Nie ma jeszcze bloków raportu. Zacznij od Text, Container albo Table z panelu po lewej stronie.",
    "Surface.Empty.Preview": "Podgląd jest pusty. Dodaj przynajmniej jeden widoczny blok raportu w trybie projektowania.",
    "Inspector.SelectedNote": "Zaznaczony typ bloku: {0}. Zmiany są mapowane z powrotem do neutralnego modelu ReportDefinition.",
    "Inspector.Empty": "Zaznacz blok na powierzchni projektowania, aby edytować jego binding, format i styl.",
    "Inspector.Page.SelectedNote": "Zaznaczony obiekt: strona. Kliknij blok raportu, aby wrócić do ustawień bloku.",
    "Inspector.Page.Title": "Strona",
    "Inspector.Format.Title": "Format",
    "Inspector.Columns.Title": "Kolumny",
    "Inspector.Columns.Copy": "Jedna linia na kolumnę: Header|Binding|Kind|Pattern|Currency|Decimals|Width|Align",
    "Inspector.FieldList.Title": "Lista pól",
    "Inspector.FieldList.Copy": "Jedna linia na pole: Label|Binding|Kind|Pattern|Currency|Decimals",
    "Inspector.ColumnsBlock.Title": "Układ kolumn",
    "Inspector.ColumnsBlock.Count": "Liczba kolumn",
    "Inspector.ColumnsBlock.Gap": "Odstęp kolumn",
    "Inspector.ColumnsBlock.Hint": "Kolumny pozostają kontrolowane: zaznacz wewnętrzny kontener kolumny i dopinaj tam bloki raportu.",
    "Inspector.SpecialField.Title": "Pole automatyczne",
    "Inspector.SpecialField.Kind": "Rodzaj pola",
    "Inspector.Pagination.Title": "Paginacja",
    "Inspector.Pagination.PageBreakBefore": "Zacznij od nowej strony",
    "Inspector.Pagination.KeepTogether": "Trzymaj blok razem",
    "Inspector.Pagination.RepeatHeader": "Powtarzaj nagłówek tabeli na każdej stronie",
    "Inspector.Style.Title": "Styl",
    "Inspector.Barcode.Title": "Kod kreskowy",
    "Inspector.QrCode.Title": "Kod QR",
    "Inspector.Page.Size": "Rozmiar",
    "Inspector.Page.Orientation": "Orientacja",
    "Inspector.Page.Margin": "Margines",
    "Inspector.Page.Orientation.Portrait": "Pionowo",
    "Inspector.Page.Orientation.Landscape": "Poziomo",
    "Inspector.Block.Name": "Nazwa bloku",
    "Inspector.Block.Text": "Tekst statyczny",
    "Inspector.Block.Binding": "Binding",
    "Inspector.Block.ImageSource": "Źródło obrazu",
    "Inspector.Block.ItemsSource": "Items source",
    "Inspector.Format.Kind": "Rodzaj formatu",
    "Inspector.Format.Pattern": "Wzorzec",
    "Inspector.Format.Currency": "Waluta",
    "Inspector.Format.Decimals": "Miejsca dziesiętne",
    "Inspector.Barcode.Type": "Typ kodu kreskowego",
    "Inspector.Barcode.ShowText": "Pokaż tekst czytelny dla człowieka",
    "Inspector.QrCode.Size": "Rozmiar",
    "Inspector.QrCode.ErrorCorrection": "Poziom korekcji błędów",
    "Inspector.Style.Margin": "Margines",
    "Inspector.Style.Padding": "Padding",
    "Inspector.Style.FontFamily": "Rodzina fontu",
    "Inspector.Style.FontSize": "Rozmiar fontu",
    "Inspector.Style.FontWeight": "Grubość fontu",
    "Inspector.Style.FontStyle": "Styl fontu",
    "Inspector.Style.TextDecoration": "Dekoracja tekstu",
    "Inspector.Style.TextAlign": "Wyrównanie tekstu",
    "Inspector.Style.Color": "Kolor",
    "Inspector.Style.BackgroundColor": "Tło",
    "Inspector.Style.Border": "Obramowanie",
    "Inspector.Style.BorderRadius": "Zaokrąglenie obramowania",
    "Inspector.Style.Width": "Szerokość",
    "Inspector.Style.MinWidth": "Minimalna szerokość",
    "Inspector.Style.MaxWidth": "Maksymalna szerokość",
    "Inspector.Style.Height": "Wysokość",
    "Inspector.Style.Display": "Display",
    "Inspector.Style.SpacingUnit": "Jednostka",
    "Inspector.Style.BorderSummary": "Aktualne obramowanie",
    "Inspector.Style.BorderWidth": "Grubość obramowania",
    "Inspector.Style.BorderStyle": "Styl obramowania",
    "Inspector.Style.BorderColor": "Kolor obramowania",
    "Inspector.Style.Border.Enabled": "Obramowanie włączone",
    "Inspector.Style.Side.Top": "Góra",
    "Inspector.Style.Side.Right": "Prawo",
    "Inspector.Style.Side.Bottom": "Dół",
    "Inspector.Style.Side.Left": "Lewo",
    "Placeholder.ImageSource": "URL albo data URI",
    "Placeholder.PageMargin": "20mm",
    "Placeholder.DatePattern": "yyyy-MM-dd",
    "Placeholder.Currency": "PLN",
    "Placeholder.Size": "140px",
    "Placeholder.BorderColor": "#0f766e",
    "Action.MoveUp": "W górę",
    "Action.MoveDown": "W dół",
    "Action.Delete": "Usuń",
    "Action.ClearSelection": "Wyczyść zaznaczenie",
    "Action.Help": "Pomoc",
    "Action.CloseHelp": "Zamknij pomoc",
    "Help.ExampleLabel": "Mini przykład",
    "Help.Column.Design": "W designie",
    "Help.Column.Preview": "W podglądzie",
    "Help.Blocks.Design.Container": "Container albo Columns",
    "Help.Blocks.Design.Container.Detail": "Używaj ich wtedy, gdy chcesz układać inne bloki wewnątrz kontrolowanego layoutu.",
    "Help.Blocks.Preview.Container": "Staje się układem pomocniczym",
    "Help.Blocks.Preview.Container.Detail": "Nie drukuje się jako osobny widget. Porządkuje tylko zawartość dzieci na stronie.",
    "Help.Blocks.Design.FieldList": "Lista pól",
    "Help.Blocks.Design.FieldList.Detail": "Dobra do danych faktury, sekcji sprzedawcy i nabywcy albo podsumowania płatności.",
    "Help.Blocks.Preview.FieldList": "Renderuje wiersze etykieta-wartość",
    "Help.Blocks.Preview.FieldList.Detail": "Każde skonfigurowane pole staje się jednym widocznym wierszem dokumentu.",
    "Help.Blocks.Design.Table": "Table albo Repeater",
    "Help.Blocks.Design.Table.Detail": "Podpinaj je do kolekcji takiej jak Items albo Notes.",
    "Help.Blocks.Preview.Table": "Powtarza wiersze danych",
    "Help.Blocks.Preview.Table.Detail": "Podgląd pokazuje jeden wiersz albo jeden powtarzany blok dla każdego elementu kolekcji.",
    "Help.Blocks.Design.Code": "Barcode albo QR code",
    "Help.Blocks.Design.Code.Detail": "Wybierz binding, który zawiera wartość do zakodowania.",
    "Help.Blocks.Preview.Code": "Rysuje kod do zeskanowania",
    "Help.Blocks.Preview.Code.Detail": "Podgląd i wydruk renderują obraz kodu na podstawie aktualnej wartości z bindingu.",
    "Help.Schema.Design.Paths": "Ścieżka bindingu",
    "Help.Schema.Design.Paths.Detail": "Wybieraj proste ścieżki, na przykład InvoiceNumber, Buyer.Name albo Items.",
    "Help.Schema.Preview.Paths": "Rozwiązana wartość",
    "Help.Schema.Preview.Paths.Detail": "Podgląd odczytuje wartość z JSON i pokazuje ją w tekście, liście pól, kodzie kreskowym albo QR.",
    "Help.Schema.Design.Collection": "Binding kolekcji",
    "Help.Schema.Design.Collection.Detail": "Tabele i repeatery mogą wskazywać jeden poziom kolekcji, na przykład Items.",
    "Help.Schema.Preview.Collection": "Wyrenderowana lista",
    "Help.Schema.Preview.Collection.Detail": "Każdy element kolekcji staje się osobnym wierszem albo powtarzanym blokiem w podglądzie i wydruku.",
    "Action.AddRootHint": "Przyciski palety dodają blok główny albo dopinają go do zaznaczonego kontenera, repeatera albo kolumny.",
    "Preview.EmptyRows": "Brak wierszy do pokazania.",
    "Preview.EmptyRepeater": "Brak elementów dla repeatera.",
    "Preview.EmptyImage": "Źródło obrazu jest puste.",
    "Preview.Barcode.Empty": "Wartość kodu kreskowego jest pusta.",
    "Preview.Barcode.Invalid": "Nieprawidłowa wartość dla kodu {0}.",
    "Preview.QrCode.Empty": "Wartość kodu QR jest pusta.",
    "Preview.QrCode.Invalid": "Nie udało się wyrenderować kodu QR.",
    "Meta.Binding": "Binding",
    "Meta.Text": "Tekst",
    "Meta.Source": "Źródło",
    "Meta.ItemsSource": "Items source",
    "Meta.Columns": "Kolumny",
    "Meta.ColumnCount": "Liczba kolumn",
    "Meta.Children": "Dzieci",
    "Meta.Fields": "Pola",
    "Meta.SpecialField": "Pole automatyczne",
    "Meta.PageBreak": "Twardy podział strony w podglądzie i na wydruku.",
    "Meta.Controlled": "Kontrolowany blok z neutralnym mapowaniem do ReportDefinition.",
    "Schema.Collection": "kolekcja",
    "Option.Default": "Użyj domyślnego",
    "Option.FontWeight.Normal": "Normalna (400)",
    "Option.FontWeight.Medium": "Średnia (500)",
    "Option.FontWeight.Semibold": "Półgruba (600)",
    "Option.FontWeight.Bold": "Pogrubiona (700)",
    "Option.FontStyle.Normal": "Normalny",
    "Option.FontStyle.Italic": "Kursywa",
    "Option.FontStyle.Oblique": "Pochylony",
    "Option.TextDecoration.None": "Brak",
    "Option.TextDecoration.Underline": "Podkreślenie",
    "Option.TextDecoration.LineThrough": "Przekreślenie",
    "Option.TextAlign.Left": "Do lewej",
    "Option.TextAlign.Center": "Do środka",
    "Option.TextAlign.Right": "Do prawej",
    "Option.TextAlign.Justify": "Wyjustuj",
    "Option.Display.Block": "Blok",
    "Option.Display.InlineBlock": "Inline block",
    "Option.Display.Flex": "Flex",
    "Option.Display.Grid": "Grid",
    "Option.BorderStyle.Solid": "Ciągłe",
    "Option.BorderStyle.Dashed": "Kreskowane",
    "Option.BorderStyle.Dotted": "Kropkowane",
    "Option.BorderStyle.Double": "Podwójne",
    "Option.Dimension.Auto": "Auto",
    "Option.Dimension.FullWidth": "Pełna szerokość",
    "Option.Dimension.None": "Brak",
    "Option.ColumnCount.Two": "2 kolumny",
    "Option.ColumnCount.Three": "3 kolumny",
    "Option.TableAlign.Left": "Do lewej",
    "Option.TableAlign.Center": "Do środka",
    "Option.TableAlign.Right": "Do prawej",
    "Option.SpecialField.CurrentDate": "Bieżąca data",
    "Option.SpecialField.PageNumber": "Numer strony",
    "Option.SpecialField.TotalPages": "Liczba stron",
    "Option.SpecialField.PageNumberOfTotalPages": "Strona X z Y",
    "Format.text": "Tekst",
    "Format.date": "Data",
    "Format.number": "Liczba",
    "Format.currency": "Waluta",
    "BlockType.Container": "Kontener",
    "BlockType.Columns": "Kolumny",
    "BlockType.FieldList": "Lista pól",
    "BlockType.SpecialField": "Pole automatyczne",
    "BlockType.Text": "Tekst",
    "BlockType.Image": "Obraz",
    "BlockType.Table": "Tabela",
    "BlockType.Repeater": "Repeater",
    "BlockType.PageBreak": "Podział strony",
    "BlockType.Barcode": "Kod kreskowy",
    "BlockType.QrCode": "Kod QR",
    "Default.BlockName.Container": "Blok kontenera",
    "Default.BlockName.Columns": "Blok kolumn",
    "Default.BlockName.FieldList": "Blok listy pól",
    "Default.BlockName.SpecialField": "Pole automatyczne",
    "Default.BlockName.Text": "Blok tekstowy",
    "Default.BlockName.Image": "Blok obrazu",
    "Default.BlockName.Table": "Blok tabeli",
    "Default.BlockName.Repeater": "Blok repeatera",
    "Default.BlockName.PageBreak": "Podział strony",
    "Default.BlockName.Barcode": "Blok kodu kreskowego",
    "Default.BlockName.QrCode": "Blok kodu QR",
    "Default.Text": "Edytowalny tekst raportu",
    "Default.Table.Name": "Nazwa",
    "Default.Table.Quantity": "Ilość",
    "Default.Table.LineTotal": "Wartość",
    "Default.FieldList.InvoiceNumber": "Numer faktury",
    "Default.FieldList.InvoiceDate": "Data faktury",
    "Default.ColumnContainer": "Kolumna {0}",
    "Default.RepeaterItemText": "Tekst elementu repeatera",
    "Error.Bridge": "Mostek ReportDesigner zakończył się błędem.",
    "Error.Unexpected": "Wystąpił nieoczekiwany błąd w ReportDesigner.",
  },
};

const state = {
  readyAnnounced: false,
  mode: "design",
  locale: "en",
  theme: "light",
  definition: createDefaultDefinition(),
  schema: { fields: [] },
  sampleData: {},
  reportData: null,
  selectedBlockId: SELECTED_PAGE_ID,
  definitionChangedTimer: 0,
  panelSizes: {
    left: 280,
    right: 320,
  },
  lastPreviewModel: {
    pages: [],
    pageCount: 0,
    usedSampleData: true,
  },
  leftRailTab: "blocks",
  expandedSchemaPaths: Object.create(null),
  copiedSchemaPath: "",
  copiedSchemaTimer: 0,
  helpModalKey: "",
  selectionPulseTargetId: "",
  selectionPulseTimer: 0,
  resizeSession: null,
};

function isPageSelection(selectionId = state.selectedBlockId) {
  return selectionId === SELECTED_PAGE_ID || !selectionId;
}

function isSectionSelection(selectionId = state.selectedBlockId) {
  return isSectionTargetId(selectionId);
}

function getSelectedSectionKey(selectionId = state.selectedBlockId) {
  return getSectionKeyFromTargetId(selectionId);
}

function createDefaultDefinition() {
  return {
    version: 1,
    page: {
      size: "A4",
      orientation: "Portrait",
      margin: "20mm",
    },
    reportHeaderBlocks: [],
    blocks: [],
    reportFooterBlocks: [],
    pageHeaderBlocks: [],
    pageHeaderSkipFirstPage: false,
    pageHeaderSkipLastPage: false,
    pageFooterBlocks: [],
    pageFooterSkipFirstPage: false,
    pageFooterSkipLastPage: false,
    editorMetadata: "",
  };
}

function createEmptyStyle() {
  return {
    margin: "",
    padding: "",
    fontFamily: "",
    fontSize: "",
    fontWeight: "",
    fontStyle: "",
    textDecoration: "",
    textAlign: "",
    color: "",
    backgroundColor: "",
    border: "",
    borderRadius: "",
    width: "",
    minWidth: "",
    maxWidth: "",
    height: "",
    display: "",
  };
}

function createEmptyFormat(kind = "text") {
  return {
    kind,
    pattern: "",
    currency: "",
    decimals: null,
  };
}

function createBlock(type) {
  const normalizedType = normalizeBlockType(type);
  const block = {
    id: buildId(normalizedType),
    type: normalizedType,
    name: getDefaultBlockName(normalizedType),
    text: "",
    binding: "",
    imageSource: "",
    itemsSource: "",
    columnCount: 0,
    columnGap: "",
    barcodeType: "Code128",
    showText: true,
    size: "",
    errorCorrectionLevel: "M",
    specialFieldKind: "",
    pageBreakBefore: false,
    keepTogether: false,
    repeatHeader: true,
    format: createEmptyFormat(),
    style: createEmptyStyle(),
    columns: [],
    fields: [],
    children: [],
  };

  if (normalizedType === "Container") {
    block.style.padding = "18px";
    block.style.backgroundColor = "#F8FAFC";
    block.style.border = "1px solid rgba(203, 213, 225, 0.82)";
    block.style.borderRadius = "16px";
  } else if (normalizedType === "Text") {
    block.text = t("Default.Text");
    block.style.fontSize = "18px";
    block.style.fontWeight = "600";
  } else if (normalizedType === "Columns") {
    block.columnCount = 2;
    block.columnGap = "18px";
    block.children = createColumnChildren(block.columnCount);
    block.keepTogether = true;
  } else if (normalizedType === "FieldList") {
    block.fields = [
      {
        label: t("Default.FieldList.InvoiceNumber"),
        binding: "InvoiceNumber",
        format: createEmptyFormat(),
      },
      {
        label: t("Default.FieldList.InvoiceDate"),
        binding: "InvoiceDate",
        format: {
          kind: "date",
          pattern: "yyyy-MM-dd",
          currency: "",
          decimals: null,
        },
      },
    ];
    block.keepTogether = true;
  } else if (normalizedType === "Image") {
    block.style.width = "120px";
  } else if (normalizedType === "SpecialField") {
    block.specialFieldKind = "CurrentDate";
    block.format = {
      kind: "date",
      pattern: "yyyy-MM-dd",
      currency: "",
      decimals: null,
    };
  } else if (normalizedType === "Table") {
    block.itemsSource = "Items";
    block.repeatHeader = true;
    block.columns = [
      {
        header: t("Default.Table.Name"),
        binding: "Name",
        width: "",
        textAlign: "",
        format: createEmptyFormat(),
      },
      {
        header: t("Default.Table.Quantity"),
        binding: "Quantity",
        width: "",
        textAlign: "right",
        format: {
          kind: "number",
          pattern: "",
          currency: "",
          decimals: 0,
        },
      },
      {
        header: t("Default.Table.LineTotal"),
        binding: "LineTotal",
        width: "",
        textAlign: "right",
        format: {
          kind: "currency",
          pattern: "",
          currency: "PLN",
          decimals: 2,
        },
      },
    ];
  } else if (normalizedType === "Repeater") {
    block.itemsSource = "Items";
    block.children = [
      {
        id: buildId("Text"),
        type: "Text",
        name: t("Default.RepeaterItemText"),
        text: "",
        binding: "Name",
        imageSource: "",
        itemsSource: "",
        barcodeType: "Code128",
        showText: true,
        size: "",
        errorCorrectionLevel: "M",
        format: createEmptyFormat(),
        style: {
          ...createEmptyStyle(),
          fontSize: "16px",
          fontWeight: "600",
        },
        columns: [],
        children: [],
      },
    ];
  } else if (normalizedType === "Barcode") {
    block.style.width = "260px";
    block.style.height = "82px";
  } else if (normalizedType === "QrCode") {
    block.size = "140px";
  }

  return block;
}

function createColumnChildren(columnCount) {
  return Array.from({ length: clampColumnCount(columnCount) }, (_, index) => createColumnContainer(index));
}

function createColumnContainer(index) {
  return {
    id: buildId("Column"),
    type: "Container",
    name: formatText("Default.ColumnContainer", index + 1),
    text: "",
    binding: "",
    imageSource: "",
    itemsSource: "",
    columnCount: 0,
    columnGap: "",
    barcodeType: "Code128",
    showText: true,
    size: "",
    errorCorrectionLevel: "M",
    specialFieldKind: "",
    pageBreakBefore: false,
    keepTogether: false,
    repeatHeader: true,
    format: createEmptyFormat(),
    style: {
      ...createEmptyStyle(),
      padding: "0",
      backgroundColor: "",
      border: "",
      borderRadius: "",
      display: "grid",
    },
    columns: [],
    fields: [],
    children: [],
  };
}

function buildId(prefix) {
  return `${String(prefix || "block").toLowerCase()}-${Math.random().toString(36).slice(2, 10)}`;
}

function normalizeLocale(locale) {
  const value = String(locale || "").trim().toLowerCase();
  if (!value) {
    return "en";
  }

  const separatorIndex = value.search(/[-_]/);
  const normalized = separatorIndex > 0 ? value.slice(0, separatorIndex) : value;
  return TRANSLATIONS[normalized] ? normalized : "en";
}

function normalizeTheme(theme) {
  return String(theme || "").trim().toLowerCase() === "dark" ? "dark" : "light";
}

function normalizeBlockType(type) {
  return BLOCK_TYPES.includes(type) ? type : "Text";
}

function normalizeBarcodeType(type) {
  return BARCODE_TYPES.includes(type) ? type : "Code128";
}

function normalizeQrErrorLevel(level) {
  return QR_ERROR_LEVELS.includes(level) ? level : "M";
}

function normalizeSpecialFieldKind(kind) {
  return SPECIAL_FIELD_KINDS.includes(kind) ? kind : "CurrentDate";
}

function t(key) {
  const language = TRANSLATIONS[state.locale] || TRANSLATIONS.en;
  return language[key] || TRANSLATIONS.en[key] || key;
}

function formatText(key, ...args) {
  return t(key).replace(/\{(\d+)\}/g, (_, index) => {
    const value = args[Number(index)];
    return value == null ? "" : String(value);
  });
}

function resolveOptionText(option) {
  if (!option) {
    return "";
  }

  if (option.labelKey) {
    return t(option.labelKey);
  }

  return option.label || option.value || "";
}

function renderSectionIcon(iconName) {
  const icons = {
    page: '<svg viewBox="0 0 14 14" aria-hidden="true"><rect x="3" y="2" width="8" height="10" rx="1.5" fill="none" stroke="currentColor" stroke-width="1.2"></rect><path d="M5 5h4M5 7h4M5 9h3" fill="none" stroke="currentColor" stroke-width="1.2" stroke-linecap="round"></path></svg>',
    container: '<svg viewBox="0 0 14 14" aria-hidden="true"><rect x="2" y="2.5" width="10" height="9" rx="1.5" fill="none" stroke="currentColor" stroke-width="1.1"></rect><path d="M2.8 5.1h8.4" fill="none" stroke="currentColor" stroke-width="1.1"></path></svg>',
    columns: '<svg viewBox="0 0 14 14" aria-hidden="true"><rect x="2" y="2.5" width="10" height="9" rx="1.3" fill="none" stroke="currentColor" stroke-width="1.1"></rect><path d="M7 2.8v8.4" fill="none" stroke="currentColor" stroke-width="1.1"></path></svg>',
    fieldList: '<svg viewBox="0 0 14 14" aria-hidden="true"><path d="M3 4.2h1.2M5.5 4.2H11M3 7h1.2M5.5 7H11M3 9.8h1.2M5.5 9.8H11" fill="none" stroke="currentColor" stroke-width="1.1" stroke-linecap="round"></path><circle cx="2.3" cy="4.2" r="0.6" fill="currentColor"></circle><circle cx="2.3" cy="7" r="0.6" fill="currentColor"></circle><circle cx="2.3" cy="9.8" r="0.6" fill="currentColor"></circle></svg>',
    specialField: '<svg viewBox="0 0 14 14" aria-hidden="true"><path d="M7 2.4v6.4M7 11.2h.01M4.2 3.6a3.3 3.3 0 015.6 2.3c0 1.8-1.6 2.3-2.4 3" fill="none" stroke="currentColor" stroke-width="1.1" stroke-linecap="round" stroke-linejoin="round"></path></svg>',
    margin: '<svg viewBox="0 0 14 14" aria-hidden="true"><rect x="2" y="2" width="10" height="10" rx="1.2" fill="none" stroke="currentColor" stroke-width="1.2"></rect><rect x="4" y="4" width="6" height="6" rx="0.8" fill="none" stroke="currentColor" stroke-width="1.2" stroke-dasharray="1.5 1.2"></rect></svg>',
    fontFamily: '<svg viewBox="0 0 14 14" aria-hidden="true"><path d="M4 11l2.1-8h1.8L10 11M4.8 8.2h4.4" fill="none" stroke="currentColor" stroke-width="1.25" stroke-linecap="round" stroke-linejoin="round"></path></svg>',
    fontSize: '<svg viewBox="0 0 14 14" aria-hidden="true"><path d="M3 11V3m0 0l-1 1m1-1l1 1M11 3v8m0 0l-1-1m1 1l1-1M5.2 10.4L7 4.5l1.8 5.9M5.9 8h2.2" fill="none" stroke="currentColor" stroke-width="1.1" stroke-linecap="round" stroke-linejoin="round"></path></svg>',
    fontWeight: '<svg viewBox="0 0 14 14" aria-hidden="true"><path d="M4 3h3.1a2 2 0 010 4H4zm0 4h3.5a2 2 0 010 4H4z" fill="none" stroke="currentColor" stroke-width="1.2" stroke-linejoin="round"></path></svg>',
    fontStyle: '<svg viewBox="0 0 14 14" aria-hidden="true"><path d="M9.5 3H6.8m2.4 0L6.2 11m.9 0H4.5m2.6 0h2.7" fill="none" stroke="currentColor" stroke-width="1.2" stroke-linecap="round" stroke-linejoin="round"></path></svg>',
    textDecoration: '<svg viewBox="0 0 14 14" aria-hidden="true"><path d="M3 4.2c0 1.8 1.4 3.3 4 3.3s4-1.5 4-3.3M7 7.6V11M3 11h8" fill="none" stroke="currentColor" stroke-width="1.2" stroke-linecap="round" stroke-linejoin="round"></path></svg>',
    textAlign: '<svg viewBox="0 0 14 14" aria-hidden="true"><path d="M3 4h8M5 6.5h4M3 9h8M4 11.5h6" fill="none" stroke="currentColor" stroke-width="1.2" stroke-linecap="round"></path></svg>',
    color: '<svg viewBox="0 0 14 14" aria-hidden="true"><path d="M4 9.5l3.5-6L11 9.5M5.2 7.4h4.1M3.6 11.2h7.1" fill="none" stroke="currentColor" stroke-width="1.2" stroke-linecap="round" stroke-linejoin="round"></path></svg>',
    border: '<svg viewBox="0 0 14 14" aria-hidden="true"><rect x="3" y="3" width="8" height="8" rx="1.2" fill="none" stroke="currentColor" stroke-width="1.2"></rect></svg>',
    radius: '<svg viewBox="0 0 14 14" aria-hidden="true"><path d="M3 11V5.5A2.5 2.5 0 015.5 3H11" fill="none" stroke="currentColor" stroke-width="1.2" stroke-linecap="round"></path><path d="M3 11h8V3" fill="none" stroke="currentColor" stroke-width="1.2" stroke-linecap="round"></path></svg>',
    display: '<svg viewBox="0 0 14 14" aria-hidden="true"><rect x="2.2" y="2.2" width="4.2" height="4.2" rx="0.7" fill="none" stroke="currentColor" stroke-width="1.1"></rect><rect x="7.6" y="2.2" width="4.2" height="4.2" rx="0.7" fill="none" stroke="currentColor" stroke-width="1.1"></rect><rect x="2.2" y="7.6" width="4.2" height="4.2" rx="0.7" fill="none" stroke="currentColor" stroke-width="1.1"></rect><rect x="7.6" y="7.6" width="4.2" height="4.2" rx="0.7" fill="none" stroke="currentColor" stroke-width="1.1"></rect></svg>',
    image: '<svg viewBox="0 0 14 14" aria-hidden="true"><rect x="2" y="2.5" width="10" height="9" rx="1.2" fill="none" stroke="currentColor" stroke-width="1.1"></rect><circle cx="5.1" cy="5.1" r="1" fill="currentColor"></circle><path d="M3.4 10l2.3-2.4 1.7 1.6 1.9-2 1.3 1.3" fill="none" stroke="currentColor" stroke-width="1.1" stroke-linecap="round" stroke-linejoin="round"></path></svg>',
    table: '<svg viewBox="0 0 14 14" aria-hidden="true"><rect x="2" y="2.5" width="10" height="9" rx="1.2" fill="none" stroke="currentColor" stroke-width="1.1"></rect><path d="M2.4 5.4h9.2M2.4 8.2h9.2M5 2.8v8.4M8.2 2.8v8.4" fill="none" stroke="currentColor" stroke-width="1.05"></path></svg>',
    repeater: '<svg viewBox="0 0 14 14" aria-hidden="true"><path d="M4 3.6h6M4 7h6M4 10.4h6" fill="none" stroke="currentColor" stroke-width="1.1" stroke-linecap="round"></path><path d="M2.4 3.6l.8-.8.8.8M4 10.4l-.8.8-.8-.8" fill="none" stroke="currentColor" stroke-width="1.1" stroke-linecap="round" stroke-linejoin="round"></path></svg>',
    pageBreak: '<svg viewBox="0 0 14 14" aria-hidden="true"><path d="M2.5 4.5h9M2.5 9.5h9" fill="none" stroke="currentColor" stroke-width="1.1" stroke-dasharray="1.6 1.4" stroke-linecap="round"></path><path d="M7 2.5v9" fill="none" stroke="currentColor" stroke-width="1.1" stroke-linecap="round"></path></svg>',
    barcode: '<svg viewBox="0 0 14 14" aria-hidden="true"><path d="M3 3v8M5 3v8M6.2 3v8M8.1 3v8M10 3v8M11.2 3v8" fill="none" stroke="currentColor" stroke-width="1.1" stroke-linecap="round"></path></svg>',
    qr: '<svg viewBox="0 0 14 14" aria-hidden="true"><rect x="2" y="2" width="3.3" height="3.3" fill="none" stroke="currentColor" stroke-width="1.1"></rect><rect x="8.7" y="2" width="3.3" height="3.3" fill="none" stroke="currentColor" stroke-width="1.1"></rect><rect x="2" y="8.7" width="3.3" height="3.3" fill="none" stroke="currentColor" stroke-width="1.1"></rect><path d="M8.7 8.7h1.5v1.5H8.7zM10.5 10.5H12v1.5h-1.5zM10.5 8.7H12" fill="currentColor"></path></svg>',
  };

  const markup = icons[iconName];
  return markup ? `<span class="rd-label-icon">${markup}</span>` : "";
}

function renderLabelContent(label, iconName) {
  return `${renderSectionIcon(iconName)}<span>${escapeHtml(label)}</span>`;
}

function getDefaultBlockName(type) {
  return t(`Default.BlockName.${normalizeBlockType(type)}`);
}

function getBlockTypeLabel(type) {
  return t(`BlockType.${normalizeBlockType(type)}`);
}

function getBlockTypeIconName(type) {
  switch (normalizeBlockType(type)) {
    case "Container":
      return "container";
    case "Columns":
      return "columns";
    case "FieldList":
      return "fieldList";
    case "SpecialField":
      return "specialField";
    case "Text":
      return "fontFamily";
    case "Image":
      return "image";
    case "Table":
      return "table";
    case "Repeater":
      return "repeater";
    case "PageBreak":
      return "pageBreak";
    case "Barcode":
      return "barcode";
    case "QrCode":
      return "qr";
    default:
      return "display";
  }
}

function renderBlockPaletteLabel(type) {
  return renderLabelContent(getBlockTypeLabel(type), getBlockTypeIconName(type));
}

function postToHost(message) {
  try {
    if (window.PhialeWebHost && typeof window.PhialeWebHost.postMessage === "function") {
      return window.PhialeWebHost.postMessage(message);
    }
  } catch (_) {
  }

  return false;
}

function notifyReady() {
  if (state.readyAnnounced) {
    return;
  }

  state.readyAnnounced = true;

  try {
    if (window.PhialeWebHost && typeof window.PhialeWebHost.notifyReady === "function") {
      window.PhialeWebHost.notifyReady({
        detail: "report designer ready",
      });
      return;
    }
  } catch (_) {
  }

  postToHost({
    type: "reportDesigner.ready",
    detail: "report designer ready",
  });
}

function emitError(message, detail) {
  postToHost({
    type: "reportDesigner.error",
    message: message || t("Error.Unexpected"),
    detail: detail || "",
  });
}

function stringifyError(error) {
  if (!error) {
    return "";
  }

  const parts = [];
  if (error.message) {
    parts.push(error.message);
  }
  if (error.stack) {
    parts.push(error.stack);
  }
  return parts.join("\n");
}

function cloneDefinition() {
  syncDefinitionSections();
  const cloned = JSON.parse(JSON.stringify(state.definition || createDefaultDefinition()));
  cloned.editorMetadata = JSON.stringify({
    selectedTargetId: state.selectedBlockId || SELECTED_PAGE_ID,
    panelSizes: {
      left: state.panelSizes.left,
      right: state.panelSizes.right,
    },
  });
  return cloned;
}

function syncDefinitionSections() {
  if (!state.definition || typeof state.definition !== "object") {
    return;
  }

  state.definition.sections = {
    reportHeader: {
      blocks: state.definition.reportHeaderBlocks || [],
    },
    body: {
      blocks: state.definition.blocks || [],
    },
    reportFooter: {
      blocks: state.definition.reportFooterBlocks || [],
    },
    pageHeader: {
      blocks: state.definition.pageHeaderBlocks || [],
      skipFirstPage: !!state.definition.pageHeaderSkipFirstPage,
      skipLastPage: !!state.definition.pageHeaderSkipLastPage,
    },
    pageFooter: {
      blocks: state.definition.pageFooterBlocks || [],
      skipFirstPage: !!state.definition.pageFooterSkipFirstPage,
      skipLastPage: !!state.definition.pageFooterSkipLastPage,
    },
  };
}

function getSectionBlocks(definition, sectionKey) {
  const source = definition && typeof definition === "object"
    ? definition
    : createDefaultDefinition();

  switch (sectionKey) {
    case "reportHeader":
      return source.reportHeaderBlocks;
    case "reportFooter":
      return source.reportFooterBlocks;
    case "pageHeader":
      return source.pageHeaderBlocks;
    case "pageFooter":
      return source.pageFooterBlocks;
    case "body":
    default:
      return source.blocks;
  }
}

function getAllSectionEntries(definition = state.definition) {
  return SECTION_DEFINITIONS.map((section) => ({
    ...section,
    targetId: getSectionTargetId(section.key),
    blocks: getSectionBlocks(definition, section.key),
  }));
}

function getFirstAvailableSelectionTarget() {
  for (const section of getAllSectionEntries()) {
    if (Array.isArray(section.blocks) && section.blocks.length > 0) {
      return section.blocks[0].id;
    }
  }

  return getSectionTargetId("body");
}

function findSectionEntryByKey(sectionKey, definition = state.definition) {
  return getAllSectionEntries(definition).find((section) => section.key === sectionKey) || null;
}

function scheduleDefinitionChanged() {
  clearTimeout(state.definitionChangedTimer);
  state.definitionChangedTimer = window.setTimeout(() => {
    postToHost({
      type: "reportDesigner.definitionChanged",
      definition: cloneDefinition(),
    });
  }, 400);
}

function normalizeMode(mode) {
  return String(mode || "").toLowerCase() === "preview" ? "preview" : "design";
}

function normalizeDefinition(definition) {
  const normalized = definition && typeof definition === "object"
    ? definition
    : createDefaultDefinition();

  normalized.version = typeof normalized.version === "number" ? normalized.version : 1;
  normalized.page = normalized.page && typeof normalized.page === "object"
    ? normalized.page
    : {
        size: "A4",
        orientation: "Portrait",
        margin: "20mm",
      };
  normalized.page.size = normalized.page.size || "A4";
  normalized.page.orientation = normalized.page.orientation || "Portrait";
  normalized.page.margin = normalized.page.margin || "20mm";
  const sections = normalized.sections && typeof normalized.sections === "object"
    ? normalized.sections
    : null;
  normalized.reportHeaderBlocks = Array.isArray(normalized.reportHeaderBlocks)
    ? normalized.reportHeaderBlocks.map(normalizeBlock)
    : Array.isArray(sections?.reportHeader?.blocks)
      ? sections.reportHeader.blocks.map(normalizeBlock)
      : [];
  normalized.blocks = Array.isArray(normalized.blocks)
    ? normalized.blocks.map(normalizeBlock)
    : Array.isArray(sections?.body?.blocks)
      ? sections.body.blocks.map(normalizeBlock)
      : [];
  normalized.reportFooterBlocks = Array.isArray(normalized.reportFooterBlocks)
    ? normalized.reportFooterBlocks.map(normalizeBlock)
    : Array.isArray(sections?.reportFooter?.blocks)
      ? sections.reportFooter.blocks.map(normalizeBlock)
      : [];
  normalized.pageHeaderBlocks = Array.isArray(normalized.pageHeaderBlocks)
    ? normalized.pageHeaderBlocks.map(normalizeBlock)
    : Array.isArray(sections?.pageHeader?.blocks)
      ? sections.pageHeader.blocks.map(normalizeBlock)
      : [];
  normalized.pageHeaderSkipFirstPage = typeof normalized.pageHeaderSkipFirstPage === "boolean"
    ? normalized.pageHeaderSkipFirstPage
    : !!sections?.pageHeader?.skipFirstPage;
  normalized.pageHeaderSkipLastPage = typeof normalized.pageHeaderSkipLastPage === "boolean"
    ? normalized.pageHeaderSkipLastPage
    : !!sections?.pageHeader?.skipLastPage;
  normalized.pageFooterBlocks = Array.isArray(normalized.pageFooterBlocks)
    ? normalized.pageFooterBlocks.map(normalizeBlock)
    : Array.isArray(sections?.pageFooter?.blocks)
      ? sections.pageFooter.blocks.map(normalizeBlock)
      : [];
  normalized.pageFooterSkipFirstPage = typeof normalized.pageFooterSkipFirstPage === "boolean"
    ? normalized.pageFooterSkipFirstPage
    : !!sections?.pageFooter?.skipFirstPage;
  normalized.pageFooterSkipLastPage = typeof normalized.pageFooterSkipLastPage === "boolean"
    ? normalized.pageFooterSkipLastPage
    : !!sections?.pageFooter?.skipLastPage;
  normalized.sections = {
    reportHeader: {
      blocks: normalized.reportHeaderBlocks,
    },
    body: {
      blocks: normalized.blocks,
    },
    reportFooter: {
      blocks: normalized.reportFooterBlocks,
    },
    pageHeader: {
      blocks: normalized.pageHeaderBlocks,
      skipFirstPage: normalized.pageHeaderSkipFirstPage,
      skipLastPage: normalized.pageHeaderSkipLastPage,
    },
    pageFooter: {
      blocks: normalized.pageFooterBlocks,
      skipFirstPage: normalized.pageFooterSkipFirstPage,
      skipLastPage: normalized.pageFooterSkipLastPage,
    },
  };
  normalized.editorMetadata = typeof normalized.editorMetadata === "string"
    ? normalized.editorMetadata
    : "";

  return normalized;
}

function normalizeBlock(block) {
  const normalized = block && typeof block === "object" ? block : createBlock("Text");
  const type = normalizeBlockType(normalized.type);
  const normalizedBlock = {
    id: normalized.id || buildId(type),
    type,
    name: normalized.name || getDefaultBlockName(type),
    text: normalized.text || "",
    binding: normalized.binding || "",
    imageSource: normalized.imageSource || "",
    itemsSource: normalized.itemsSource || "",
    columnCount: type === "Columns" ? clampColumnCount(normalized.columnCount || (Array.isArray(normalized.children) ? normalized.children.length : 2)) : 0,
    columnGap: normalized.columnGap || "",
    barcodeType: normalizeBarcodeType(normalized.barcodeType),
    showText: normalized.showText !== false,
    size: normalized.size || "",
    errorCorrectionLevel: normalizeQrErrorLevel(normalized.errorCorrectionLevel),
    specialFieldKind: normalizeSpecialFieldKind(normalized.specialFieldKind),
    pageBreakBefore: !!normalized.pageBreakBefore,
    keepTogether: !!normalized.keepTogether,
    repeatHeader: normalized.repeatHeader !== false,
    format: normalizeFormat(normalized.format),
    style: normalizeStyle(normalized.style),
    columns: Array.isArray(normalized.columns) ? normalized.columns.map(normalizeColumn) : [],
    fields: Array.isArray(normalized.fields) ? normalized.fields.map(normalizeFieldListItem) : [],
    children: Array.isArray(normalized.children) ? normalized.children.map(normalizeBlock) : [],
  };

  if (type === "Columns") {
    synchronizeColumnsBlock(normalizedBlock);
  }

  return normalizedBlock;
}

function normalizeColumn(column) {
  return {
    header: column && column.header ? String(column.header) : "",
    binding: column && column.binding ? String(column.binding) : "",
    width: column && column.width ? String(column.width) : "",
    textAlign: column && column.textAlign ? String(column.textAlign) : "",
    format: normalizeFormat(column ? column.format : null),
  };
}

function normalizeFieldListItem(item) {
  return {
    label: item && item.label ? String(item.label) : "",
    binding: item && item.binding ? String(item.binding) : "",
    format: normalizeFormat(item ? item.format : null),
  };
}

function synchronizeColumnsBlock(block) {
  if (!block || block.type !== "Columns") {
    return;
  }

  const desiredCount = clampColumnCount(block.columnCount || block.children.length || 2);
  const currentChildren = Array.isArray(block.children) ? block.children.slice(0, desiredCount) : [];

  while (currentChildren.length < desiredCount) {
    currentChildren.push(createColumnContainer(currentChildren.length));
  }

  currentChildren.forEach((child, index) => {
    child.type = "Container";
    if (!child.name) {
      child.name = formatText("Default.ColumnContainer", index + 1);
    }
    child.style = normalizeStyle(child.style);
    child.style.display = child.style.display || "grid";
    child.style.padding = child.style.padding || "0";
  });

  block.columnCount = desiredCount;
  block.children = currentChildren;
}

function normalizeFormat(format) {
  const normalized = format && typeof format === "object" ? format : createEmptyFormat();
  const kind = FORMAT_KINDS.includes(String(normalized.kind || "").toLowerCase())
    ? String(normalized.kind).toLowerCase()
    : "text";

  return {
    kind,
    pattern: normalized.pattern || "",
    currency: normalized.currency || "",
    decimals: typeof normalized.decimals === "number" ? normalized.decimals : null,
  };
}

function normalizeStyle(style) {
  const normalized = style && typeof style === "object" ? style : createEmptyStyle();
  const result = createEmptyStyle();

  for (const field of STYLE_FIELDS) {
    result[field] = normalized[field] ? String(normalized[field]) : "";
  }

  return result;
}

function normalizeSchema(schema) {
  const normalized = schema && typeof schema === "object" ? schema : { fields: [] };
  return {
    fields: Array.isArray(normalized.fields) ? normalized.fields.map(normalizeField) : [],
  };
}

function normalizeField(field) {
  const normalized = field && typeof field === "object" ? field : {};
  return {
    name: normalized.name ? String(normalized.name) : "",
    displayName: normalized.displayName ? String(normalized.displayName) : "",
    description: normalized.description ? String(normalized.description) : "",
    type: normalized.type ? String(normalized.type) : "string",
    isCollection: !!normalized.isCollection,
    children: Array.isArray(normalized.children) ? normalized.children.map(normalizeField) : [],
  };
}

function parseJson(text, fallbackValue) {
  if (!text) {
    return fallbackValue;
  }

  try {
    return JSON.parse(text);
  } catch (_) {
    return fallbackValue;
  }
}

function restoreEditorMetadata(definition) {
  if (!definition || !definition.editorMetadata) {
    ensureSelection();
    return;
  }

  try {
    const metadata = JSON.parse(definition.editorMetadata);
    const selectedTargetId = metadata && typeof metadata.selectedTargetId === "string"
      ? metadata.selectedTargetId
      : metadata && typeof metadata.selectedBlockId === "string"
        ? metadata.selectedBlockId
        : "";

    if (selectedTargetId === SELECTED_PAGE_ID || isSectionTargetId(selectedTargetId)) {
      state.selectedBlockId = SELECTED_PAGE_ID;
      if (isSectionTargetId(selectedTargetId)) {
        state.selectedBlockId = selectedTargetId;
      }
    } else if (selectedTargetId && findBlockById(selectedTargetId)) {
      state.selectedBlockId = selectedTargetId;
    }

    if (metadata && metadata.panelSizes && typeof metadata.panelSizes === "object") {
      const left = Number(metadata.panelSizes.left);
      const right = Number(metadata.panelSizes.right);
      if (Number.isFinite(left) && left >= MIN_PANEL_SIZES.left) {
        state.panelSizes.left = left;
      }
      if (Number.isFinite(right) && right >= MIN_PANEL_SIZES.right) {
        state.panelSizes.right = right;
      }
    }
  } catch (_) {
  }

  ensureSelection();
}

function ensureSelection() {
  if (isPageSelection() || isSectionSelection()) {
    return;
  }

  if (state.selectedBlockId && findBlockById(state.selectedBlockId)) {
    return;
  }

  state.selectedBlockId = getFirstAvailableSelectionTarget();
}

function findBlockById(id, blocks = null) {
  if (!id) {
    return null;
  }

  const list = Array.isArray(blocks)
    ? blocks
    : getAllSectionEntries().flatMap((section) => section.blocks || []);

  if (!Array.isArray(list)) {
    return null;
  }

  for (const block of list) {
    if (block.id === id) {
      return block;
    }

    const nested = findBlockById(id, block.children);
    if (nested) {
      return nested;
    }
  }

  return null;
}

function findBlockEntryById(list, id, parent = null, sectionKey = "body") {
  if (!Array.isArray(list) || !id) {
    return null;
  }

  for (let index = 0; index < list.length; index += 1) {
    const block = list[index];
    if (block.id === id) {
      return {
        block,
        parent,
        list,
        index,
        sectionKey,
      };
    }

    const nested = findBlockEntryById(block.children, id, block, sectionKey);
    if (nested) {
      return nested;
    }
  }

  return null;
}

function triggerSelectionPulse(targetId) {
  const safeTargetId = String(targetId || "");

  if (state.selectionPulseTimer) {
    window.clearTimeout(state.selectionPulseTimer);
    state.selectionPulseTimer = 0;
  }

  state.selectionPulseTargetId = safeTargetId;
  state.selectionPulseTimer = window.setTimeout(() => {
    state.selectionPulseTimer = 0;
    if (state.selectionPulseTargetId !== safeTargetId) {
      return;
    }

    state.selectionPulseTargetId = "";
    renderApp();
  }, 820);
}

function canHostChildren(block) {
  return !!block && (block.type === "Container" || block.type === "Repeater");
}

function addBlock(type) {
  const block = createBlock(type);
  const selected = findBlockById(state.selectedBlockId);
  const selectedSectionKey = getSelectedSectionKey();

  if (selected && canHostChildren(selected)) {
    selected.children.push(block);
  } else if (selectedSectionKey) {
    const section = findSectionEntryByKey(selectedSectionKey);
    (section?.blocks || state.definition.blocks).push(block);
  } else {
    state.definition.blocks.push(block);
  }

  state.selectedBlockId = block.id;
  renderApp();
  scheduleDefinitionChanged();
}

function deleteBlock(id) {
  if (!id) {
    return;
  }

  let removed = false;
  for (const section of getAllSectionEntries()) {
    removed = removeBlockById(section.blocks, id) || removed;
    if (removed) {
      break;
    }
  }

  if (!removed) {
    return;
  }

  ensureSelection();
  renderApp();
  scheduleDefinitionChanged();
}

function removeBlockById(list, id) {
  if (!Array.isArray(list) || !id) {
    return false;
  }

  const index = list.findIndex((block) => block.id === id);
  if (index >= 0) {
    list.splice(index, 1);
    return true;
  }

  for (const block of list) {
    if (removeBlockById(block.children, id)) {
      return true;
    }
  }

  return false;
}

function moveBlock(id, direction) {
  let entry = null;
  for (const section of getAllSectionEntries()) {
    entry = findBlockEntryById(section.blocks, id, null, section.key);
    if (entry) {
      break;
    }
  }

  if (!entry) {
    return;
  }

  const step = direction === "down" ? 1 : -1;
  const targetIndex = entry.index + step;
  if (targetIndex < 0 || targetIndex >= entry.list.length) {
    return;
  }

  const moved = entry.list.splice(entry.index, 1)[0];
  entry.list.splice(targetIndex, 0, moved);
  renderApp();
  scheduleDefinitionChanged();
}

function updatePageField(field, value) {
  if (!state.definition.page || typeof state.definition.page !== "object") {
    state.definition.page = createDefaultDefinition().page;
  }

  state.definition.page[field] = value || "";
  if (field === "size" && !state.definition.page.size) {
    state.definition.page.size = "A4";
  }
  if (field === "orientation" && !state.definition.page.orientation) {
    state.definition.page.orientation = "Portrait";
  }
  if (field === "margin" && !state.definition.page.margin) {
    state.definition.page.margin = "20mm";
  }

  renderApp();
  scheduleDefinitionChanged();
}

function updateSectionField(field, value) {
  const sectionKey = getSelectedSectionKey();
  if (!sectionKey) {
    return;
  }

  if (sectionKey === "pageHeader") {
    if (field === "skipFirstPage") {
      state.definition.pageHeaderSkipFirstPage = !!value;
    } else if (field === "skipLastPage") {
      state.definition.pageHeaderSkipLastPage = !!value;
    }
  } else if (sectionKey === "pageFooter") {
    if (field === "skipFirstPage") {
      state.definition.pageFooterSkipFirstPage = !!value;
    } else if (field === "skipLastPage") {
      state.definition.pageFooterSkipLastPage = !!value;
    }
  }

  renderApp();
  scheduleDefinitionChanged();
}

function updateLengthField(scopeKey, fieldName, part, value, selectedBlock) {
  const defaultUnit = fieldName === "margin" ? "mm" : "px";
  const currentValue = scopeKey === "pageField"
    ? state.definition.page[fieldName]
    : scopeKey === "blockField"
      ? selectedBlock?.[fieldName]
      : selectedBlock?.style?.[fieldName];
  const parsed = parseLengthValue(currentValue, defaultUnit);
  const next = parsed.kind === "raw"
    ? { kind: "length", amount: "", unit: defaultUnit }
    : { ...parsed };

  if (part === "amount") {
    next.amount = String(value ?? "").trim();
  } else if (part === "unit") {
    next.unit = String(value ?? defaultUnit).trim() || defaultUnit;
  }

  const composed = composeLengthValue(next, defaultUnit);
  if (scopeKey === "pageField") {
    state.definition.page[fieldName] = composed;
  } else if (scopeKey === "blockField" && selectedBlock) {
    selectedBlock[fieldName] = composed;
  } else if (scopeKey === "styleField" && selectedBlock) {
    selectedBlock.style[fieldName] = composed;
  }
}

function updateSpacingField(fieldName, part, value, selectedBlock) {
  if (!selectedBlock) {
    return;
  }

  const parsed = parseBoxSpacingValue(selectedBlock.style[fieldName], "px");
  const next = parsed.kind === "raw"
    ? { kind: "box", top: "", right: "", bottom: "", left: "", unit: "px" }
    : { ...parsed };

  if (part === "unit") {
    next.unit = String(value ?? "px").trim() || "px";
  } else {
    next[part] = String(value ?? "").trim();
  }

  selectedBlock.style[fieldName] = composeBoxSpacingValue(next, next.unit || "px");
}

function updateBorderField(fieldName, part, value, selectedBlock) {
  if (!selectedBlock || fieldName !== "border") {
    return;
  }

  const parsed = parseBorderValue(selectedBlock.style.border);
  const next = parsed.kind === "raw"
    ? { kind: "border", enabled: false, width: "1", unit: "px", style: "solid", color: "#cbd5e1" }
    : { ...parsed };

  if (part === "enabled") {
    next.enabled = !!value;
  } else if (part === "width") {
    next.width = String(value ?? "").trim();
  } else if (part === "unit") {
    next.unit = String(value ?? "px").trim() || "px";
  } else if (part === "style") {
    next.style = String(value ?? "solid").trim() || "solid";
  } else if (part === "color") {
    next.color = String(value ?? "").trim();
  }

  selectedBlock.style.border = composeBorderValue(next);
}

function applyColorField(scopeKey, fieldName, part, value, selectedBlock) {
  const nextValue = String(value ?? "").trim();

  if (scopeKey === "pageField") {
    updatePageField(fieldName, nextValue);
    return true;
  }

  if (!selectedBlock) {
    return false;
  }

  if (scopeKey === "styleField") {
    selectedBlock.style[fieldName] = nextValue;
    return true;
  }

  if (scopeKey === "blockField") {
    selectedBlock[fieldName] = nextValue;
    return true;
  }

  if (scopeKey === "borderField") {
    updateBorderField(fieldName, part || "color", nextValue, selectedBlock);
    return true;
  }

  return false;
}

function handleColorInput(target) {
  if (!target) {
    return;
  }

  const selectedBlock = findBlockById(state.selectedBlockId);
  const applied = applyColorField(
    target.dataset.colorScope || "styleField",
    target.dataset.colorField || "",
    target.dataset.colorPart || "",
    readTargetValue(target),
    selectedBlock);

  if (!applied) {
    return;
  }

  renderApp();
  scheduleDefinitionChanged();
}

function handleShellClick(actionElement) {
  const action = actionElement.dataset.action;
  if (!action) {
    return;
  }

  if (action === "apply-color-swatch") {
    const selectedBlock = findBlockById(state.selectedBlockId);
    const applied = applyColorField(
      actionElement.dataset.colorScope || "styleField",
      actionElement.dataset.colorField || "",
      actionElement.dataset.colorPart || "",
      actionElement.dataset.colorValue || "",
      selectedBlock);
    if (applied) {
      renderApp();
      scheduleDefinitionChanged();
    }
    return;
  }

  if (action === "add-block") {
    addBlock(actionElement.dataset.type || "Text");
    return;
  }

  if (action === "set-left-rail-tab") {
    state.leftRailTab = actionElement.dataset.tab === "schema" ? "schema" : "blocks";
    renderApp();
    return;
  }

  if (action === "open-help") {
    state.helpModalKey = actionElement.dataset.helpKey || "";
    renderApp();
    return;
  }

  if (action === "close-help") {
    state.helpModalKey = "";
    renderApp();
    return;
  }

  if (action === "toggle-schema-node") {
    const path = actionElement.dataset.path || "";
    if (path) {
      state.expandedSchemaPaths[path] = !isSchemaNodeExpanded(path, 1);
      renderApp();
    }
    return;
  }

  if (action === "copy-schema-path") {
    const path = actionElement.dataset.path || "";
    if (path) {
      copyTextToClipboardAsync(path).then((copied) => {
        if (!copied) {
          return;
        }

        state.copiedSchemaPath = path;
        if (state.copiedSchemaTimer) {
          window.clearTimeout(state.copiedSchemaTimer);
        }

        renderApp();
        state.copiedSchemaTimer = window.setTimeout(() => {
          state.copiedSchemaPath = "";
          state.copiedSchemaTimer = 0;
          renderApp();
        }, 1200);
      });
    }
    return;
  }

  if (action === "select-block") {
    state.selectedBlockId = actionElement.dataset.blockId || "";
    triggerSelectionPulse(state.selectedBlockId);
    renderApp();
    return;
  }

  if (action === "select-page") {
    state.selectedBlockId = SELECTED_PAGE_ID;
    triggerSelectionPulse(SELECTED_PAGE_ID);
    renderApp();
    return;
  }

  if (action === "select-section") {
    const sectionKey = actionElement.dataset.sectionKey || "body";
    state.selectedBlockId = getSectionTargetId(sectionKey);
    triggerSelectionPulse(state.selectedBlockId);
    renderApp();
    return;
  }

  if (action === "delete-block") {
    deleteBlock(actionElement.dataset.blockId || "");
    return;
  }

  if (action === "move-block-up") {
    moveBlock(actionElement.dataset.blockId || "", "up");
    return;
  }

  if (action === "move-block-down") {
    moveBlock(actionElement.dataset.blockId || "", "down");
    return;
  }

  if (action === "clear-selection") {
    state.selectedBlockId = SELECTED_PAGE_ID;
    triggerSelectionPulse(SELECTED_PAGE_ID);
    renderApp();
  }
}

function handleShellInput(target) {
  if (!target) {
    return;
  }

  const selectedBlock = findBlockById(state.selectedBlockId);
  const value = readTargetValue(target);

  if (target.dataset.lengthField) {
    updateLengthField(
      target.dataset.lengthScope || "styleField",
      target.dataset.lengthField,
      target.dataset.lengthPart || "amount",
      value,
      selectedBlock);
    renderApp();
    scheduleDefinitionChanged();
    return;
  }

  if (target.dataset.spacingField) {
    updateSpacingField(
      target.dataset.spacingField,
      target.dataset.spacingPart || "top",
      value,
      selectedBlock);
    renderApp();
    scheduleDefinitionChanged();
    return;
  }

  if (target.dataset.borderField) {
    updateBorderField(
      target.dataset.borderField,
      target.dataset.borderPart || target.dataset.borderField,
      value,
      selectedBlock);
    renderApp();
    scheduleDefinitionChanged();
    return;
  }

  if (target.dataset.pageField) {
    updatePageField(target.dataset.pageField, String(value ?? ""));
    return;
  }

  if (target.dataset.sectionField) {
    updateSectionField(target.dataset.sectionField, value);
    return;
  }

  if (!selectedBlock) {
    return;
  }

  if (target.dataset.blockField) {
    const field = target.dataset.blockField;
    if (field === "showText") {
      selectedBlock.showText = !!value;
    } else if (field === "columnCount") {
      selectedBlock.columnCount = clampColumnCount(value);
      synchronizeColumnsBlock(selectedBlock);
    } else {
      selectedBlock[field] = value == null ? "" : (typeof value === "boolean" ? value : String(value));
    }
  } else if (target.dataset.styleField) {
    selectedBlock.style[target.dataset.styleField] = value == null ? "" : String(value);
  } else if (target.dataset.formatField) {
    if (target.dataset.formatField === "decimals") {
      selectedBlock.format.decimals = value === "" ? null : Number(value);
      if (!Number.isFinite(selectedBlock.format.decimals)) {
        selectedBlock.format.decimals = null;
      }
    } else {
      selectedBlock.format[target.dataset.formatField] = value == null ? "" : String(value);
    }
  } else if (target.dataset.columnsField === "columns") {
    selectedBlock.columns = parseTableColumnsText(String(value ?? ""));
  } else if (target.dataset.fieldsField === "fields") {
    selectedBlock.fields = parseFieldListText(String(value ?? ""));
  } else {
    return;
  }

  if (selectedBlock.type === "Barcode") {
    selectedBlock.barcodeType = normalizeBarcodeType(selectedBlock.barcodeType);
  }

  if (selectedBlock.type === "QrCode") {
    selectedBlock.errorCorrectionLevel = normalizeQrErrorLevel(selectedBlock.errorCorrectionLevel);
  }

  if (selectedBlock.type === "SpecialField") {
    selectedBlock.specialFieldKind = normalizeSpecialFieldKind(selectedBlock.specialFieldKind);
  }

  renderApp();
  scheduleDefinitionChanged();
}

function readTargetValue(target) {
  if (target instanceof HTMLInputElement && target.type === "checkbox") {
    return target.checked;
  }

  return target.value == null ? "" : target.value;
}

function isShellFieldTarget(target) {
  return target instanceof HTMLElement && target.matches(SHELL_FIELD_SELECTOR);
}

function shouldHandleFieldOnInput(target) {
  if (target instanceof HTMLTextAreaElement) {
    return true;
  }

  if (target instanceof HTMLInputElement) {
    return target.type !== "checkbox" && target.type !== "radio";
  }

  return false;
}

function getPreviewData() {
  if (state.reportData && typeof state.reportData === "object") {
    return {
      data: state.reportData,
      usedSampleData: false,
    };
  }

  return {
    data: state.sampleData || {},
    usedSampleData: true,
  };
}

function buildPreviewModel() {
  const previewData = getPreviewData();
  const pages = buildPages(previewData.data);

  return {
    pages,
    pageCount: pages.length,
    usedSampleData: previewData.usedSampleData,
  };
}

function emitPreviewReady(previewModel) {
  postToHost({
    type: "reportDesigner.previewReady",
    pageCount: previewModel.pageCount,
    usedSampleData: previewModel.usedSampleData,
  });
}

function applyDocumentContext() {
  document.documentElement.dataset.theme = state.theme;
  document.documentElement.lang = state.locale;
  document.title = t("App.Title");
}

function applyPanelSizes() {
  shellElement.style.setProperty("--rd-left-size", `${Math.round(state.panelSizes.left)}px`);
  shellElement.style.setProperty("--rd-right-size", `${Math.round(state.panelSizes.right)}px`);
}

function camelToKebab(text) {
  return String(text || "")
    .replace(/([a-z0-9])([A-Z])/g, "$1-$2")
    .toLowerCase();
}

function escapeSelectorValue(value) {
  return String(value ?? "")
    .replaceAll("\\", "\\\\")
    .replaceAll("\"", "\\\"");
}

function buildFieldSelector(element) {
  if (!(element instanceof HTMLElement)) {
    return "";
  }

  const parts = [];
  for (const [key, value] of Object.entries(element.dataset || {})) {
    if (value == null || value === "") {
      continue;
    }

    parts.push(`[data-${camelToKebab(key)}="${escapeSelectorValue(value)}"]`);
  }

  if (element instanceof HTMLInputElement && element.type) {
    parts.push(`[type="${escapeSelectorValue(element.type)}"]`);
  }

  return `${element.tagName.toLowerCase()}${parts.join("")}`;
}

function captureActiveFieldState() {
  const active = document.activeElement;
  if (!(active instanceof HTMLElement) || !active.matches(SHELL_FIELD_SELECTOR)) {
    return null;
  }

  const selector = buildFieldSelector(active);
  if (!selector) {
    return null;
  }

  const matches = Array.from(document.querySelectorAll(selector));
  const index = Math.max(0, matches.indexOf(active));
  const state = { selector, index };

  if (active instanceof HTMLInputElement || active instanceof HTMLTextAreaElement) {
    state.selectionStart = typeof active.selectionStart === "number" ? active.selectionStart : null;
    state.selectionEnd = typeof active.selectionEnd === "number" ? active.selectionEnd : null;
  }

  return state;
}

function restoreActiveFieldState(activeFieldState) {
  if (!activeFieldState || !activeFieldState.selector) {
    return;
  }

  const matches = Array.from(document.querySelectorAll(activeFieldState.selector));
  const target = matches[activeFieldState.index] || matches[0];
  if (!(target instanceof HTMLElement)) {
    return;
  }

  target.focus({ preventScroll: true });

  if ((target instanceof HTMLInputElement || target instanceof HTMLTextAreaElement) &&
      activeFieldState.selectionStart != null &&
      activeFieldState.selectionEnd != null) {
    try {
      target.setSelectionRange(activeFieldState.selectionStart, activeFieldState.selectionEnd);
    } catch {
      // Some input types do not support text selection ranges.
    }
  }
}

function captureShellViewportState() {
  return {
    leftRailScrollTop: document.getElementById("leftRailScroll")?.scrollTop ?? 0,
    inspectorScrollTop: document.getElementById("inspectorScroll")?.scrollTop ?? 0,
    surfaceScrollTop: document.getElementById("surfaceScroll")?.scrollTop ?? 0,
    activeFieldState: captureActiveFieldState(),
  };
}

function restoreShellViewportState(viewportState) {
  if (!viewportState) {
    return;
  }

  const leftRailScroll = document.getElementById("leftRailScroll");
  if (leftRailScroll) {
    leftRailScroll.scrollTop = viewportState.leftRailScrollTop;
  }

  const inspectorScroll = document.getElementById("inspectorScroll");
  if (inspectorScroll) {
    inspectorScroll.scrollTop = viewportState.inspectorScrollTop;
  }

  const surfaceScroll = document.getElementById("surfaceScroll");
  if (surfaceScroll) {
    surfaceScroll.scrollTop = viewportState.surfaceScrollTop;
  }

  restoreActiveFieldState(viewportState.activeFieldState);
}

function renderHelpTrigger(helpKey) {
  return `
    <button
      type="button"
      class="rd-help-trigger"
      data-action="open-help"
      data-help-key="${escapeAttribute(helpKey)}"
      aria-label="${escapeAttribute(t("Action.Help"))}"
      title="${escapeAttribute(t("Action.Help"))}">
      ?
    </button>
  `;
}

function isSchemaNodeExpanded(path, depth) {
  const stored = state.expandedSchemaPaths[path];
  if (typeof stored === "boolean") {
    return stored;
  }

  return depth < 1;
}

function buildSchemaNodePath(parentPath, fieldName) {
  if (!fieldName) {
    return parentPath || "";
  }

  return parentPath ? `${parentPath}.${fieldName}` : fieldName;
}

async function copyTextToClipboardAsync(text) {
  const value = String(text || "");
  if (!value) {
    return false;
  }

  try {
    if (navigator.clipboard && typeof navigator.clipboard.writeText === "function") {
      await navigator.clipboard.writeText(value);
      return true;
    }
  } catch {
    // fall back to execCommand below
  }

  try {
    const textarea = document.createElement("textarea");
    textarea.value = value;
    textarea.setAttribute("readonly", "true");
    textarea.style.position = "fixed";
    textarea.style.opacity = "0";
    document.body.appendChild(textarea);
    textarea.select();
    const copied = document.execCommand("copy");
    textarea.remove();
    return copied;
  } catch {
    return false;
  }
}

function createHelpMatrix(helpKey) {
  if (helpKey === "Blocks") {
    return {
      design: [
        { icon: "container", label: t("Help.Blocks.Design.Container"), detail: t("Help.Blocks.Design.Container.Detail") },
        { icon: "fieldList", label: t("Help.Blocks.Design.FieldList"), detail: t("Help.Blocks.Design.FieldList.Detail") },
        { icon: "table", label: t("Help.Blocks.Design.Table"), detail: t("Help.Blocks.Design.Table.Detail") },
        { icon: "barcode", label: t("Help.Blocks.Design.Code"), detail: t("Help.Blocks.Design.Code.Detail") },
      ],
      preview: [
        { icon: "container", label: t("Help.Blocks.Preview.Container"), detail: t("Help.Blocks.Preview.Container.Detail") },
        { icon: "fieldList", label: t("Help.Blocks.Preview.FieldList"), detail: t("Help.Blocks.Preview.FieldList.Detail") },
        { icon: "table", label: t("Help.Blocks.Preview.Table"), detail: t("Help.Blocks.Preview.Table.Detail") },
        { icon: "barcode", label: t("Help.Blocks.Preview.Code"), detail: t("Help.Blocks.Preview.Code.Detail") },
      ],
    };
  }

  return {
    design: [
      { icon: "fieldList", label: t("Help.Schema.Design.Paths"), detail: t("Help.Schema.Design.Paths.Detail") },
      { icon: "table", label: t("Help.Schema.Design.Collection"), detail: t("Help.Schema.Design.Collection.Detail") },
    ],
    preview: [
      { icon: "text", label: t("Help.Schema.Preview.Paths"), detail: t("Help.Schema.Preview.Paths.Detail") },
      { icon: "repeater", label: t("Help.Schema.Preview.Collection"), detail: t("Help.Schema.Preview.Collection.Detail") },
    ],
  };
}

function renderHelpColumn(title, items) {
  return `
    <section class="rd-help-column">
      <h3 class="rd-help-column-title">${escapeHtml(title)}</h3>
      <div class="rd-help-list">
        ${items.map((item) => `
          <div class="rd-help-item">
            ${renderSectionIcon(item.icon)}
            <div class="rd-help-item-copy">
              <span class="rd-help-item-label">${escapeHtml(item.label)}</span>
              <span class="rd-help-item-detail">${escapeHtml(item.detail)}</span>
            </div>
          </div>
        `).join("")}
      </div>
    </section>
  `;
}

function renderHelpModal() {
  if (!state.helpModalKey) {
    return "";
  }

  const title = t(`Panel.${state.helpModalKey}.HelpTitle`);
  const body = t(`Panel.${state.helpModalKey}.HelpBody`);
  const example = t(`Panel.${state.helpModalKey}.HelpExample`);
  const matrix = createHelpMatrix(state.helpModalKey);

  return `
    <div class="rd-help-modal-backdrop" data-action="close-help">
      <section
        class="rd-help-modal"
        role="dialog"
        aria-modal="true"
        aria-labelledby="rdHelpModalTitle"
        data-stop-close="true">
        <div class="rd-panel-head">
          <h2 class="rd-help-modal-title" id="rdHelpModalTitle">${escapeHtml(title)}</h2>
          <button
            type="button"
            class="rd-help-trigger"
            data-action="close-help"
            aria-label="${escapeAttribute(t("Action.CloseHelp"))}"
            title="${escapeAttribute(t("Action.CloseHelp"))}">
            ×
          </button>
        </div>
        <p class="rd-help-modal-body">${escapeHtml(body)}</p>
        <div class="rd-help-matrix">
          ${renderHelpColumn(t("Help.Column.Design"), matrix.design)}
          ${renderHelpColumn(t("Help.Column.Preview"), matrix.preview)}
        </div>
        <div class="rd-help-modal-example">
          <p class="rd-help-modal-example-label">${escapeHtml(t("Help.ExampleLabel"))}</p>
          <p class="rd-help-modal-example-copy">${escapeHtml(example)}</p>
        </div>
        <div class="rd-help-modal-actions">
          <button type="button" class="rd-action-button" data-action="close-help">
            ${escapeHtml(t("Action.CloseHelp"))}
          </button>
        </div>
      </section>
    </div>
  `;
}

function renderApp() {
  ensureSelection();
  applyDocumentContext();
  const viewportState = captureShellViewportState();

  const previewModel = buildPreviewModel();
  state.lastPreviewModel = previewModel;

  shellElement.innerHTML = `
    <aside id="leftRail">
      <section class="rd-section">
        <div class="rd-panel-head">
          <div class="rd-tab-strip" role="tablist" aria-label="${escapeAttribute(t("Panel.Blocks.Title"))}">
            <button
              type="button"
              class="rd-tab-button${state.leftRailTab === "blocks" ? " is-active" : ""}"
              data-action="set-left-rail-tab"
              data-tab="blocks"
              role="tab"
              aria-selected="${state.leftRailTab === "blocks" ? "true" : "false"}">
              ${escapeHtml(t("Panel.Tab.Blocks"))}
            </button>
            <button
              type="button"
              class="rd-tab-button${state.leftRailTab === "schema" ? " is-active" : ""}"
              data-action="set-left-rail-tab"
              data-tab="schema"
              role="tab"
              aria-selected="${state.leftRailTab === "schema" ? "true" : "false"}">
              ${escapeHtml(t("Panel.Tab.Schema"))}
            </button>
          </div>
          ${renderHelpTrigger(state.leftRailTab === "schema" ? "Schema" : "Blocks")}
        </div>
      </section>
      <section class="rd-section rd-section-scroll" id="leftRailScroll">
        ${state.leftRailTab === "blocks" ? `
        <div class="rd-left-rail-pane">
          <div class="rd-palette">
            ${BLOCK_TYPES.map((type) => `<button type="button" data-action="add-block" data-type="${escapeAttribute(type)}">${renderBlockPaletteLabel(type)}</button>`).join("")}
          </div>
        </div>` : `
        <div class="rd-left-rail-pane">
          <div class="rd-schema-tree rd-schema-tree-compact">
            ${renderSchemaTree(state.schema.fields)}
          </div>
        </div>`}
      </section>
    </aside>
    <div class="rd-splitter" data-splitter="left" aria-hidden="true"></div>
    <section id="surfaceCard">
      <header id="surfaceHeader">
        <h1 id="surfaceTitle">${escapeHtml(state.mode === "preview" ? t("Surface.Title.Preview") : t("Surface.Title.Design"))}</h1>
        <p id="surfaceSubtitle">${escapeHtml(state.mode === "preview" ? t("Surface.Subtitle.Preview") : t("Surface.Subtitle.Design"))}</p>
        <div class="rd-page-summary">${renderSectionIcon("page")}<span>${escapeHtml(formatPageSummary())}</span></div>
      </header>
      <div id="surfaceScroll">
        ${state.mode === "preview" ? renderPreviewMarkup(previewModel) : renderDesignMarkup()}
      </div>
    </section>
    <div class="rd-splitter" data-splitter="right" aria-hidden="true"></div>
    <aside id="inspectorRail">
      <section class="rd-section">
        <h2 class="rd-panel-title">${escapeHtml(t("Panel.Inspector.Title"))}</h2>
        <p class="rd-copy">${escapeHtml(t("Panel.Inspector.Copy"))}</p>
      </section>
      <section class="rd-section" id="inspectorScroll">
        ${renderInspectorMarkup()}
      </section>
    </aside>
    ${renderHelpModal()}
  `;

  shellElement.classList.add("rd-focusable");
  shellElement.tabIndex = 0;
  applyPanelSizes();
  restoreShellViewportState(viewportState);
  return previewModel;
}

function renderSchemaTree(fields, parentPath = "", depth = 0) {
  if (!Array.isArray(fields) || fields.length === 0) {
    return `<div class="rd-empty">${escapeHtml(t("Panel.Schema.Empty"))}</div>`;
  }

  return fields
    .map((field) => {
      const fieldName = field.name || "";
      const nodePath = buildSchemaNodePath(parentPath, fieldName);
      const hasChildren = Array.isArray(field.children) && field.children.length > 0;
      const isExpanded = hasChildren ? isSchemaNodeExpanded(nodePath, depth) : false;
      const displayName = field.displayName || field.name || "(unnamed field)";
      const metaParts = [field.type || "unknown"];
      if (field.isCollection) {
        metaParts.push(t("Schema.Collection"));
      }

      return `
        <div class="rd-schema-node-compact" data-depth="${depth}">
          <div class="rd-schema-row" style="--rd-schema-depth:${depth};">
            <button
              type="button"
              class="rd-schema-toggle${hasChildren ? "" : " is-spacer"}"
              ${hasChildren ? `data-action="toggle-schema-node" data-path="${escapeAttribute(nodePath)}" aria-label="${escapeAttribute(displayName)}"` : "tabindex=\"-1\" aria-hidden=\"true\""}
              aria-expanded="${hasChildren ? String(isExpanded) : "false"}">
              ${hasChildren ? `<span class="rd-schema-toggle-glyph">${isExpanded ? "▾" : "▸"}</span>` : ""}
            </button>
            <div class="rd-schema-main">
              <span class="rd-schema-name">${escapeHtml(displayName)}</span>
              <span class="rd-schema-meta">${escapeHtml(metaParts.join(" · "))}</span>
            </div>
            ${nodePath ? `
              <button
                type="button"
                class="rd-schema-copy${state.copiedSchemaPath === nodePath ? " is-copied" : ""}"
                data-action="copy-schema-path"
                data-path="${escapeAttribute(nodePath)}"
                title="${escapeAttribute(nodePath)}">
                ${escapeHtml(state.copiedSchemaPath === nodePath ? t("Schema.Copied") : t("Schema.CopyPath"))}
              </button>
            ` : ""}
          </div>
          ${field.description ? `<div class="rd-schema-description" style="--rd-schema-depth:${depth};">${escapeHtml(field.description)}</div>` : ""}
          ${hasChildren && isExpanded ? `<div class="rd-schema-children-compact">${renderSchemaTree(field.children, nodePath, depth + 1)}</div>` : ""}
        </div>
      `;
    })
    .join("");
}

function formatPageSummary() {
  const page = state.definition.page || createDefaultDefinition().page;
  const orientationKey = String(page.orientation || "Portrait").toLowerCase() === "landscape"
    ? "Inspector.Page.Orientation.Landscape"
    : "Inspector.Page.Orientation.Portrait";

  return formatText(
    "Surface.PageSummary",
    page.size || "A4",
    t(orientationKey),
    page.margin || "20mm");
}

function getSectionLabel(sectionKey) {
  const normalizedKey = String(sectionKey || "");
  const titleKey = normalizedKey.charAt(0).toUpperCase() + normalizedKey.slice(1);
  return t(`Section.${titleKey}`);
}

function renderSectionBlocks(section) {
  if (Array.isArray(section.blocks) && section.blocks.length > 0) {
    return `<div class="rd-design-list">${renderBlockList(section.blocks)}</div>`;
  }

  return `<div class="rd-empty">${escapeHtml(getSectionLabel(section.key))}</div>`;
}

function renderSectionMeta(section) {
  const meta = [t(section.copyKey)];
  if (section.key === "pageHeader") {
    if (state.definition.pageHeaderSkipFirstPage) {
      meta.push(t("Inspector.Section.SkipFirstPage"));
    }
    if (state.definition.pageHeaderSkipLastPage) {
      meta.push(t("Inspector.Section.SkipLastPage"));
    }
  }

  if (section.key === "pageFooter") {
    if (state.definition.pageFooterSkipFirstPage) {
      meta.push(t("Inspector.Section.SkipFirstPage"));
    }
    if (state.definition.pageFooterSkipLastPage) {
      meta.push(t("Inspector.Section.SkipLastPage"));
    }
  }

  return meta.join(" · ");
}

function renderDesignMarkup() {
  const orientation = String(state.definition.page.orientation || "Portrait").toLowerCase();
  const pageClass = orientation === "landscape" ? "rd-page landscape" : "rd-page";
  const pageMargin = state.definition.page.margin || "20mm";
  const isPageSelected = isPageSelection();
  const sections = getAllSectionEntries();

  return `
    <div class="rd-design-workspace">
      <section class="${pageClass} rd-design-page${isPageSelected ? " is-page-selected" : ""}" data-action="select-page">
        <div class="rd-design-page-canvas" style="padding:${escapeAttribute(pageMargin)};">
          <div class="rd-design-page-body">
            ${sections.map((section) => `
              <div class="rd-design-section${state.selectedBlockId === section.targetId ? " is-page-selected" : ""}" data-action="select-section" data-section-key="${escapeAttribute(section.key)}">
                <div class="rd-block-head">
                  <div class="rd-block-title">
                    <span class="rd-chip">${escapeHtml(getSectionLabel(section.key))}</span>
                  </div>
                </div>
                <div class="rd-block-meta">${escapeHtml(renderSectionMeta(section))}</div>
                ${renderSectionBlocks(section)}
              </div>
            `).join("")}
          </div>
        </div>
      </section>
    </div>
  `;
}

function renderBlockList(blocks) {
  return blocks
    .map((block) => {
      const isSelected = block.id === state.selectedBlockId;
      return `
        <div class="rd-block" data-action="select-block" data-block-id="${escapeAttribute(block.id)}" data-selected="${isSelected ? "true" : "false"}">
          <div class="rd-block-head">
            <div class="rd-block-title">
              <span class="rd-chip">${escapeHtml(getBlockTypeLabel(block.type))}</span>
              <span class="rd-block-name">${escapeHtml(block.name || getDefaultBlockName(block.type))}</span>
            </div>
            <div class="rd-block-actions">
              <button type="button" class="rd-action-button" data-action="move-block-up" data-block-id="${escapeAttribute(block.id)}">${escapeHtml(t("Action.MoveUp"))}</button>
              <button type="button" class="rd-action-button" data-action="move-block-down" data-block-id="${escapeAttribute(block.id)}">${escapeHtml(t("Action.MoveDown"))}</button>
              <button type="button" class="rd-action-button" data-action="delete-block" data-block-id="${escapeAttribute(block.id)}">${escapeHtml(t("Action.Delete"))}</button>
            </div>
          </div>
          <div class="rd-block-meta">${buildBlockMeta(block)}</div>
          ${block.children && block.children.length > 0 ? `<div class="rd-block-children">${renderBlockList(block.children)}</div>` : ""}
        </div>
      `;
    })
    .join("");
}

function buildBlockMeta(block) {
  const rows = [];

  if (block.type === "Text") {
    rows.push(block.binding
      ? `${escapeHtml(t("Meta.Binding"))}: <strong>${escapeHtml(block.binding)}</strong>`
      : `${escapeHtml(t("Meta.Text"))}: <strong>${escapeHtml(shorten(block.text || "", 160))}</strong>`);
  }

  if (block.type === "Image") {
    rows.push(block.binding
      ? `${escapeHtml(t("Meta.Binding"))}: <strong>${escapeHtml(block.binding)}</strong>`
      : `${escapeHtml(t("Meta.Source"))}: <strong>${escapeHtml(shorten(block.imageSource || "", 100))}</strong>`);
  }

  if (block.type === "Table" || block.type === "Repeater") {
    rows.push(`${escapeHtml(t("Meta.ItemsSource"))}: <strong>${escapeHtml(block.itemsSource || "(empty)")}</strong>`);
  }

  if (block.type === "Table") {
    rows.push(`${escapeHtml(t("Meta.Columns"))}: <strong>${block.columns.length}</strong>`);
  }

  if (block.type === "FieldList") {
    rows.push(`${escapeHtml(t("Meta.Fields"))}: <strong>${block.fields.length}</strong>`);
  }

  if (block.type === "Columns") {
    rows.push(`${escapeHtml(t("Meta.ColumnCount"))}: <strong>${block.columnCount || block.children.length}</strong>`);
    rows.push(`${escapeHtml(t("Meta.Children"))}: <strong>${block.children.length}</strong>`);
  }

  if (block.type === "Container" || block.type === "Repeater") {
    rows.push(`${escapeHtml(t("Meta.Children"))}: <strong>${block.children.length}</strong>`);
  }

  if (block.type === "Barcode") {
    rows.push(`${escapeHtml(t("Inspector.Barcode.Type"))}: <strong>${escapeHtml(block.barcodeType)}</strong>`);
  }

  if (block.type === "QrCode") {
    rows.push(`${escapeHtml(t("Inspector.QrCode.Size"))}: <strong>${escapeHtml(block.size || "140px")}</strong>`);
  }

  if (block.type === "SpecialField") {
    rows.push(`${escapeHtml(t("Meta.SpecialField"))}: <strong>${escapeHtml(t(`Option.SpecialField.${normalizeSpecialFieldKind(block.specialFieldKind)}`))}</strong>`);
  }

  if (block.type === "PageBreak") {
    rows.push(escapeHtml(t("Meta.PageBreak")));
  }

  if (rows.length === 0) {
    rows.push(escapeHtml(t("Meta.Controlled")));
  }

  return rows.join("<br />");
}

function renderInspectorMarkup() {
  const pageSelected = isPageSelection();
  const selectedSectionKey = pageSelected ? "" : getSelectedSectionKey();
  const selectedBlock = pageSelected ? null : findBlockById(state.selectedBlockId);

  return `
    <div class="rd-inspector-body">
      ${pageSelected ? `
        <div class="rd-inspector-note">${escapeHtml(t("Inspector.Page.SelectedNote"))}</div>
        ${renderPageInspector()}
      ` : selectedSectionKey ? `
        <div class="rd-inspector-note">${escapeHtml(formatText("Inspector.Section.SelectedNote", getSectionLabel(selectedSectionKey)))}</div>
        ${renderSectionInspector(selectedSectionKey)}
      ` : selectedBlock ? `
        <div class="rd-inspector-note">${escapeHtml(formatText("Inspector.SelectedNote", getBlockTypeLabel(selectedBlock.type)))}</div>
        ${renderField(t("Inspector.Block.Name"), "blockField", "name", selectedBlock.name, false, "", "text", [], "page")}
        ${selectedBlock.type === "Text" ? renderField(t("Inspector.Block.Text"), "blockField", "text", selectedBlock.text, true, "", "text", [], "fontFamily") : ""}
        ${selectedBlock.type === "Text" || selectedBlock.type === "Image" || selectedBlock.type === "Barcode" || selectedBlock.type === "QrCode" ? renderField(t("Inspector.Block.Binding"), "blockField", "binding", selectedBlock.binding, false, "", "text", [], "display") : ""}
        ${selectedBlock.type === "Image" ? renderField(t("Inspector.Block.ImageSource"), "blockField", "imageSource", selectedBlock.imageSource, true, t("Placeholder.ImageSource"), "text", [], "page") : ""}
        ${selectedBlock.type === "Table" || selectedBlock.type === "Repeater" ? renderField(t("Inspector.Block.ItemsSource"), "blockField", "itemsSource", selectedBlock.itemsSource, false, "", "text", [], "display") : ""}
        ${renderFormatInspector(selectedBlock)}
        ${renderColumnsLayoutInspector(selectedBlock)}
        ${renderColumnsInspector(selectedBlock)}
        ${renderFieldListInspector(selectedBlock)}
        ${renderSpecialFieldInspector(selectedBlock)}
        ${renderBarcodeInspector(selectedBlock)}
        ${renderQrCodeInspector(selectedBlock)}
        ${renderPaginationInspector(selectedBlock)}
        ${renderStyleInspector(selectedBlock)}
        <button type="button" class="rd-inline-button" data-action="clear-selection">${escapeHtml(t("Action.ClearSelection"))}</button>
      ` : `
        ${renderPageInspector()}
      `}
    </div>
  `;
}

function renderSectionInspector(sectionKey) {
  const isPageBand = sectionKey === "pageHeader" || sectionKey === "pageFooter";
  const skipFirstPage = sectionKey === "pageHeader"
    ? state.definition.pageHeaderSkipFirstPage
    : sectionKey === "pageFooter"
      ? state.definition.pageFooterSkipFirstPage
      : false;
  const skipLastPage = sectionKey === "pageHeader"
    ? state.definition.pageHeaderSkipLastPage
    : sectionKey === "pageFooter"
      ? state.definition.pageFooterSkipLastPage
      : false;

  return `
    <div class="rd-preview-card">
      <div class="rd-panel-title">${renderLabelContent(getSectionLabel(sectionKey), "page")}</div>
      <div class="rd-copy">${escapeHtml(t(`Section.Copy.${sectionKey.charAt(0).toUpperCase()}${sectionKey.slice(1)}`))}</div>
      ${isPageBand ? `
        <div class="rd-inspector-body" style="margin-top:12px;">
          <div class="rd-panel-title">${renderLabelContent(t("Inspector.Section.Options"), "page")}</div>
          ${renderCheckboxField(t("Inspector.Section.SkipFirstPage"), "sectionField", "skipFirstPage", skipFirstPage, "page")}
          ${renderCheckboxField(t("Inspector.Section.SkipLastPage"), "sectionField", "skipLastPage", skipLastPage, "page")}
        </div>
      ` : ""}
      <button type="button" class="rd-inline-button" data-action="clear-selection">${escapeHtml(t("Action.ClearSelection"))}</button>
    </div>
  `;
}

function renderPageInspector() {
  return `
    <div class="rd-preview-card">
      <div class="rd-panel-title">${renderLabelContent(t("Inspector.Page.Title"), "page")}</div>
      <div class="rd-inspector-body" style="margin-top:12px;">
        ${renderSelectField(t("Inspector.Page.Size"), "pageField", "size", state.definition.page.size, [
          { value: "A4", text: "A4" },
          { value: "A5", text: "A5" },
          { value: "Letter", text: "Letter" },
        ], "page")}
        ${renderSelectField(t("Inspector.Page.Orientation"), "pageField", "orientation", state.definition.page.orientation, [
          { value: "Portrait", text: t("Inspector.Page.Orientation.Portrait") },
          { value: "Landscape", text: t("Inspector.Page.Orientation.Landscape") },
        ], "display")}
        ${renderLengthEditor(t("Inspector.Page.Margin"), "pageField", "margin", state.definition.page.margin, {
          defaultUnit: "mm",
          units: ["mm", "px"],
          min: "0",
          step: "1",
          placeholder: t("Placeholder.PageMargin"),
          iconName: "margin",
        })}
      </div>
    </div>
  `;
}

function renderFormatInspector(block) {
  if (!block || (block.type !== "Text" && block.type !== "SpecialField")) {
    return "";
  }

  if (block.type === "SpecialField" && normalizeSpecialFieldKind(block.specialFieldKind) !== "CurrentDate") {
    return "";
  }

  return `
    <div class="rd-preview-card">
      <div class="rd-panel-title">${renderLabelContent(t("Inspector.Format.Title"), "fontFamily")}</div>
      <div class="rd-inspector-body" style="margin-top:12px;">
        ${block.type === "SpecialField"
          ? ""
          : renderSelectField(t("Inspector.Format.Kind"), "formatField", "kind", block.format.kind, FORMAT_KINDS.map((kind) => ({ value: kind, text: t(`Format.${kind}`) })), "display")}
        ${renderField(t("Inspector.Format.Pattern"), "formatField", "pattern", block.format.pattern, false, t("Placeholder.DatePattern"))}
        ${block.type === "SpecialField"
          ? ""
          : renderField(t("Inspector.Format.Currency"), "formatField", "currency", block.format.currency, false, t("Placeholder.Currency"))}
        ${block.type === "SpecialField"
          ? ""
          : renderField(t("Inspector.Format.Decimals"), "formatField", "decimals", block.format.decimals == null ? "" : String(block.format.decimals), false, "2", "number")}
      </div>
    </div>
  `;
}

function renderColumnsLayoutInspector(block) {
  if (!block || block.type !== "Columns") {
    return "";
  }

  return `
    <div class="rd-preview-card">
      <div class="rd-panel-title">${renderLabelContent(t("Inspector.ColumnsBlock.Title"), "display")}</div>
      <div class="rd-copy">${escapeHtml(t("Inspector.ColumnsBlock.Hint"))}</div>
      <div class="rd-inspector-body" style="margin-top:12px;">
        ${renderSelectField(t("Inspector.ColumnsBlock.Count"), "blockField", "columnCount", String(block.columnCount || 2), COLUMN_COUNT_OPTIONS.map((count) => ({
          value: String(count),
          text: t(count === 2 ? "Option.ColumnCount.Two" : "Option.ColumnCount.Three"),
        })), "display")}
        ${renderLengthEditor(t("Inspector.ColumnsBlock.Gap"), "blockField", "columnGap", block.columnGap, {
          defaultUnit: "px",
          units: ["px", "mm", "rem"],
          min: "0",
          step: "1",
          iconName: "margin",
        })}
      </div>
    </div>
  `;
}

function renderColumnsInspector(block) {
  if (!block || block.type !== "Table") {
    return "";
  }

  const value = formatTableColumnsText(block.columns || []);

  return `
    <div class="rd-preview-card">
      <div class="rd-panel-title">${renderLabelContent(t("Inspector.Columns.Title"), "display")}</div>
      <div class="rd-copy">${escapeHtml(t("Inspector.Columns.Copy"))}</div>
      <div class="rd-inspector-body" style="margin-top:12px;">
        ${renderField(t("Inspector.Columns.Title"), "columnsField", "columns", value, true)}
      </div>
    </div>
  `;
}

function renderFieldListInspector(block) {
  if (!block || block.type !== "FieldList") {
    return "";
  }

  const value = formatFieldListText(block.fields || []);

  return `
    <div class="rd-preview-card">
      <div class="rd-panel-title">${renderLabelContent(t("Inspector.FieldList.Title"), "display")}</div>
      <div class="rd-copy">${escapeHtml(t("Inspector.FieldList.Copy"))}</div>
      <div class="rd-inspector-body" style="margin-top:12px;">
        ${renderField(t("Inspector.FieldList.Title"), "fieldsField", "fields", value, true)}
      </div>
    </div>
  `;
}

function renderSpecialFieldInspector(block) {
  if (!block || block.type !== "SpecialField") {
    return "";
  }

  return `
    <div class="rd-preview-card">
      <div class="rd-panel-title">${renderLabelContent(t("Inspector.SpecialField.Title"), "page")}</div>
      <div class="rd-inspector-body" style="margin-top:12px;">
        ${renderSelectField(t("Inspector.SpecialField.Kind"), "blockField", "specialFieldKind", block.specialFieldKind, SPECIAL_FIELD_KINDS.map((kind) => ({
          value: kind,
          text: t(`Option.SpecialField.${kind}`),
        })), "page")}
      </div>
    </div>
  `;
}

function renderBarcodeInspector(block) {
  if (!block || block.type !== "Barcode") {
    return "";
  }

  return `
    <div class="rd-preview-card">
      <div class="rd-panel-title">${renderLabelContent(t("Inspector.Barcode.Title"), "barcode")}</div>
      <div class="rd-inspector-body" style="margin-top:12px;">
        ${renderSelectField(t("Inspector.Barcode.Type"), "blockField", "barcodeType", block.barcodeType, BARCODE_TYPES.map((type) => ({ value: type, text: type })), "barcode")}
        ${renderCheckboxField(t("Inspector.Barcode.ShowText"), "blockField", "showText", block.showText, "barcode")}
      </div>
    </div>
  `;
}

function renderQrCodeInspector(block) {
  if (!block || block.type !== "QrCode") {
    return "";
  }

  return `
    <div class="rd-preview-card">
      <div class="rd-panel-title">${renderLabelContent(t("Inspector.QrCode.Title"), "qr")}</div>
      <div class="rd-inspector-body" style="margin-top:12px;">
        ${renderLengthEditor(t("Inspector.QrCode.Size"), "blockField", "size", block.size, {
          defaultUnit: "px",
          units: ["px", "mm"],
          min: "24",
          step: "4",
          placeholder: t("Placeholder.Size"),
          iconName: "qr",
        })}
        ${renderSelectField(t("Inspector.QrCode.ErrorCorrection"), "blockField", "errorCorrectionLevel", block.errorCorrectionLevel, QR_ERROR_LEVELS.map((value) => ({ value, text: value })), "qr")}
      </div>
    </div>
  `;
}

function renderPaginationInspector(block) {
  if (!block || block.type === "PageBreak") {
    return "";
  }

  return `
    <div class="rd-preview-card">
      <div class="rd-panel-title">${renderLabelContent(t("Inspector.Pagination.Title"), "page")}</div>
      <div class="rd-inspector-body" style="margin-top:12px;">
        ${renderCheckboxField(t("Inspector.Pagination.PageBreakBefore"), "blockField", "pageBreakBefore", block.pageBreakBefore, "page")}
        ${renderCheckboxField(t("Inspector.Pagination.KeepTogether"), "blockField", "keepTogether", block.keepTogether, "page")}
        ${block.type === "Table"
          ? renderCheckboxField(t("Inspector.Pagination.RepeatHeader"), "blockField", "repeatHeader", block.repeatHeader !== false, "page")
          : ""}
      </div>
    </div>
  `;
}

function renderStyleInspector(block) {
  if (!block) {
    return "";
  }

  return `
    <div class="rd-preview-card">
      <div class="rd-panel-title">${renderLabelContent(t("Inspector.Style.Title"), "display")}</div>
      <div class="rd-inspector-body" style="margin-top:12px;">
        ${renderBoxSpacingEditor(t("Inspector.Style.Margin"), "margin", block.style.margin, "margin")}
        ${renderBoxSpacingEditor(t("Inspector.Style.Padding"), "padding", block.style.padding, "margin")}
        <div class="rd-field-row">
          ${renderSelectField(t("Inspector.Style.FontFamily"), "styleField", "fontFamily", block.style.fontFamily, FONT_FAMILY_OPTIONS.map((option) => ({ value: option.value, text: resolveOptionText(option) })), "fontFamily")}
          ${renderLengthEditor(t("Inspector.Style.FontSize"), "styleField", "fontSize", block.style.fontSize, {
            defaultUnit: "px",
            units: SIZE_UNITS,
            min: "1",
            step: "1",
            iconName: "fontSize",
          })}
        </div>
        <div class="rd-field-row">
          ${renderSelectField(t("Inspector.Style.FontWeight"), "styleField", "fontWeight", block.style.fontWeight, FONT_WEIGHT_OPTIONS.map((option) => ({ value: option.value, text: resolveOptionText(option) })), "fontWeight")}
          ${renderSelectField(t("Inspector.Style.FontStyle"), "styleField", "fontStyle", block.style.fontStyle, FONT_STYLE_OPTIONS.map((option) => ({ value: option.value, text: resolveOptionText(option) })), "fontStyle")}
        </div>
        <div class="rd-field-row">
          ${renderSelectField(t("Inspector.Style.TextDecoration"), "styleField", "textDecoration", block.style.textDecoration, TEXT_DECORATION_OPTIONS.map((option) => ({ value: option.value, text: resolveOptionText(option) })), "textDecoration")}
          ${renderSelectField(t("Inspector.Style.TextAlign"), "styleField", "textAlign", block.style.textAlign, TEXT_ALIGN_OPTIONS.map((option) => ({ value: option.value, text: resolveOptionText(option) })), "textAlign")}
        </div>
        <div class="rd-field-row">
          ${renderColorEditor(t("Inspector.Style.Color"), "styleField", "color", block.style.color, "#0f172a", "color")}
          ${renderColorEditor(t("Inspector.Style.BackgroundColor"), "styleField", "backgroundColor", block.style.backgroundColor, "#ffffff", "color")}
        </div>
        ${renderBorderEditor(block.style.border)}
        <div class="rd-field-row">
          ${renderSelectField(t("Inspector.Style.Width"), "styleField", "width", block.style.width, getDimensionOptions("width").map((option) => ({ value: option.value, text: resolveOptionText(option) })), "display")}
          ${renderSelectField(t("Inspector.Style.MinWidth"), "styleField", "minWidth", block.style.minWidth, getDimensionOptions("minWidth").map((option) => ({ value: option.value, text: resolveOptionText(option) })), "display")}
        </div>
        <div class="rd-field-row">
          ${renderSelectField(t("Inspector.Style.MaxWidth"), "styleField", "maxWidth", block.style.maxWidth, getDimensionOptions("maxWidth").map((option) => ({ value: option.value, text: resolveOptionText(option) })), "display")}
          ${renderSelectField(t("Inspector.Style.Height"), "styleField", "height", block.style.height, getDimensionOptions("height").map((option) => ({ value: option.value, text: resolveOptionText(option) })), "display")}
        </div>
        <div class="rd-field-row">
          ${renderLengthEditor(t("Inspector.Style.BorderRadius"), "styleField", "borderRadius", block.style.borderRadius, {
            defaultUnit: "px",
            units: ["px", "%"],
            min: "0",
            step: "1",
            iconName: "radius",
          })}
          ${renderSelectField(t("Inspector.Style.Display"), "styleField", "display", block.style.display, DISPLAY_OPTIONS.map((option) => ({ value: option.value, text: resolveOptionText(option) })), "display")}
        </div>
      </div>
    </div>
  `;
}

function renderBoxSpacingEditor(label, fieldName, value, iconName) {
  const parsed = parseBoxSpacingValue(value, "px");
  if (parsed.kind === "raw") {
    return renderField(label, "styleField", fieldName, value, false, "", "text", [], iconName);
  }

  return `
    <div class="rd-composite-card">
      <div class="rd-composite-summary">${renderSectionIcon(iconName)}<span>${escapeHtml(label)}: ${escapeHtml(composeBoxSpacingValue(parsed, parsed.unit || "px") || "0")}</span></div>
      <div class="rd-field-inline-row">
        <div class="rd-field-grid-four">
          ${renderSpacingSideField(fieldName, "top", parsed.top, t("Inspector.Style.Side.Top"))}
          ${renderSpacingSideField(fieldName, "right", parsed.right, t("Inspector.Style.Side.Right"))}
          ${renderSpacingSideField(fieldName, "bottom", parsed.bottom, t("Inspector.Style.Side.Bottom"))}
          ${renderSpacingSideField(fieldName, "left", parsed.left, t("Inspector.Style.Side.Left"))}
        </div>
        <div class="rd-field rd-field-mini">
          <label>${escapeHtml(t("Inspector.Style.SpacingUnit"))}</label>
          <select data-spacing-field="${escapeAttribute(fieldName)}" data-spacing-part="unit">
            ${BOX_SPACING_UNITS.map((unit) => `<option value="${escapeAttribute(unit)}"${unit === parsed.unit ? " selected" : ""}>${escapeHtml(unit)}</option>`).join("")}
          </select>
        </div>
      </div>
    </div>
  `;
}

function renderSpacingSideField(fieldName, side, value, label) {
  return `
    <div class="rd-field rd-field-mini">
      <label>${escapeHtml(label)}</label>
      <input type="number" step="1" data-spacing-field="${escapeAttribute(fieldName)}" data-spacing-part="${escapeAttribute(side)}" value="${escapeAttribute(value || "")}" />
    </div>
  `;
}

function renderLengthEditor(label, scopeKey, fieldName, value, options = {}) {
  const parsed = parseLengthValue(value, options.defaultUnit || "px");
  if (parsed.kind === "raw") {
    return renderField(label, scopeKey, fieldName, value, false, options.placeholder || "", "text", [], options.iconName || "");
  }

  const units = Array.isArray(options.units) && options.units.length > 0 ? options.units : ["px"];
  const amount = parsed.amount || "";

  return `
    <div class="rd-field">
      <label>${renderLabelContent(label, options.iconName || "")}</label>
      <div class="rd-field-unit-row">
        <input type="number"
          step="${escapeAttribute(String(options.step || "1"))}"
          ${options.min != null ? `min="${escapeAttribute(String(options.min))}"` : ""}
          data-length-scope="${escapeAttribute(scopeKey)}"
          data-length-field="${escapeAttribute(fieldName)}"
          data-length-part="amount"
          value="${escapeAttribute(amount)}"
          placeholder="${escapeAttribute(options.placeholder || "")}" />
        <select
          data-length-scope="${escapeAttribute(scopeKey)}"
          data-length-field="${escapeAttribute(fieldName)}"
          data-length-part="unit">
          ${units.map((unit) => `<option value="${escapeAttribute(unit)}"${unit === parsed.unit ? " selected" : ""}>${escapeHtml(unit)}</option>`).join("")}
        </select>
      </div>
    </div>
  `;
}

function renderBorderEditor(value) {
  const parsed = parseBorderValue(value);
  if (parsed.kind === "raw") {
    return renderField(t("Inspector.Style.Border"), "styleField", "border", value, false, "", "text", [], "border");
  }

  return `
    <div class="rd-composite-card">
      <div class="rd-composite-summary">${renderSectionIcon("border")}<span>${escapeHtml(t("Inspector.Style.BorderSummary"))}: ${escapeHtml(composeBorderValue(parsed))}</span></div>
      ${renderCheckboxField(t("Inspector.Style.Border.Enabled"), "borderField", "border", parsed.enabled, "border", "enabled")}
      <div class="rd-field-inline-row-three">
        <div class="rd-field rd-field-mini">
          <label>${renderLabelContent(t("Inspector.Style.BorderWidth"), "border")}</label>
          <input type="number" min="0" step="1" data-border-field="border" data-border-part="width" value="${escapeAttribute(parsed.width || "1")}" />
        </div>
        <div class="rd-field rd-field-mini">
          <label>${escapeHtml(t("Inspector.Style.SpacingUnit"))}</label>
          <select data-border-field="border" data-border-part="unit">
            ${SIZE_UNITS.map((unit) => `<option value="${escapeAttribute(unit)}"${unit === parsed.unit ? " selected" : ""}>${escapeHtml(unit)}</option>`).join("")}
          </select>
        </div>
        <div class="rd-field rd-field-mini">
          <label>${renderLabelContent(t("Inspector.Style.BorderStyle"), "border")}</label>
          <select data-border-field="border" data-border-part="style">
            ${BORDER_STYLE_OPTIONS.map((option) => `<option value="${escapeAttribute(option.value)}"${option.value === parsed.style ? " selected" : ""}>${escapeHtml(resolveOptionText(option))}</option>`).join("")}
          </select>
        </div>
      </div>
      ${renderColorEditor(t("Inspector.Style.BorderColor"), "borderField", "border", parsed.color, t("Placeholder.BorderColor"), "color", "color")}
    </div>
  `;
}

function buildScopeAttributes(scopeKey, fieldName, part = "") {
  if (scopeKey === "pageField") {
    return `data-page-field="${escapeAttribute(fieldName)}"`;
  }

  if (scopeKey === "blockField") {
    return `data-block-field="${escapeAttribute(fieldName)}"`;
  }

  if (scopeKey === "styleField") {
    return `data-style-field="${escapeAttribute(fieldName)}"`;
  }

  if (scopeKey === "formatField") {
    return `data-format-field="${escapeAttribute(fieldName)}"`;
  }

  if (scopeKey === "borderField") {
    const safeFieldName = escapeAttribute(fieldName);
    const safePart = escapeAttribute(part || fieldName);
    return `data-border-field="${safeFieldName}" data-border-part="${safePart}"`;
  }

  if (scopeKey === "fieldsField") {
    return `data-fields-field="${escapeAttribute(fieldName)}"`;
  }

  return `data-columns-field="${escapeAttribute(fieldName)}"`;
}

function renderColorEditor(label, scopeKey, fieldName, value, placeholder, iconName = "color", part = "") {
  const safeValue = String(value || "").trim();
  const normalizedSelection = normalizeHexColor(safeValue);
  const pickerValue = toColorInputValue(safeValue, placeholder || "#0f172a");
  const colorPart = part || fieldName;

  return `
    <div class="rd-field">
      <label>${renderLabelContent(label, iconName)}</label>
      <div class="rd-color-stack">
        <div class="rd-color-input-row">
          <input
            type="text"
            ${buildScopeAttributes(scopeKey, fieldName, part)}
            value="${escapeAttribute(safeValue)}"
            placeholder="${escapeAttribute(placeholder || "")}"
            spellcheck="false"
            autocomplete="off" />
          <input
            type="color"
            class="rd-color-picker"
            data-color-picker="true"
            data-color-scope="${escapeAttribute(scopeKey)}"
            data-color-field="${escapeAttribute(fieldName)}"
            data-color-part="${escapeAttribute(colorPart)}"
            value="${escapeAttribute(pickerValue)}"
            aria-label="${escapeAttribute(label)}" />
        </div>
        <div class="rd-color-swatch-grid">
          ${BASIC_COLOR_SWATCHES.map((swatch) => `
            <button
              type="button"
              class="rd-color-swatch${normalizedSelection === swatch ? " is-selected" : ""}"
              data-action="apply-color-swatch"
              data-color-scope="${escapeAttribute(scopeKey)}"
              data-color-field="${escapeAttribute(fieldName)}"
              data-color-part="${escapeAttribute(colorPart)}"
              data-color-value="${escapeAttribute(swatch)}"
              title="${escapeAttribute(swatch)}"
              aria-label="${escapeAttribute(`${label}: ${swatch}`)}"
              style="--rd-swatch-color:${escapeAttribute(swatch)};">
            </button>
          `).join("")}
        </div>
      </div>
    </div>
  `;
}

function renderField(label, scopeKey, fieldName, value, multiline = false, placeholder = "", type = "text", suggestions = [], iconName = "") {
  const control = multiline
    ? `<textarea ${buildScopeAttributes(scopeKey, fieldName)} placeholder="${escapeAttribute(placeholder)}">${escapeHtml(value || "")}</textarea>`
    : `<input type="${escapeAttribute(type)}" ${buildScopeAttributes(scopeKey, fieldName)} value="${escapeAttribute(value || "")}" placeholder="${escapeAttribute(placeholder)}" />`;

  return `
    <div class="rd-field">
      <label>${renderLabelContent(label, iconName)}</label>
      ${control}
    </div>
  `;
}

function renderSelectField(label, scopeKey, fieldName, value, items, iconName = "") {
  return `
    <div class="rd-field">
      <label>${renderLabelContent(label, iconName)}</label>
      <select ${buildScopeAttributes(scopeKey, fieldName)}>
        ${items.map((item) => `<option value="${escapeAttribute(item.value)}"${String(item.value) === String(value) ? " selected" : ""}>${escapeHtml(item.text)}</option>`).join("")}
      </select>
    </div>
  `;
}

function renderCheckboxField(label, scopeKey, fieldName, checked, iconName = "", part = "") {
  return `
    <div class="rd-field">
      <label>${renderLabelContent(label, iconName)}</label>
      <label class="rd-checkbox-row">
        <input type="checkbox" ${buildScopeAttributes(scopeKey, fieldName, part)}${checked ? " checked" : ""} />
        <span>${escapeHtml(label)}</span>
      </label>
    </div>
  `;
}

function renderPreviewMarkup(previewModel) {
  if (!previewModel.pages.length) {
    return `<div class="rd-empty">${escapeHtml(t("Surface.Empty.Preview"))}</div>`;
  }

  return `<div class="rd-preview-stack">${previewModel.pages.map((page) => renderPreviewPage(page)).join("")}</div>`;
}

function buildBodyPages(blocks, data) {
  const pages = [];
  let currentBlocks = [];

  const flushPage = () => {
    pages.push({
      blocks: currentBlocks.slice(),
      data,
    });
    currentBlocks = [];
  };

  for (const block of blocks || []) {
    if (block.pageBreakBefore && currentBlocks.length > 0) {
      flushPage();
    }

    if (block.type === "PageBreak") {
      flushPage();
      continue;
    }

    currentBlocks.push(block);
  }

  if (currentBlocks.length > 0 || pages.length === 0) {
    flushPage();
  }

  return pages.map((page, index) => ({
    ...page,
    pageNumber: index + 1,
    totalPages: pages.length,
  }));
}

function buildPages(data) {
  const bodyPages = buildBodyPages(state.definition.blocks, data);
  const totalPages = bodyPages.length;

  return bodyPages.map((page, index) => {
    const pageNumber = index + 1;
    return {
      ...page,
      pageNumber,
      totalPages,
      reportHeaderBlocks: pageNumber === 1 ? (state.definition.reportHeaderBlocks || []) : [],
      reportFooterBlocks: pageNumber === totalPages ? (state.definition.reportFooterBlocks || []) : [],
      pageHeaderBlocks: shouldRenderPageSection(
        pageNumber,
        totalPages,
        state.definition.pageHeaderSkipFirstPage,
        state.definition.pageHeaderSkipLastPage)
        ? (state.definition.pageHeaderBlocks || [])
        : [],
      pageFooterBlocks: shouldRenderPageSection(
        pageNumber,
        totalPages,
        state.definition.pageFooterSkipFirstPage,
        state.definition.pageFooterSkipLastPage)
        ? (state.definition.pageFooterBlocks || [])
        : [],
    };
  });
}

function renderPreviewPage(page) {
  const orientation = String(state.definition.page.orientation || "Portrait").toLowerCase();
  const pageMargin = state.definition.page.margin || "20mm";
  const pageClass = orientation === "landscape" ? "rd-page landscape" : "rd-page";
  const pageSelected = isPageSelection();
  const pageSelectionClass = pageSelected ? `${pageClass} is-page-selected` : pageClass;
  const pagePulse = state.selectionPulseTargetId === SELECTED_PAGE_ID;
  const pageSelectionAttributes = ` data-action="select-page" data-selected="${pageSelected ? "true" : "false"}" data-pulse="${pagePulse ? "true" : "false"}"`;

  return `
    <section class="${pageSelectionClass}"${pageSelectionAttributes}>
      <div class="rd-page-content" style="padding:${escapeAttribute(pageMargin)};">
        ${page.reportHeaderBlocks.map((block) => renderBlockPreview(block, page.data, page.data, page)).join("")}
        ${page.pageHeaderBlocks.map((block) => renderBlockPreview(block, page.data, page.data, page)).join("")}
        ${page.blocks.map((block) => renderBlockPreview(block, page.data, page.data, page)).join("")}
        ${page.reportFooterBlocks.map((block) => renderBlockPreview(block, page.data, page.data, page)).join("")}
        ${page.pageFooterBlocks.map((block) => renderBlockPreview(block, page.data, page.data, page)).join("")}
      </div>
    </section>
  `;
}

function buildPreviewSelectionAttributes(block, styleAttribute = "") {
  const selected = isSelectionTargetSelected(block && block.id, state.selectedBlockId);
  const pulse = isSelectionTargetSelected(block && block.id, state.selectionPulseTargetId);
  const blockId = block && block.id ? block.id : "";
  return `${styleAttribute} data-action="select-block" data-block-id="${escapeAttribute(blockId)}" data-selected="${selected ? "true" : "false"}" data-pulse="${pulse ? "true" : "false"}"`;
}

function renderBlockPreview(block, context, rootData, pageContext) {
  const styles = styleToCss(block.style);
  let styleAttribute = styles ? ` style="${escapeAttribute(styles)}"` : "";
  if (block.keepTogether) {
    styleAttribute = appendCss(styleAttribute, "break-inside:avoid;page-break-inside:avoid;");
  }
  if (block.pageBreakBefore) {
    styleAttribute = appendCss(styleAttribute, "break-before:page;page-break-before:always;");
  }

  switch (block.type) {
    case "Columns": {
      const columnCount = clampColumnCount(block.columnCount || block.children.length || 2);
      const gap = block.columnGap || "18px";
      const columnsStyle = appendCss(styleAttribute, `display:grid;grid-template-columns:repeat(${columnCount}, minmax(0, 1fr));column-gap:${gap};align-items:start;`);
      return `<div class="rd-preview-block rd-preview-columns"${buildPreviewSelectionAttributes(block, columnsStyle)}>${(block.children || []).map((child) => renderBlockPreview(child, context, rootData, pageContext)).join("")}</div>`;
    }

    case "FieldList": {
      const items = Array.isArray(block.fields) ? block.fields : [];
      return `<div class="rd-preview-block rd-preview-field-list"${buildPreviewSelectionAttributes(block, styleAttribute)}>${items.map((item) => `
        <div class="rd-preview-field-row">
          <div class="rd-preview-field-label">${escapeHtml(item.label || "")}</div>
          <div class="rd-preview-field-value">${escapeHtml(formatValue(resolvePath(context, item.binding, rootData), item.format))}</div>
        </div>
      `).join("")}</div>`;
    }

    case "SpecialField": {
      const value = resolveSpecialFieldValue(block.specialFieldKind, {
        locale: state.locale,
        currentPageNumber: pageContext?.pageNumber || 1,
        totalPages: pageContext?.totalPages || 1,
        datePattern: block.format?.pattern || "",
      });
      return `<div class="rd-preview-block"${buildPreviewSelectionAttributes(block, styleAttribute)}>${escapeHtml(value)}</div>`;
    }

    case "Container":
      return `<div class="rd-preview-block ${hasContainerChrome(block.style) ? "rd-preview-card" : "rd-preview-section"}"${buildPreviewSelectionAttributes(block, styleAttribute)}>${(block.children || []).map((child) => renderBlockPreview(child, context, rootData, pageContext)).join("")}</div>`;

    case "Text": {
      const rawValue = block.binding ? resolvePath(context, block.binding, rootData) : block.text;
      const formatted = formatValue(rawValue, block.format);
      return `<div class="rd-preview-block"${buildPreviewSelectionAttributes(block, styleAttribute)}>${escapeHtml(formatted)}</div>`;
    }

    case "Image": {
      const resolvedSource = block.binding ? resolvePath(context, block.binding, rootData) : block.imageSource;
      if (!resolvedSource) {
        return `<div class="rd-preview-block rd-empty"${buildPreviewSelectionAttributes(block, styleAttribute)}>${escapeHtml(t("Preview.EmptyImage"))}</div>`;
      }

      return `<img class="rd-image rd-preview-block"${buildPreviewSelectionAttributes(block, styleAttribute)} src="${escapeAttribute(String(resolvedSource))}" alt="${escapeAttribute(block.name || "Report image")}" />`;
    }

    case "Table": {
      const rows = resolvePath(context, block.itemsSource, rootData);
      const items = Array.isArray(rows) ? rows : [];
      const columns = Array.isArray(block.columns) ? block.columns : [];
      const hasColumnWidths = columns.some((column) => !!column.width);
      const colGroupHtml = hasColumnWidths
        ? `<colgroup>${columns.map((column) => `<col${column.width ? ` style="width:${escapeAttribute(column.width)};"` : ""} />`).join("")}</colgroup>`
        : "";
      const headerCells = columns.map((column) => `<th${buildTableCellStyle(column)}>${escapeHtml(column.header || column.binding || "")}</th>`).join("");
      const bodyHtml = items.length > 0
        ? items.map((item) => `
            <tr>
              ${columns.map((column) => `<td${buildTableCellStyle(column)}>${escapeHtml(formatValue(resolvePath(item, column.binding, rootData), column.format))}</td>`).join("")}
            </tr>
          `).join("")
        : `<tr><td colspan="${Math.max(columns.length, 1)}">${escapeHtml(t("Preview.EmptyRows"))}</td></tr>`;
      const headerSection = block.repeatHeader === false
        ? ""
        : `<thead><tr>${headerCells}</tr></thead>`;
      const bodyRows = block.repeatHeader === false
        ? `<tr class="rd-preview-table-header-row">${headerCells}</tr>${bodyHtml}`
        : bodyHtml;

      return `<div class="rd-preview-block"${buildPreviewSelectionAttributes(block, styleAttribute)}><table class="rd-preview-table">${colGroupHtml}${headerSection}<tbody>${bodyRows}</tbody></table></div>`;
    }

    case "Repeater": {
      const rows = resolvePath(context, block.itemsSource, rootData);
      const items = Array.isArray(rows) ? rows : [];
      if (items.length === 0) {
        return `<div class="rd-preview-block rd-empty"${buildPreviewSelectionAttributes(block, styleAttribute)}>${escapeHtml(t("Preview.EmptyRepeater"))}</div>`;
      }

      return `<div class="rd-preview-block rd-preview-repeater"${buildPreviewSelectionAttributes(block, styleAttribute)}>${items.map((item) => `<div class="rd-preview-section">${(block.children || []).map((child) => renderBlockPreview(child, item, rootData, pageContext)).join("")}</div>`).join("")}</div>`;
    }

    case "Barcode":
      return renderBarcodePreview(block, context, rootData, styleAttribute);

    case "QrCode":
      return renderQrCodePreview(block, context, rootData, styleAttribute);

    case "PageBreak":
      return "";

    default:
      return "";
  }
}

function buildTableCellStyle(column) {
  const parts = [];
  if (column && column.textAlign) {
    parts.push(`text-align:${column.textAlign}`);
  }

  return parts.length > 0 ? ` style="${escapeAttribute(parts.join(";"))}"` : "";
}

function hasContainerChrome(style) {
  const safeStyle = normalizeStyle(style);
  return !!(safeStyle.padding || safeStyle.backgroundColor || safeStyle.border || safeStyle.borderRadius);
}

function renderBarcodePreview(block, context, rootData, styleAttribute) {
  const value = block.binding ? resolvePath(context, block.binding, rootData) : block.text;
  if (value == null || value === "") {
    return `<div class="rd-preview-block rd-empty"${buildPreviewSelectionAttributes(block, styleAttribute)}>${escapeHtml(t("Preview.Barcode.Empty"))}</div>`;
  }

  try {
    const formatMap = {
      Code128: "CODE128",
      EAN13: "EAN13",
      EAN8: "EAN8",
      Code39: "CODE39",
    };
    const format = formatMap[normalizeBarcodeType(block.barcodeType)] || "CODE128";
    const width = parseCssNumber(block.style.width, 260);
    const height = parseCssNumber(block.style.height, 82);
    const svg = document.createElementNS("http://www.w3.org/2000/svg", "svg");
    window.JsBarcode(svg, String(value), {
      format,
      displayValue: block.showText !== false,
      width: format === "CODE128" ? 1.8 : 2,
      height: Math.max(40, height - (block.showText !== false ? 20 : 4)),
      margin: 6,
      fontSize: 14,
      background: "#ffffff",
      lineColor: "#111827",
    });

    const wrapperStyle = appendCss(styleAttribute, `width:${width}px;min-height:${height}px;`);
    return `<div class="rd-preview-block"${buildPreviewSelectionAttributes(block, wrapperStyle)}><div class="rd-code-block">${svg.outerHTML}</div></div>`;
  } catch (_) {
    return `<div class="rd-preview-block rd-empty"${buildPreviewSelectionAttributes(block, styleAttribute)}>${escapeHtml(formatText("Preview.Barcode.Invalid", normalizeBarcodeType(block.barcodeType)))}</div>`;
  }
}

function renderQrCodePreview(block, context, rootData, styleAttribute) {
  const value = block.binding ? resolvePath(context, block.binding, rootData) : block.text;
  if (value == null || value === "") {
    return `<div class="rd-preview-block rd-empty"${buildPreviewSelectionAttributes(block, styleAttribute)}>${escapeHtml(t("Preview.QrCode.Empty"))}</div>`;
  }

  try {
    const size = parseCssNumber(block.size || block.style.width, 140);
    const qr = window.qrcode(0, normalizeQrErrorLevel(block.errorCorrectionLevel));
    qr.addData(String(value), "Byte");
    qr.make();
    const moduleCount = qr.getModuleCount();
    const cellSize = Math.max(2, Math.floor(size / Math.max(moduleCount, 1)));
    const svgMarkup = qr.createSvgTag(cellSize, 2);
    const wrapperStyle = appendCss(styleAttribute, `width:${size}px;height:${size}px;`);
    return `<div class="rd-preview-block"${buildPreviewSelectionAttributes(block, wrapperStyle)}><div class="rd-code-block rd-qr">${svgMarkup}</div></div>`;
  } catch (_) {
    return `<div class="rd-preview-block rd-empty"${buildPreviewSelectionAttributes(block, styleAttribute)}>${escapeHtml(t("Preview.QrCode.Invalid"))}</div>`;
  }
}

function appendCss(styleAttribute, rawCss) {
  if (!rawCss) {
    return styleAttribute || "";
  }

  const css = rawCss.endsWith(";") ? rawCss : `${rawCss};`;
  if (!styleAttribute) {
    return ` style="${escapeAttribute(css)}"`;
  }

  const current = styleAttribute.slice(8, -1);
  return ` style="${current}${escapeAttribute(css)}"`;
}

function parseCssNumber(value, fallbackValue) {
  if (!value) {
    return fallbackValue;
  }

  const parsed = Number.parseFloat(String(value).replace(",", "."));
  return Number.isFinite(parsed) ? parsed : fallbackValue;
}

function resolvePath(context, path, rootData) {
  if (!path) {
    return "";
  }

  const segments = String(path)
    .split(".")
    .map((segment) => segment.trim())
    .filter(Boolean);

  const fromContext = resolveFromObject(context, segments);
  if (fromContext !== undefined) {
    return fromContext;
  }

  return resolveFromObject(rootData, segments);
}

function resolveFromObject(source, segments) {
  let current = source;
  for (const segment of segments) {
    if (current == null || typeof current !== "object" || !(segment in current)) {
      return undefined;
    }
    current = current[segment];
  }

  return current;
}

function formatValue(value, format) {
  if (value == null) {
    return "";
  }

  const safeFormat = normalizeFormat(format);
  if (safeFormat.kind === "date") {
    const date = new Date(value);
    if (Number.isNaN(date.getTime())) {
      return String(value);
    }

    if (safeFormat.pattern === "yyyy-MM-dd") {
      return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, "0")}-${String(date.getDate()).padStart(2, "0")}`;
    }

    return date.toLocaleDateString(state.locale);
  }

  if (safeFormat.kind === "number") {
    const number = Number(value);
    if (!Number.isFinite(number)) {
      return String(value);
    }

    const decimals = safeFormat.decimals == null ? 2 : safeFormat.decimals;
    return number.toLocaleString(state.locale, {
      minimumFractionDigits: decimals,
      maximumFractionDigits: decimals,
    });
  }

  if (safeFormat.kind === "currency") {
    const number = Number(value);
    if (!Number.isFinite(number)) {
      return String(value);
    }

    const decimals = safeFormat.decimals == null ? 2 : safeFormat.decimals;
    return new Intl.NumberFormat(state.locale, {
      style: "currency",
      currency: safeFormat.currency || "USD",
      minimumFractionDigits: decimals,
      maximumFractionDigits: decimals,
    }).format(number);
  }

  return String(value);
}

function styleToCss(style) {
  const safeStyle = normalizeStyle(style);
  const propertyMap = {
    margin: "margin",
    padding: "padding",
    fontFamily: "font-family",
    fontSize: "font-size",
    fontWeight: "font-weight",
    fontStyle: "font-style",
    textDecoration: "text-decoration",
    textAlign: "text-align",
    color: "color",
    backgroundColor: "background-color",
    border: "border",
    borderRadius: "border-radius",
    width: "width",
    minWidth: "min-width",
    maxWidth: "max-width",
    height: "height",
    display: "display",
  };

  return Object.entries(propertyMap)
    .filter(([key]) => !!safeStyle[key])
    .map(([key, cssName]) => `${cssName}:${String(safeStyle[key])}`)
    .join(";");
}

function clamp(value, minValue, maxValue) {
  return Math.min(Math.max(value, minValue), maxValue);
}

function beginResize(splitterName, event) {
  if (!shellElement || !(event instanceof PointerEvent)) {
    return;
  }

  state.resizeSession = {
    splitter: splitterName,
    pointerId: event.pointerId,
  };

  event.preventDefault();
  const splitterElement = event.target instanceof Element ? event.target.closest("[data-splitter]") : null;
  if (splitterElement) {
    splitterElement.dataset.active = "true";
    splitterElement.setPointerCapture?.(event.pointerId);
  }
}

function updateResize(event) {
  if (!state.resizeSession || !shellElement) {
    return;
  }

  const rect = shellElement.getBoundingClientRect();
  const availableForSidePanels = rect.width - (MIN_PANEL_SIZES.splitter * 2) - MIN_PANEL_SIZES.center;
  if (availableForSidePanels <= 0) {
    return;
  }

  if (state.resizeSession.splitter === "left") {
    const maxLeft = Math.max(MIN_PANEL_SIZES.left, availableForSidePanels - state.panelSizes.right);
    const nextLeft = clamp(event.clientX - rect.left, MIN_PANEL_SIZES.left, maxLeft);
    state.panelSizes.left = nextLeft;
  } else {
    const maxRight = Math.max(MIN_PANEL_SIZES.right, availableForSidePanels - state.panelSizes.left);
    const nextRight = clamp(rect.right - event.clientX, MIN_PANEL_SIZES.right, maxRight);
    state.panelSizes.right = nextRight;
  }

  applyPanelSizes();
}

function endResize() {
  if (!state.resizeSession) {
    return;
  }

  const activeSplitter = shellElement.querySelector(`[data-splitter="${state.resizeSession.splitter}"]`);
  if (activeSplitter) {
    activeSplitter.dataset.active = "false";
  }

  state.resizeSession = null;
}

function focusShell() {
  shellElement?.focus();
}

function parseHostMessage(message) {
  if (!message) {
    return null;
  }

  if (typeof message === "string") {
    try {
      return JSON.parse(message);
    } catch {
      return null;
    }
  }

  return typeof message === "object" ? message : null;
}

function emitModeChanged() {
  postToHost({
    type: "reportDesigner.modeChanged",
    mode: state.mode,
  });
}

function ensurePreviewMode() {
  if (state.mode !== "preview") {
    state.mode = "preview";
    emitModeChanged();
  }
}

function nextFrame() {
  return new Promise((resolve) => window.requestAnimationFrame(() => resolve()));
}

async function printFromPreviewAsync() {
  ensurePreviewMode();
  const previewModel = renderApp();
  emitPreviewReady(previewModel);
  await nextFrame();
  await nextFrame();
  window.print();
}
async function handleHostMessage(message) {
  try {
    const normalizedMessage = parseHostMessage(message);
    if (!normalizedMessage || typeof normalizedMessage !== "object") {
      return;
    }

    switch (normalizedMessage.type) {
      case "reportDesigner.loadDefinition":
        state.definition = normalizeDefinition(normalizedMessage.definition);
        restoreEditorMetadata(state.definition);
        renderApp();
        break;

      case "reportDesigner.getDefinition":
        postToHost({
          type: "reportDesigner.definitionSnapshot",
          requestId: normalizedMessage.requestId || "",
          definition: cloneDefinition(),
        });
        break;

      case "reportDesigner.setMode":
        state.mode = normalizeMode(normalizedMessage.mode);
        renderApp();
        emitModeChanged();
        if (state.mode === "preview") {
          emitPreviewReady(state.lastPreviewModel);
        }
        break;

      case "reportDesigner.setLocale":
        state.locale = normalizeLocale(normalizedMessage.locale);
        renderApp();
        break;

      case "reportDesigner.setTheme":
        state.theme = normalizeTheme(normalizedMessage.theme);
        renderApp();
        break;

      case "reportDesigner.setDataSchema":
        state.schema = normalizeSchema(normalizedMessage.schema);
        renderApp();
        break;

      case "reportDesigner.setSampleData":
        state.sampleData = parseJson(normalizedMessage.json, {});
        renderApp();
        break;

      case "reportDesigner.setReportData":
        state.reportData = parseJson(normalizedMessage.json, {});
        renderApp();
        break;

      case "reportDesigner.refreshPreview": {
        const previewModel = buildPreviewModel();
        if (state.mode === "preview") {
          renderApp();
        }
        emitPreviewReady(previewModel);
        break;
      }

      case "reportDesigner.print":
        await printFromPreviewAsync();
        break;

      case "reportDesigner.focus":
        focusShell();
        break;
    }
  } catch (error) {
    emitError(t("Error.Bridge"), stringifyError(error));
  }
}

function escapeHtml(value) {
  return String(value || "")
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;")
    .replaceAll("\"", "&quot;")
    .replaceAll("'", "&#39;");
}

function escapeAttribute(value) {
  return escapeHtml(value);
}

function shorten(value, maxLength) {
  const text = String(value || "");
  if (text.length <= maxLength) {
    return text;
  }

  return `${text.slice(0, Math.max(0, maxLength - 1))}\u2026`;
}

function collectActionElements(startElement) {
  const actions = [];
  let current = startElement instanceof Element ? startElement : null;

  while (current) {
    if (current instanceof HTMLElement && current.dataset && current.dataset.action) {
      actions.push(current);
    }

    current = current.parentElement;
  }

  return actions;
}

window.addEventListener("phiale-webhost-bridge-ready", () => {
  window.PhialeWebHost.onHostMessage = handleHostMessage;
  notifyReady();
});

document.addEventListener("click", (event) => {
  if (!(event.target instanceof Element)) {
    return;
  }

  if (event.target.closest("[data-stop-close=\"true\"]") &&
      !event.target.closest("[data-action]")) {
    return;
  }

  const actionElements = collectActionElements(event.target);
  const actionDescriptor = resolveActionTarget(
    actionElements.map((element) => ({
      action: element.dataset.action || "",
      blockId: element.dataset.blockId || "",
    })),
    !!event.ctrlKey || !!event.metaKey);
  const actionElement = actionDescriptor
    ? actionElements.find((element) =>
      (element.dataset.action || "") === actionDescriptor.action &&
      (element.dataset.blockId || "") === (actionDescriptor.blockId || ""))
    : null;
  if (!actionElement) {
    return;
  }

  event.preventDefault();
  handleShellClick(actionElement);
});

document.addEventListener("keydown", (event) => {
  if (event.key !== "Escape" || !state.helpModalKey) {
    return;
  }

  state.helpModalKey = "";
  renderApp();
});

document.addEventListener("change", (event) => {
  if (!(event.target instanceof HTMLElement)) {
    return;
  }

  if (event.target.matches("[data-color-picker]")) {
    if (shouldHandleFieldOnInput(event.target)) {
      return;
    }
    handleColorInput(event.target);
    return;
  }

  if (!isShellFieldTarget(event.target) || shouldHandleFieldOnInput(event.target)) {
    return;
  }

  handleShellInput(event.target);
});

document.addEventListener("input", (event) => {
  if (!(event.target instanceof HTMLElement)) {
    return;
  }

  if (event.target.matches("[data-color-picker]")) {
    handleColorInput(event.target);
    return;
  }

  if (!isShellFieldTarget(event.target) || !shouldHandleFieldOnInput(event.target)) {
    return;
  }

  handleShellInput(event.target);
});

document.addEventListener("pointerdown", (event) => {
  if (!(event.target instanceof Element)) {
    return;
  }

  const splitter = event.target.closest("[data-splitter]");
  if (!splitter) {
    return;
  }

  beginResize(splitter.dataset.splitter || "", event);
});

window.addEventListener("pointermove", (event) => {
  if (!(event instanceof PointerEvent)) {
    return;
  }

  updateResize(event);
});

window.addEventListener("pointerup", endResize);
window.addEventListener("pointercancel", endResize);
window.addEventListener("blur", endResize);

window.addEventListener("error", (event) => {
  emitError(t("Error.Unexpected"), event.message || "");
});

window.addEventListener("unhandledrejection", (event) => {
  emitError(t("Error.Unexpected"), stringifyError(event.reason));
});

applyDocumentContext();
renderApp();

if (window.PhialeWebHost && typeof window.PhialeWebHost.postMessage === "function") {
  window.PhialeWebHost.onHostMessage = handleHostMessage;
  notifyReady();
}
