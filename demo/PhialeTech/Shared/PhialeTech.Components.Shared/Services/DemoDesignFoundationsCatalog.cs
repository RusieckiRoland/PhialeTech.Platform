using System;
using System.Collections.Generic;
using PhialeTech.Components.Shared.Model;

namespace PhialeTech.Components.Shared.Services
{
    public sealed class DemoDesignFoundationsCatalog
    {
        public IReadOnlyList<string> BuildHighlights(string languageCode)
        {
            var polish = IsPolish(languageCode);
            return new[]
            {
                polish ? "Nagłówki: Bahnschrift SemiBold" : "Headings: Bahnschrift SemiBold",
                polish ? "Tekst bazowy: Segoe UI" : "Body copy: Segoe UI",
                polish ? "Kod: Consolas" : "Code: Consolas",
                polish ? "Kolory są semantyczne i gotowe na dzień / noc" : "Colors are semantic and ready for day / night",
                polish ? "Kontrolki zbiegają się do radius 6, powierzchnie do 12 / 14" : "Controls converge on radius 6, surfaces on 12 / 14",
            };
        }

        public IReadOnlyList<DemoFoundationTypographyTokenViewModel> BuildTypographyTokens(string languageCode)
        {
            var polish = IsPolish(languageCode);
            return new[]
            {
                new DemoFoundationTypographyTokenViewModel(
                    "Text.Hero",
                    polish ? "Hero / nagłówek strony" : "Hero / page headline",
                    polish ? "Główny nagłówek wybranego przykładu i najmocniejszy punkt wejścia." : "Primary selected-example headline and the strongest entry point.",
                    polish ? "Grid | Foundations" : "Grid | Foundations",
                    "Bahnschrift SemiBold",
                    34d,
                    "SemiBold",
                    "primary",
                    "Bahnschrift SemiBold · 34"),
                new DemoFoundationTypographyTokenViewModel(
                    "Text.Display",
                    polish ? "Display / przegląd" : "Display / overview",
                    polish ? "Nagłówki overview i większe tytuły sekcji." : "Overview headlines and larger section entries.",
                    polish ? "Wybierz przykład" : "Select an example",
                    "Bahnschrift SemiBold",
                    30d,
                    "SemiBold",
                    "primary",
                    "Bahnschrift SemiBold · 30"),
                new DemoFoundationTypographyTokenViewModel(
                    "Text.Section",
                    polish ? "Tytuł sekcji" : "Section title",
                    polish ? "Nagłówki sekcji, kart i mocniejszych bloków narracyjnych." : "Section headlines, cards and stronger narrative blocks.",
                    polish ? "Typografia i role" : "Typography and roles",
                    "Bahnschrift SemiBold",
                    18d,
                    "SemiBold",
                    "primary",
                    "Bahnschrift SemiBold · 18"),
                new DemoFoundationTypographyTokenViewModel(
                    "Text.ControlTitle",
                    polish ? "Tytul kontrolki / zwarty header" : "Control title / compact header",
                    polish ? "Naglowki popupow, podpis miesiaca kalendarza i zwarte headery osadzone w kontrolce." : "Popup titles, calendar month captions and compact embedded headers.",
                    polish ? "kwiecien 2026" : "April 2026",
                    "Bahnschrift SemiBold",
                    22d,
                    "SemiBold",
                    "primary",
                    "Bahnschrift SemiBold · 22"),
                new DemoFoundationTypographyTokenViewModel(
                    "Text.ControlLabel",
                    polish ? "Label kontrolki / wybor" : "Control label / choice",
                    polish ? "Dni tygodnia, wartosci dni i zwarte etykiety wyboru wewnatrz kontrolek." : "Weekday labels, day values and compact choice labels inside controls.",
                    polish ? "14" : "14",
                    "Bahnschrift SemiBold",
                    15d,
                    "SemiBold",
                    "primary",
                    "Bahnschrift SemiBold · 15"),
                new DemoFoundationTypographyTokenViewModel(
                    "Text.Label",
                    polish ? "Label / chrome" : "Label / chrome",
                    polish ? "Etykiety pól, tabów i zwarta nawigacja." : "Field labels, tab chrome and compact navigation.",
                    polish ? "Motyw" : "Theme",
                    "Bahnschrift SemiBold",
                    14d,
                    "SemiBold",
                    "header",
                    "Bahnschrift SemiBold · 14"),
                new DemoFoundationTypographyTokenViewModel(
                    "Text.Body",
                    polish ? "Tekst opisowy" : "Body copy",
                    polish ? "Główne opisy i tekst objaśniający scenariusz." : "Primary descriptions and scenario copy.",
                    polish ? "To jest główna warstwa tekstu objaśniającego i nie powinna walczyć z nagłówkiem." : "This is the main explanatory layer and should not compete with the headline.",
                    "Segoe UI",
                    15d,
                    "Normal",
                    "secondary",
                    "Segoe UI · 15"),
                new DemoFoundationTypographyTokenViewModel(
                    "Text.Support",
                    polish ? "Tekst pomocniczy" : "Support text",
                    polish ? "Hinty, statusy toolbaru i pomocnicze metadane." : "Hints, toolbar status and supportive metadata.",
                    polish ? "Używaj tego tonu do komentarzy pomocniczych i drugiego planu." : "Use this tone for supportive commentary and second-plane metadata.",
                    "Segoe UI",
                    13d,
                    "Normal",
                    "secondary",
                    "Segoe UI · 13"),
                new DemoFoundationTypographyTokenViewModel(
                    "Text.Token",
                    polish ? "Mikro token" : "Micro token",
                    polish ? "Chipy, lekkie tagi i małe metadane." : "Chips, light tags and compact metadata.",
                    polish ? "DemoToken" : "DemoToken",
                    "Segoe UI",
                    11d,
                    "Normal",
                    "token",
                    "Segoe UI · 11"),
                new DemoFoundationTypographyTokenViewModel(
                    "Text.Code",
                    polish ? "Kod" : "Code",
                    polish ? "Zakładka Code i fragmenty techniczne." : "Code tab and technical snippets.",
                    "new GridColumnDefinition(\"ObjectName\", \"Object name\")",
                    "Consolas",
                    13d,
                    "Normal",
                    "primary",
                    "Consolas · 13"),
            };
        }

