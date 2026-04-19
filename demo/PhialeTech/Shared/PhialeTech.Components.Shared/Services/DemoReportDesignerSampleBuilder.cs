using PhialeTech.ReportDesigner.Abstractions;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace PhialeTech.Components.Shared.Services
{
    public static class DemoReportDesignerSampleBuilder
    {
        private const string LogoDataUri =
            "data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='160' height='48' viewBox='0 0 160 48'%3E%3Crect width='160' height='48' rx='14' fill='%230f766e'/%3E%3Ctext x='18' y='30' fill='white' font-family='Segoe UI, Arial, sans-serif' font-size='18' font-weight='700'%3EPhialeTech%3C/text%3E%3C/svg%3E";

        public static ReportDefinition CreateDefinition(string languageCode = "en")
        {
            bool polish = IsPolish(languageCode);
            var definition = new ReportDefinition
            {
                Version = 1,
                Page = new ReportPageSettings
                {
                    Size = "A4",
                    Orientation = "Portrait",
                    Margin = "18mm"
                },
                Blocks = new List<ReportBlockDefinition>
                {
                    new ReportBlockDefinition
                    {
                        Id = "header-title",
                        Type = "Text",
                        Name = polish ? "Nagłówek raportu" : "Header title",
                        Text = polish ? "Przegląd faktury" : "Invoice overview",
                        Style = new ReportBlockStyle
                        {
                            FontSize = "28px",
                            FontWeight = "700",
                            Margin = "0 0 14px 0"
                        }
                    },
                    new ReportBlockDefinition
                    {
                        Id = "logo",
                        Type = "Image",
                        Name = polish ? "Logo marki" : "Brand logo",
                        ImageSource = LogoDataUri,
                        Style = new ReportBlockStyle
                        {
                            Width = "140px",
                            Margin = "0 0 18px 0"
                        }
                    },
                    new ReportBlockDefinition
                    {
                        Id = "invoice-overview-columns",
                        Type = "Columns",
                        Name = polish ? "Kolumny nagłówka faktury" : "Invoice header columns",
                        ColumnCount = 2,
                        ColumnGap = "18px",
                        KeepTogether = true,
                        Children = new List<ReportBlockDefinition>
                        {
                            new ReportBlockDefinition
                            {
                                Id = "invoice-overview-left",
                                Type = "Container",
                                Name = polish ? "Lewa kolumna nagłówka" : "Header left column",
                                Children = new List<ReportBlockDefinition>
                                {
                                    new ReportBlockDefinition
                                    {
                                        Id = "invoice-details-title",
                                        Type = "Text",
                                        Name = polish ? "Nagłówek danych faktury" : "Invoice details title",
                                        Text = polish ? "Dane faktury" : "Invoice details",
                                        Style = new ReportBlockStyle
                                        {
                                            FontSize = "16px",
                                            FontWeight = "700",
                                            Margin = "0 0 10px 0"
                                        }
                                    },
                                    new ReportBlockDefinition
                                    {
                                        Id = "invoice-field-list",
                                        Type = "FieldList",
                                        Name = polish ? "Lista pól faktury" : "Invoice field list",
                                        KeepTogether = true,
                                        Fields = new List<ReportFieldListItemDefinition>
                                        {
                                            new ReportFieldListItemDefinition
                                            {
                                                Label = polish ? "Numer faktury" : "Invoice number",
                                                Binding = "InvoiceNumber"
                                            },
                                            new ReportFieldListItemDefinition
                                            {
                                                Label = polish ? "Data faktury" : "Invoice date",
                                                Binding = "InvoiceDate",
                                                Format = new ReportValueFormat
                                                {
                                                    Kind = "date",
                                                    Pattern = "yyyy-MM-dd"
                                                }
                                            },
                                            new ReportFieldListItemDefinition
                                            {
                                                Label = polish ? "Netto" : "Net total",
                                                Binding = "TotalNet",
                                                Format = new ReportValueFormat
                                                {
                                                    Kind = "currency",
                                                    Currency = "PLN",
                                                    Decimals = 2
                                                }
                                            },
                                            new ReportFieldListItemDefinition
                                            {
                                                Label = polish ? "VAT" : "VAT",
                                                Binding = "TotalVat",
                                                Format = new ReportValueFormat
                                                {
                                                    Kind = "currency",
                                                    Currency = "PLN",
                                                    Decimals = 2
                                                }
                                            },
                                            new ReportFieldListItemDefinition
                                            {
                                                Label = polish ? "Brutto" : "Gross total",
                                                Binding = "TotalGross",
                                                Format = new ReportValueFormat
                                                {
                                                    Kind = "currency",
                                                    Currency = "PLN",
                                                    Decimals = 2
                                                }
                                            }
                                        }
                                    }
                                }
                            },
                            new ReportBlockDefinition
                            {
                                Id = "invoice-overview-right",
                                Type = "Container",
                                Name = polish ? "Prawa kolumna nagłówka" : "Header right column",
                                Children = new List<ReportBlockDefinition>
                                {
                                    new ReportBlockDefinition
                                    {
                                        Id = "print-date-title",
                                        Type = "Text",
                                        Name = polish ? "Nagłówek daty wydruku" : "Print date title",
                                        Text = polish ? "Data wydruku" : "Print date",
                                        Style = new ReportBlockStyle
                                        {
                                            FontSize = "16px",
                                            FontWeight = "700",
                                            Margin = "0 0 8px 0"
                                        }
                                    },
                                    new ReportBlockDefinition
                                    {
                                        Id = "print-date-value",
                                        Type = "SpecialField",
                                        Name = polish ? "Bieżąca data" : "Current date",
                                        SpecialFieldKind = ReportSpecialFieldKinds.CurrentDate,
                                        Format = new ReportValueFormat
                                        {
                                            Kind = "date",
                                            Pattern = "yyyy-MM-dd"
                                        },
                                        Style = new ReportBlockStyle
                                        {
                                            Margin = "0 0 18px 0",
                                            FontWeight = "600"
                                        }
                                    },
                                    new ReportBlockDefinition
                                    {
                                        Id = "buyer-title",
                                        Type = "Text",
                                        Name = polish ? "Nagłówek nabywcy" : "Buyer title",
                                        Text = polish ? "Nabywca" : "Buyer",
                                        Style = new ReportBlockStyle
                                        {
                                            FontSize = "16px",
                                            FontWeight = "700",
                                            Margin = "0 0 10px 0"
                                        }
                                    },
                                    new ReportBlockDefinition
                                    {
                                        Id = "buyer-field-list",
                                        Type = "FieldList",
                                        Name = polish ? "Lista pól nabywcy" : "Buyer field list",
                                        KeepTogether = true,
                                        Fields = new List<ReportFieldListItemDefinition>
                                        {
                                            new ReportFieldListItemDefinition
                                            {
                                                Label = polish ? "Nazwa" : "Name",
                                                Binding = "Buyer.Name"
                                            },
                                            new ReportFieldListItemDefinition
                                            {
                                                Label = polish ? "NIP" : "Tax ID",
                                                Binding = "Buyer.TaxId"
                                            },
                                            new ReportFieldListItemDefinition
                                            {
                                                Label = polish ? "Miasto" : "City",
                                                Binding = "Buyer.City"
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    },
                    new ReportBlockDefinition
                    {
                        Id = "business-columns",
                        Type = "Columns",
                        Name = polish ? "Kolumny biznesowe" : "Business columns",
                        ColumnCount = 2,
                        ColumnGap = "18px",
                        KeepTogether = true,
                        Style = new ReportBlockStyle
                        {
                            Margin = "0 0 18px 0"
                        },
                        Children = new List<ReportBlockDefinition>
                        {
                            new ReportBlockDefinition
                            {
                                Id = "seller-column",
                                Type = "Container",
                                Name = polish ? "Kolumna sprzedawcy" : "Seller column",
                                Children = new List<ReportBlockDefinition>
                                {
                                    new ReportBlockDefinition
                                    {
                                        Id = "seller-title",
                                        Type = "Text",
                                        Name = polish ? "Nagłówek sprzedawcy" : "Seller title",
                                        Text = polish ? "Sprzedawca" : "Seller",
                                        Style = new ReportBlockStyle
                                        {
                                            FontSize = "16px",
                                            FontWeight = "700",
                                            Margin = "0 0 10px 0"
                                        }
                                    },
                                    new ReportBlockDefinition
                                    {
                                        Id = "seller-field-list",
                                        Type = "FieldList",
                                        Name = polish ? "Lista pól sprzedawcy" : "Seller field list",
                                        KeepTogether = true,
                                        Fields = new List<ReportFieldListItemDefinition>
                                        {
                                            new ReportFieldListItemDefinition
                                            {
                                                Label = polish ? "Nazwa" : "Name",
                                                Binding = "Seller.Name"
                                            },
                                            new ReportFieldListItemDefinition
                                            {
                                                Label = polish ? "NIP" : "Tax ID",
                                                Binding = "Seller.TaxId"
                                            },
                                            new ReportFieldListItemDefinition
                                            {
                                                Label = polish ? "Miasto" : "City",
                                                Binding = "Seller.City"
                                            }
                                        }
                                    }
                                }
                            },
                            new ReportBlockDefinition
                            {
                                Id = "payment-column",
                                Type = "Container",
                                Name = polish ? "Kolumna płatności" : "Payment column",
                                Children = new List<ReportBlockDefinition>
                                {
                                    new ReportBlockDefinition
                                    {
                                        Id = "payment-title",
                                        Type = "Text",
                                        Name = polish ? "Nagłówek płatności" : "Payment title",
                                        Text = polish ? "Płatność" : "Payment",
                                        Style = new ReportBlockStyle
                                        {
                                            FontSize = "16px",
                                            FontWeight = "700",
                                            Margin = "0 0 10px 0"
                                        }
                                    },
                                    new ReportBlockDefinition
                                    {
                                        Id = "payment-field-list",
                                        Type = "FieldList",
                                        Name = polish ? "Lista pól płatności" : "Payment field list",
                                        KeepTogether = true,
                                        Fields = new List<ReportFieldListItemDefinition>
                                        {
                                            new ReportFieldListItemDefinition
                                            {
                                                Label = polish ? "Metoda" : "Method",
                                                Binding = "Payment.Method"
                                            },
                                            new ReportFieldListItemDefinition
                                            {
                                                Label = polish ? "Termin" : "Due date",
                                                Binding = "Payment.DueDate",
                                                Format = new ReportValueFormat
                                                {
                                                    Kind = "date",
                                                    Pattern = "yyyy-MM-dd"
                                                }
                                            },
                                            new ReportFieldListItemDefinition
                                            {
                                                Label = polish ? "Konto" : "Bank account",
                                                Binding = "Payment.BankAccount"
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    },
                    new ReportBlockDefinition
                    {
                        Id = "items-table",
                        Type = "Table",
                        Name = polish ? "Tabela pozycji" : "Items table",
                        ItemsSource = "Items",
                        Columns = new List<ReportTableColumnDefinition>
                        {
                            new ReportTableColumnDefinition
                            {
                                Header = polish ? "Pozycja" : "Item",
                                Binding = "Name",
                                Width = "45%"
                            },
                            new ReportTableColumnDefinition
                            {
                                Header = polish ? "Ilość" : "Quantity",
                                Binding = "Quantity",
                                Width = "12%",
                                TextAlign = "right",
                                Format = new ReportValueFormat
                                {
                                    Kind = "number",
                                    Decimals = 0
                                }
                            },
                            new ReportTableColumnDefinition
                            {
                                Header = polish ? "Cena jedn." : "Unit price",
                                Binding = "UnitPrice",
                                Width = "18%",
                                TextAlign = "right",
                                Format = new ReportValueFormat
                                {
                                    Kind = "currency",
                                    Currency = "PLN",
                                    Decimals = 2
                                }
                            },
                            new ReportTableColumnDefinition
                            {
                                Header = polish ? "Wartość" : "Line total",
                                Binding = "LineTotal",
                                Width = "25%",
                                TextAlign = "right",
                                Format = new ReportValueFormat
                                {
                                    Kind = "currency",
                                    Currency = "PLN",
                                    Decimals = 2
                                }
                            }
                        }
                    },
                    new ReportBlockDefinition
                    {
                        Id = "totals-card",
                        Type = "Container",
                        Name = polish ? "Podsumowanie końcowe" : "Totals summary",
                        KeepTogether = true,
                        Style = new ReportBlockStyle
                        {
                            Padding = "14px 16px",
                            Margin = "16px 0 0 0",
                            BackgroundColor = "#F8FAFC",
                            Border = "1px solid rgba(203, 213, 225, 0.82)",
                            BorderRadius = "16px"
                        },
                        Children = new List<ReportBlockDefinition>
                        {
                            new ReportBlockDefinition
                            {
                                Id = "totals-field-list",
                                Type = "FieldList",
                                Name = polish ? "Lista sum" : "Totals field list",
                                Fields = new List<ReportFieldListItemDefinition>
                                {
                                    new ReportFieldListItemDefinition
                                    {
                                        Label = polish ? "Netto" : "Net total",
                                        Binding = "TotalNet",
                                        Format = new ReportValueFormat
                                        {
                                            Kind = "currency",
                                            Currency = "PLN",
                                            Decimals = 2
                                        }
                                    },
                                    new ReportFieldListItemDefinition
                                    {
                                        Label = polish ? "VAT" : "VAT",
                                        Binding = "TotalVat",
                                        Format = new ReportValueFormat
                                        {
                                            Kind = "currency",
                                            Currency = "PLN",
                                            Decimals = 2
                                        }
                                    },
                                    new ReportFieldListItemDefinition
                                    {
                                        Label = polish ? "Brutto" : "Gross total",
                                        Binding = "TotalGross",
                                        Format = new ReportValueFormat
                                        {
                                            Kind = "currency",
                                            Currency = "PLN",
                                            Decimals = 2
                                        }
                                    }
                                },
                                Style = new ReportBlockStyle
                                {
                                    FontWeight = "600"
                                }
                            }
                        }
                    },
                    new ReportBlockDefinition
                    {
                        Id = "codes-heading",
                        Type = "Text",
                        Name = polish ? "Nagłówek kodów" : "Codes heading",
                        PageBreakBefore = true,
                        Text = polish ? "Kody i znaczniki wydruku" : "Codes and print markers",
                        Style = new ReportBlockStyle
                        {
                            FontSize = "20px",
                            FontWeight = "700",
                            Margin = "0 0 12px 0"
                        }
                    },
                    new ReportBlockDefinition
                    {
                        Id = "barcode-code128",
                        Type = "Barcode",
                        Name = polish ? "Kod Code128" : "Code128 barcode",
                        Binding = "TrackingCode128",
                        BarcodeType = ReportBarcodeTypes.Code128,
                        ShowText = true,
                        Style = new ReportBlockStyle
                        {
                            Width = "320px",
                            Height = "82px",
                            Margin = "0 0 18px 0"
                        }
                    },
                    new ReportBlockDefinition
                    {
                        Id = "barcode-ean13",
                        Type = "Barcode",
                        Name = polish ? "Kod EAN13" : "EAN13 barcode",
                        Binding = "RetailBarcodeEan13",
                        BarcodeType = ReportBarcodeTypes.Ean13,
                        ShowText = true,
                        Style = new ReportBlockStyle
                        {
                            Width = "240px",
                            Height = "82px",
                            Margin = "0 0 18px 0"
                        }
                    },
                    new ReportBlockDefinition
                    {
                        Id = "barcode-ean8",
                        Type = "Barcode",
                        Name = polish ? "Kod EAN8" : "EAN8 barcode",
                        Binding = "RetailBarcodeEan8",
                        BarcodeType = ReportBarcodeTypes.Ean8,
                        ShowText = true,
                        Style = new ReportBlockStyle
                        {
                            Width = "180px",
                            Height = "82px",
                            Margin = "0 0 18px 0"
                        }
                    },
                    new ReportBlockDefinition
                    {
                        Id = "barcode-code39",
                        Type = "Barcode",
                        Name = polish ? "Kod Code39" : "Code39 barcode",
                        Binding = "WarehouseCode39",
                        BarcodeType = ReportBarcodeTypes.Code39,
                        ShowText = true,
                        Style = new ReportBlockStyle
                        {
                            Width = "260px",
                            Height = "82px",
                            Margin = "0 0 18px 0"
                        }
                    },
                    new ReportBlockDefinition
                    {
                        Id = "qr-code",
                        Type = "QrCode",
                        Name = polish ? "Kod QR" : "QR code",
                        Binding = "TrackingQr",
                        Size = "140px",
                        ErrorCorrectionLevel = ReportQrCodeErrorCorrectionLevels.Medium,
                        Style = new ReportBlockStyle
                        {
                            Margin = "6px 0 18px 0"
                        }
                    },
                    new ReportBlockDefinition
                    {
                        Id = "notes-heading",
                        Type = "Text",
                        Name = polish ? "Nagłówek uwag" : "Notes heading",
                        Text = polish ? "Uwagi dostawy" : "Delivery notes",
                        Style = new ReportBlockStyle
                        {
                            FontSize = "20px",
                            FontWeight = "700",
                            Margin = "0 0 12px 0"
                        }
                    },
                    new ReportBlockDefinition
                    {
                        Id = "notes-repeater",
                        Type = "Repeater",
                        Name = polish ? "Repeater uwag" : "Notes repeater",
                        ItemsSource = "Notes",
                        Style = new ReportBlockStyle
                        {
                            Display = "grid",
                            Margin = "0"
                        },
                        Children = new List<ReportBlockDefinition>
                        {
                            new ReportBlockDefinition
                            {
                                Id = "note-text",
                                Type = "Text",
                                Name = polish ? "Treść uwagi" : "Note text",
                                Binding = "Text",
                                Style = new ReportBlockStyle
                                {
                                    Padding = "10px 12px",
                                    Margin = "0 0 10px 0",
                                    Border = "1px solid rgba(203, 213, 225, 0.82)",
                                    BorderRadius = "12px",
                                    BackgroundColor = "#FFFFFF"
                                }
                            }
                        }
                    },
                    new ReportBlockDefinition
                    {
                        Id = "page-number",
                        Type = "SpecialField",
                        Name = polish ? "Numeracja stron" : "Page numbering",
                        SpecialFieldKind = ReportSpecialFieldKinds.PageNumberOfTotalPages,
                        Style = new ReportBlockStyle
                        {
                            Margin = "24px 0 0 0",
                            TextAlign = "right",
                            FontWeight = "600"
                        }
                    }
                }
            };

            if (definition.Blocks.Count >= 2)
            {
                definition.Sections.ReportHeader.Blocks.Add(definition.Blocks[0]);
                definition.Sections.ReportHeader.Blocks.Add(definition.Blocks[1]);
                definition.Blocks.RemoveAt(1);
                definition.Blocks.RemoveAt(0);
            }

            if (definition.Blocks.Count > 0)
            {
                int pageNumberIndex = definition.Blocks.Count - 1;
                definition.Sections.PageFooter.Blocks.Add(definition.Blocks[pageNumberIndex]);
                definition.Blocks.RemoveAt(pageNumberIndex);
            }

            definition.Sections.Body.Blocks = definition.Blocks;
            definition.Sections.PageHeader.SkipFirstPage = true;

            return definition;
        }

        public static ReportDataSchema CreateSchema(string languageCode = "en")
        {
            bool polish = IsPolish(languageCode);
            return new ReportDataSchema
            {
                Fields = new List<ReportDataFieldDefinition>
                {
                    new ReportDataFieldDefinition
                    {
                        Name = "InvoiceNumber",
                        DisplayName = polish ? "Numer faktury" : "Invoice number",
                        Type = "string"
                    },
                    new ReportDataFieldDefinition
                    {
                        Name = "InvoiceDate",
                        DisplayName = polish ? "Data faktury" : "Invoice date",
                        Type = "date"
                    },
                    new ReportDataFieldDefinition
                    {
                        Name = "TotalNet",
                        DisplayName = polish ? "Kwota netto" : "Net total",
                        Type = "number"
                    },
                    new ReportDataFieldDefinition
                    {
                        Name = "TotalVat",
                        DisplayName = polish ? "Kwota VAT" : "VAT total",
                        Type = "number"
                    },
                    new ReportDataFieldDefinition
                    {
                        Name = "TotalGross",
                        DisplayName = polish ? "Kwota brutto" : "Total gross",
                        Type = "number"
                    },
                    new ReportDataFieldDefinition
                    {
                        Name = "TrackingCode128",
                        DisplayName = polish ? "Kod śledzenia Code128" : "Code128 tracking code",
                        Type = "string"
                    },
                    new ReportDataFieldDefinition
                    {
                        Name = "RetailBarcodeEan13",
                        DisplayName = polish ? "Kod EAN13" : "EAN13 barcode",
                        Type = "string"
                    },
                    new ReportDataFieldDefinition
                    {
                        Name = "RetailBarcodeEan8",
                        DisplayName = polish ? "Kod EAN8" : "EAN8 barcode",
                        Type = "string"
                    },
                    new ReportDataFieldDefinition
                    {
                        Name = "WarehouseCode39",
                        DisplayName = polish ? "Kod magazynowy Code39" : "Code39 warehouse code",
                        Type = "string"
                    },
                    new ReportDataFieldDefinition
                    {
                        Name = "TrackingQr",
                        DisplayName = polish ? "Kod QR" : "QR code",
                        Type = "string"
                    },
                    new ReportDataFieldDefinition
                    {
                        Name = "Seller",
                        DisplayName = polish ? "Sprzedawca" : "Seller",
                        Type = "object",
                        Children = new List<ReportDataFieldDefinition>
                        {
                            new ReportDataFieldDefinition
                            {
                                Name = "Name",
                                DisplayName = polish ? "Nazwa" : "Name",
                                Type = "string"
                            },
                            new ReportDataFieldDefinition
                            {
                                Name = "City",
                                DisplayName = polish ? "Miasto" : "City",
                                Type = "string"
                            },
                            new ReportDataFieldDefinition
                            {
                                Name = "TaxId",
                                DisplayName = polish ? "NIP" : "Tax ID",
                                Type = "string"
                            }
                        }
                    },
                    new ReportDataFieldDefinition
                    {
                        Name = "Buyer",
                        DisplayName = polish ? "Nabywca" : "Buyer",
                        Type = "object",
                        Children = new List<ReportDataFieldDefinition>
                        {
                            new ReportDataFieldDefinition
                            {
                                Name = "Name",
                                DisplayName = polish ? "Nazwa" : "Name",
                                Type = "string"
                            },
                            new ReportDataFieldDefinition
                            {
                                Name = "City",
                                DisplayName = polish ? "Miasto" : "City",
                                Type = "string"
                            },
                            new ReportDataFieldDefinition
                            {
                                Name = "TaxId",
                                DisplayName = polish ? "NIP" : "Tax ID",
                                Type = "string"
                            }
                        }
                    },
                    new ReportDataFieldDefinition
                    {
                        Name = "Payment",
                        DisplayName = polish ? "Płatność" : "Payment",
                        Type = "object",
                        Children = new List<ReportDataFieldDefinition>
                        {
                            new ReportDataFieldDefinition
                            {
                                Name = "Method",
                                DisplayName = polish ? "Metoda" : "Method",
                                Type = "string"
                            },
                            new ReportDataFieldDefinition
                            {
                                Name = "DueDate",
                                DisplayName = polish ? "Termin" : "Due date",
                                Type = "date"
                            },
                            new ReportDataFieldDefinition
                            {
                                Name = "BankAccount",
                                DisplayName = polish ? "Konto" : "Bank account",
                                Type = "string"
                            }
                        }
                    },
                    new ReportDataFieldDefinition
                    {
                        Name = "Items",
                        DisplayName = polish ? "Pozycje" : "Items",
                        Type = "object",
                        IsCollection = true,
                        Children = new List<ReportDataFieldDefinition>
                        {
                            new ReportDataFieldDefinition
                            {
                                Name = "Name",
                                DisplayName = polish ? "Nazwa" : "Name",
                                Type = "string"
                            },
                            new ReportDataFieldDefinition
                            {
                                Name = "Quantity",
                                DisplayName = polish ? "Ilość" : "Quantity",
                                Type = "number"
                            },
                            new ReportDataFieldDefinition
                            {
                                Name = "UnitPrice",
                                DisplayName = polish ? "Cena jedn." : "Unit price",
                                Type = "number"
                            },
                            new ReportDataFieldDefinition
                            {
                                Name = "LineTotal",
                                DisplayName = polish ? "Wartość" : "Line total",
                                Type = "number"
                            }
                        }
                    },
                    new ReportDataFieldDefinition
                    {
                        Name = "Notes",
                        DisplayName = polish ? "Uwagi" : "Notes",
                        Type = "object",
                        IsCollection = true,
                        Children = new List<ReportDataFieldDefinition>
                        {
                            new ReportDataFieldDefinition
                            {
                                Name = "Text",
                                DisplayName = polish ? "Treść" : "Text",
                                Type = "string"
                            }
                        }
                    }
                }
            };
        }

        public static string CreateSampleDataJson(string languageCode = "en")
        {
            bool polish = IsPolish(languageCode);
            return JsonSerializer.Serialize(new
            {
                InvoiceNumber = polish ? "PRÓBKA-2026-001" : "SAMPLE-2026-001",
                InvoiceDate = "2026-03-24",
                TotalNet = 10439.43m,
                TotalVat = 2401.07m,
                TotalGross = 12840.50m,
                TrackingCode128 = "PT-2026-PL-000418",
                RetailBarcodeEan13 = "5901234123457",
                RetailBarcodeEan8 = "55123457",
                WarehouseCode39 = "INV-26-0418",
                TrackingQr = polish
                    ? "https://phialetech.local/śledzenie/próbka-2026-001"
                    : "https://phialetech.local/tracking/sample-2026-001",
                Seller = new
                {
                    Name = "PhialeTech Sp. z o.o.",
                    City = polish ? "Warszawa" : "Warsaw",
                    TaxId = "PL-525-00-11-222"
                },
                Buyer = new
                {
                    Name = polish ? "Północna Logistyka Sp. z o.o." : "North Logistics Ltd.",
                    City = polish ? "Łódź" : "Lodz",
                    TaxId = "PL-947-20-88-110"
                },
                Payment = new
                {
                    Method = polish ? "Przelew 14 dni" : "Bank transfer 14 days",
                    DueDate = "2026-04-07",
                    BankAccount = "12 1140 2004 0000 3202 7654 1120"
                },
                Items = new[]
                {
                    new { Name = polish ? "Drukarka termiczna" : "Thermal printer", Quantity = 2, UnitPrice = 1699.99m, LineTotal = 3399.98m },
                    new { Name = polish ? "Pakiet rolek etykiet" : "Label roll pack", Quantity = 12, UnitPrice = 42.50m, LineTotal = 510.00m },
                    new { Name = polish ? "Tablet terenowy" : "Field tablet", Quantity = 3, UnitPrice = 2976.84m, LineTotal = 8930.52m }
                },
                Notes = new[]
                {
                    new { Text = polish ? "Użyj danych przykładowych w trybie projektowania, aby sprawdzić bindingi bez dotykania danych produkcyjnych." : "Use sample data in design mode to validate bindings without touching real production datasets." },
                    new { Text = polish ? "Podgląd używa tych samych bloków, formatowania i kodów co ścieżka wydruku." : "Preview uses the same blocks, formatting and codes as the print path." }
                }
            });
        }

        public static string CreateReportDataJson(string languageCode = "en")
        {
            bool polish = IsPolish(languageCode);
            return JsonSerializer.Serialize(new
            {
                InvoiceNumber = polish ? "FV-2026-0418" : "INV-2026-0418",
                InvoiceDate = "2026-03-28",
                TotalNet = 7772.68m,
                TotalVat = 1787.72m,
                TotalGross = 9560.40m,
                TrackingCode128 = "SHIP-PL-2026-0418",
                RetailBarcodeEan13 = "9780201379624",
                RetailBarcodeEan8 = "96385074",
                WarehouseCode39 = "WH-0418-PL",
                TrackingQr = polish
                    ? "https://phialetech.local/śledzenie/fv-2026-0418"
                    : "https://phialetech.local/tracking/inv-2026-0418",
                Seller = new
                {
                    Name = "PhialeTech Sp. z o.o.",
                    City = polish ? "Warszawa" : "Warsaw",
                    TaxId = "PL-525-00-11-222"
                },
                Buyer = new
                {
                    Name = polish ? "Zarząd Infrastruktury Miejskiej" : "City Infrastructure Unit",
                    City = polish ? "Kraków" : "Krakow",
                    TaxId = "PL-945-18-01-331"
                },
                Payment = new
                {
                    Method = polish ? "Przelew 7 dni" : "Bank transfer 7 days",
                    DueDate = "2026-04-04",
                    BankAccount = "54 1140 2004 0000 3902 7654 2211"
                },
                Items = new[]
                {
                    new { Name = polish ? "Akumulator do drona" : "Inspection drone battery", Quantity = 6, UnitPrice = 420.00m, LineTotal = 2520.00m },
                    new { Name = polish ? "Antena GNSS" : "GNSS field antenna", Quantity = 2, UnitPrice = 1850.00m, LineTotal = 3700.00m },
                    new { Name = polish ? "Walizka transportowa" : "Protective carrying case", Quantity = 4, UnitPrice = 835.10m, LineTotal = 3340.40m }
                },
                Notes = new[]
                {
                    new { Text = polish ? "Dane raportowe potwierdzają, że ten sam layout działa z produkcyjnym JSON-em i nie wymaga osobnej ścieżki wydruku." : "Report data confirms that the same layout works with production JSON and does not require a separate print path." },
                    new { Text = polish ? "Wydruk korzysta z aktualnego preview, łącznie z kodami kreskowymi, kodem QR i podziałami stron." : "Print uses the current preview surface, including barcodes, QR code and page breaks." }
                }
            });
        }

        private static bool IsPolish(string languageCode)
        {
            if (string.IsNullOrWhiteSpace(languageCode))
            {
                return false;
            }

            return languageCode.Trim().StartsWith("pl", StringComparison.OrdinalIgnoreCase);
        }
    }
}