        public IReadOnlyList<DemoFoundationColorTokenViewModel> BuildTextColorTokens(string languageCode)
        {
            var polish = IsPolish(languageCode);
            return new[]
            {
                new DemoFoundationColorTokenViewModel("DemoPrimaryTextBrush", polish ? "Główne dane i mocne nagłówki." : "Primary content and strong headlines.", "#1F2937", "#F4F6FA"),
                new DemoFoundationColorTokenViewModel("DemoSecondaryTextBrush", polish ? "Opisy, hinty i drugi plan." : "Descriptions, hints and secondary copy.", "#5B6572", "#BBC5D4"),
                new DemoFoundationColorTokenViewModel("DemoHeaderTextBrush", polish ? "Label, chrome i nazwy sekcji." : "Labels, chrome and section labels.", "#4B5563", "#C8D0DE"),
            };
        }

        public IReadOnlyList<DemoFoundationColorTokenViewModel> BuildSurfaceTokens(string languageCode)
        {
            var polish = IsPolish(languageCode);
            return new[]
            {
                new DemoFoundationColorTokenViewModel("DemoWindowBackgroundBrush", polish ? "Tło całego shella." : "Shell background.", "#F4F6FA", "#101722"),
                new DemoFoundationColorTokenViewModel("DemoPanelBackgroundBrush", polish ? "Podstawowa powierzchnia kart i paneli." : "Primary card and panel surface.", "#FFFFFF", "#171C25"),
                new DemoFoundationColorTokenViewModel("DemoInputBackgroundBrush", polish ? "Pola wejścia i dropdowny." : "Inputs and dropdowns.", "#FFFFFF", "#1E232D"),
                new DemoFoundationColorTokenViewModel("DemoHintBackgroundBrush", polish ? "Hinty i łagodne callouty." : "Hints and gentle callouts.", "#F6FAF9", "#1A2431"),
                new DemoFoundationColorTokenViewModel("DemoCodeBackgroundBrush", polish ? "Powierzchnia dla kodu." : "Code surface.", "#131A22", "#0F141C"),
                new DemoFoundationColorTokenViewModel("DemoGridBackgroundBrush", polish ? "Tło żywej powierzchni grida." : "Live grid surface background.", "#FFFFFF", "#171C25"),
            };
        }

        public IReadOnlyList<DemoFoundationColorTokenViewModel> BuildFormShellTokens(string languageCode)
        {
            var polish = IsPolish(languageCode);
            return new[]
            {
                new DemoFoundationColorTokenViewModel(
                    "Brush.FormShell.HeaderBackground",
                    polish ? "Tło nagłówka formatki: meta, tytuł i opis." : "Form header background: metadata, title and description.",
                    "#F0F3F7",
                    "#242B36"),
                new DemoFoundationColorTokenViewModel(
                    "Brush.FormShell.TopActionBackground",
                    polish ? "Tło górnego paska akcji i narzędzi dokumentu." : "Top action bar background for document tools and commands.",
                    "#F8FAFC",
                    "#1E232D"),
                new DemoFoundationColorTokenViewModel(
                    "Brush.FormShell.ContentBackground",
                    polish ? "Główna powierzchnia robocza dla layoutu i pól." : "Primary working surface for layout and editable fields.",
                    "#FFFFFF",
                    "#171C25"),
                new DemoFoundationColorTokenViewModel(
                    "Brush.FormShell.BottomActionBackground",
                    polish ? "Tło dolnego paska akcji i commit stripu." : "Bottom action bar background for commit actions.",
                    "#F8FAFC",
                    "#1E232D"),
                new DemoFoundationColorTokenViewModel(
                    "Brush.FormShell.FooterBackground",
                    polish ? "Tło stopki: noty, status i dolny chrome." : "Footer background: notes, status and bottom chrome.",
                    "#F0F3F7",
                    "#242B36"),
                new DemoFoundationColorTokenViewModel(
                    "Brush.FormShell.RegionBorder",
                    polish ? "Obramowanie regionów i zewnętrzny kontur formatki." : "Region border and outer form shell outline.",
                    "#D3D9E1",
                    "#3E4657"),
                new DemoFoundationColorTokenViewModel(
                    "Brush.FormShell.Divider",
                    polish ? "Delikatne separatory między headerem, contentem i footerem." : "Subtle dividers between header, content and footer.",
                    "#E4E7EC",
                    "#343C4B"),
            };
        }

        public IReadOnlyList<DemoFoundationMeasureTokenViewModel> BuildFormShellSpacingTokens(string languageCode)
        {
            var polish = IsPolish(languageCode);
            return new[]
            {
                new DemoFoundationMeasureTokenViewModel(
                    "Thickness.FormShell.HeaderPadding",
                    "24,18,24,14",
                    polish ? "Inset regionu Header: metadata, tytuł i opis." : "Inset for the Header region: metadata, title and description."),
                new DemoFoundationMeasureTokenViewModel(
                    "Thickness.FormShell.TopActionPadding",
                    "24,12,24,16",
                    polish ? "Inset górnego paska akcji i narzędzi dokumentu." : "Inset for the top action panel and document tools."),
                new DemoFoundationMeasureTokenViewModel(
                    "Thickness.FormShell.TopActionMergedPadding",
                    "24,0,24,16",
                    polish ? "Inset górnych akcji, gdy panel wtapia się w nagłówek." : "Inset for top actions when the panel is merged into the header."),
                new DemoFoundationMeasureTokenViewModel(
                    "Thickness.FormShell.LayoutPadding",
                    "24,20,24,20",
                    polish ? "Główny oddech regionu Layout i pól edycyjnych." : "Primary breathing room for the Layout region and editable fields."),
                new DemoFoundationMeasureTokenViewModel(
                    "Thickness.FormShell.BottomActionPadding",
                    "24,12,24,12",
                    polish ? "Inset dolnego paska commit actions." : "Inset for the bottom commit action panel."),
                new DemoFoundationMeasureTokenViewModel(
                    "Thickness.FormShell.BottomActionMergedPadding",
                    "24,12,24,8",
                    polish ? "Inset dolnych akcji, gdy panel wtapia się w footer." : "Inset for bottom actions when the panel is merged into the footer."),
                new DemoFoundationMeasureTokenViewModel(
                    "Thickness.FormShell.FooterPadding",
                    "24,12,24,16",
                    polish ? "Inset stopki: noty, status i dolny chrome." : "Inset for the footer: notes, status and bottom chrome."),
            };
        }

        public IReadOnlyList<DemoFoundationColorTokenViewModel> BuildAccentTokens(string languageCode)
        {
            var polish = IsPolish(languageCode);
            return new[]
            {
                new DemoFoundationColorTokenViewModel("DemoPlatformBadgeBackgroundBrush", polish ? "Wyróżnione badge i aktywne stany lekkiego chromu." : "Highlighted badges and light chrome states.", "#E6F4F1", "#1B3047"),
                new DemoFoundationColorTokenViewModel("DemoCommitButtonBackgroundBrush", polish ? "Akcja potwierdzająca o najwyższym priorytecie." : "Highest-priority confirmation action.", "#0F766E", "#4B8DFF"),
                new DemoFoundationColorTokenViewModel("DemoWarningBackgroundBrush", polish ? "Ostrzeżenia i stany wymagające uwagi." : "Warnings and attention states.", "#FDF6E3", "#3B3221"),
            };
        }

        public IReadOnlyList<DemoFoundationMeasureTokenViewModel> BuildShapeTokens(string languageCode)
        {
            var polish = IsPolish(languageCode);
            return new[]
            {
                new DemoFoundationMeasureTokenViewModel("Radius.14", "14", polish ? "Duże karty overview, code surface i mocniejsze kontenery." : "Large overview cards, code surface and stronger containers."),
                new DemoFoundationMeasureTokenViewModel("Radius.12", "12", polish ? "Standardowe panele i powierzchnie robocze." : "Standard panels and work surfaces."),
                new DemoFoundationMeasureTokenViewModel("Radius.10", "10", polish ? "Badge, statusy i hintowe bannery." : "Badges, status banners and hint callouts."),
                new DemoFoundationMeasureTokenViewModel("Radius.6", "6", polish ? "Przyciski, inputy, popupy i kontrolki interakcyjne." : "Buttons, inputs, popups and interactive controls."),
            };
        }

        public IReadOnlyList<DemoFoundationMeasureTokenViewModel> BuildSpacingTokens(string languageCode)
        {
            var polish = IsPolish(languageCode);
            return new[]
            {
                new DemoFoundationMeasureTokenViewModel("Space.18", "18", polish ? "Główne paddingi surface i code preview." : "Primary surface padding and code preview inset."),
                new DemoFoundationMeasureTokenViewModel("Space.16", "16", polish ? "Między większymi blokami detail view." : "Between larger detail-view blocks."),
                new DemoFoundationMeasureTokenViewModel("Space.14", "14", polish ? "Padding kart, toolbar buttonow i card copy." : "Card padding, toolbar buttons and card copy."),
                new DemoFoundationMeasureTokenViewModel("Space.10", "10", polish ? "Inline gap, chip rhythm i zwarte marginesy." : "Inline gaps, chip rhythm and compact margins."),
                new DemoFoundationMeasureTokenViewModel("Space.8", "8", polish ? "Inset etykiet i drobniejszy oddech kontrolek." : "Label inset and smaller control breathing room."),
            };
        }

        public string GetIntroTitle(string languageCode)
        {
            return IsPolish(languageCode)
                ? "Co jest już ustalone"
                : "What is already defined";
        }

        public string GetIntroDescription(string languageCode)
        {
            return IsPolish(languageCode)
                ? "Ta karta zbiera foundation tokens i stałe wizualne, które już realnie występują w demo. Kolory są semantyczne, a spacing i radius to wyodrębnione wartości, które warto dalej stabilizować jako pełne tokeny."
                : "This card gathers the foundation tokens and visual constants already used in the demo. Colors are semantic today, while spacing and radius are extracted constants worth stabilizing as full tokens next.";
        }

        public string GetTypographyTitle(string languageCode)
        {
            return IsPolish(languageCode)
                ? "Typografia i role"
                : "Typography and roles";
        }

        public string GetTypographyDescription(string languageCode)
        {
            return IsPolish(languageCode)
                ? "Zamiast numerów rozmiarów bez kontekstu pokazujemy role tekstu: gdzie mają być głośne, gdzie opisowe, a gdzie tylko pomocnicze."
                : "Instead of raw font sizes without context, the demo shows text roles: where type should be loud, descriptive or quietly supportive.";
        }

        public string GetColorsTitle(string languageCode)
        {
            return IsPolish(languageCode)
                ? "Kolory semantyczne"
                : "Semantic colors";
        }

        public string GetColorsDescription(string languageCode)
        {
            return IsPolish(languageCode)
                ? "Brush tokens już są semantyczne i przechodzą między trybem dziennym i nocnym bez przepisywania layoutu."
                : "Brush tokens are already semantic and move between day and night without rewriting the layout.";
        }

        public string GetRhythmTitle(string languageCode)
        {
            return IsPolish(languageCode)
                ? "Rytm i kształt"
                : "Rhythm and shape";
        }

        public string GetRhythmDescription(string languageCode)
        {
            return IsPolish(languageCode)
                ? "Spacing i radius nie są jeszcze osobnymi resource tokenami, ale są już na tyle powtarzalne, że można je nazwać i pilnować."
                : "Spacing and radius are not dedicated resource tokens yet, but they already repeat enough to be named and protected.";
        }

        public string GetTextColorsTitle(string languageCode)
        {
            return IsPolish(languageCode) ? "Tekst" : "Text";
        }

        public string GetSurfaceColorsTitle(string languageCode)
        {
            return IsPolish(languageCode) ? "Powierzchnie" : "Surfaces";
        }

        public string GetAccentColorsTitle(string languageCode)
        {
            return IsPolish(languageCode) ? "Akcenty / statusy" : "Accents / states";
        }

        public string GetFormShellColorsTitle(string languageCode)
        {
            return IsPolish(languageCode) ? "Kolory FormShell" : "FormShell colors";
        }

        public string GetFormShellColorsDescription(string languageCode)
        {
            return IsPolish(languageCode)
                ? "Globalne role powierzchni dla pięciu regionów shella: nagłówek, górny pasek akcji, layout, dolny pasek akcji i stopka."
                : "Global surface roles for the five shell regions: header, top action bar, layout, bottom action bar and footer.";
        }

        public string GetFormShellSpacingTitle(string languageCode)
        {
            return IsPolish(languageCode) ? "Spacing FormShell" : "FormShell spacing";
        }

        public string GetFormShellSpacingDescription(string languageCode)
        {
            return IsPolish(languageCode)
                ? "Te tokeny thickness sterują paddingiem pięciu regionów shella i muszą być wspólne dla design systemu oraz prawdziwej formatki."
                : "These thickness tokens drive the padding of the five shell regions and must be shared by the design system and the real form.";
        }

        public string GetShapesTitle(string languageCode)
        {
            return IsPolish(languageCode) ? "Radius" : "Radius";
        }

        public string GetSpacingTitle(string languageCode)
        {
            return IsPolish(languageCode) ? "Spacing" : "Spacing";
        }

        public string GetTokenLabel(string languageCode)
        {
            return IsPolish(languageCode) ? "Token" : "Token";
        }

        public string GetRoleLabel(string languageCode)
        {
            return IsPolish(languageCode) ? "Rola" : "Role";
        }

        public string GetUseLabel(string languageCode)
        {
            return IsPolish(languageCode) ? "Zastosowanie" : "Usage";
        }

        public string GetDayLabel(string languageCode)
        {
            return IsPolish(languageCode) ? "Dzień" : "Day";
        }

        public string GetNightLabel(string languageCode)
        {
            return IsPolish(languageCode) ? "Noc" : "Night";
        }

        public string GetValueLabel(string languageCode)
        {
            return IsPolish(languageCode) ? "Wartość" : "Value";
        }

        private static bool IsPolish(string languageCode)
        {
            return string.Equals(languageCode?.Trim(), "pl", StringComparison.OrdinalIgnoreCase);
        }
    }
}
