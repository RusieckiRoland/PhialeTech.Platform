using System.Collections.Generic;
using System.Text;
using PhialeTech.Components.Shared.Model;

namespace PhialeTech.Components.Shared.Services
{
    internal static class DemoLicenseCatalog
    {
        public static IReadOnlyList<DemoThirdPartyLicenseEntryViewModel> BuildThirdPartyEntries()
        {
            return new[]
            {
                new DemoThirdPartyLicenseEntryViewModel(
                    "PDF.js",
                    "PdfViewer document rendering and PDF print pipeline",
                    "Apache License 2.0",
                    "Mozilla Foundation and PDF.js contributors",
                    "Keep the Apache 2.0 license text and bundled upstream license files with distributed assets. No in-product UI attribution is required.",
                    new[]
                    {
                        "src/PhialeTech/Shared/PhialeTech.WebAssets/Assets/PdfViewer/pdfjs.LICENSE",
                        "src/PhialeTech/Shared/PhialeTech.WebAssets/Assets/PdfViewer/cmaps/LICENSE",
                        "src/PhialeTech/Shared/PhialeTech.WebAssets/Assets/PdfViewer/standard_fonts/LICENSE_LIBERATION",
                        "src/PhialeTech/Shared/PhialeTech.WebAssets/Assets/PdfViewer/standard_fonts/LICENSE_FOXIT",
                        "src/PhialeTech/Shared/PhialeTech.WebAssets/Assets/PdfViewer/wasm/LICENSE_*"
                    }),
                new DemoThirdPartyLicenseEntryViewModel(
                    "JsBarcode",
                    "ReportDesigner print-template preview and barcode blocks",
                    "MIT",
                    "Copyright (c) 2016 Johan Lindell",
                    "Keep the copyright notice and permission notice in copies or substantial portions of the software that ship with the ReportDesigner print assets. No in-product UI attribution is required.",
                    new[]
                    {
                        "src/PhialeTech/Shared/PhialeTech.WebAssets/Assets/ReportDesigner/ThirdPartyNotices.md",
                        "src/PhialeTech/Shared/PhialeTech.WebAssets/Assets/ReportDesigner/vendor/JsBarcode.MIT-LICENSE.txt",
                        "src/PhialeTech/Shared/PhialeTech.WebAssets/Assets/ReportDesigner/vendor/JsBarcode.all.min.js"
                    }),
                new DemoThirdPartyLicenseEntryViewModel(
                    "qrcode-generator",
                    "ReportDesigner print-template preview and QR code blocks",
                    "MIT",
                    "Copyright (c) 2009 Kazuhiko Arase",
                    "Keep the copyright notice and permission notice in copies or substantial portions of the software that ship with the ReportDesigner print assets. No in-product UI attribution is required.",
                    new[]
                    {
                        "src/PhialeTech/Shared/PhialeTech.WebAssets/Assets/ReportDesigner/ThirdPartyNotices.md",
                        "src/PhialeTech/Shared/PhialeTech.WebAssets/Assets/ReportDesigner/vendor/qrcode-generator.MIT-LICENSE.txt",
                        "src/PhialeTech/Shared/PhialeTech.WebAssets/Assets/ReportDesigner/vendor/qrcode.js",
                        "src/PhialeTech/Shared/PhialeTech.WebAssets/Assets/ReportDesigner/vendor/qrcode_UTF8.js"
                    }),
                new DemoThirdPartyLicenseEntryViewModel(
                    "Monaco Editor",
                    "MonacoEditor browser-hosted code editor surface",
                    "MIT",
                    "Copyright (c) Microsoft Corporation",
                    "Keep the MIT license text and third-party notices with distributed Monaco assets. No in-product UI attribution is required.",
                    new[]
                    {
                        "src/PhialeTech/Shared/PhialeTech.WebAssets/Assets/Monaco/LICENSE",
                        "src/PhialeTech/Shared/PhialeTech.WebAssets/Assets/Monaco/ThirdPartyNotices.txt",
                        "src/PhialeTech/Shared/PhialeTech.WebAssets/Assets/Monaco/min/vs/loader.js"
                    }),
                new DemoThirdPartyLicenseEntryViewModel(
                    "Tiptap OSS DocumentEditor",
                    "DocumentEditor browser-hosted rich text surface with HTML, Markdown, and JSON persistence",
                    "MIT",
                    "Copyright (c) Tiptap, ueberdosis and other OSS contributors",
                    "Keep the MIT license text and third-party notices with the bundled DocumentEditor assets. Markdown support is provided by the OSS @tiptap/markdown package only, without paid conversion/import services.",
                    new[]
                    {
                        "src/PhialeTech/Shared/PhialeTech.WebAssets/Assets/DocumentEditor/ThirdPartyNotices.md",
                        "src/PhialeTech/Shared/PhialeTech.WebAssets/Assets/DocumentEditor/phiale-document-editor-host.js",
                        "src/PhialeTech/Shared/PhialeTech.WebAssets/Assets/DocumentEditor/index.html"
                    }),
            };
        }

        public static string BuildMyLicenseMarkdown()
        {
            var builder = new StringBuilder();
            builder.AppendLine("# My License");
            builder.AppendLine();
            builder.AppendLine("Reserved placeholder.");
            builder.AppendLine();
            builder.AppendLine("Fill this card with the product or company license text when it is ready to publish.");
            return builder.ToString();
        }

        public static string BuildThirdPartyMarkdown()
        {
            var builder = new StringBuilder();
            builder.AppendLine("# Third-party licenses");
            builder.AppendLine();

            foreach (var entry in BuildThirdPartyEntries())
            {
                builder.AppendLine($"## {entry.ComponentName}");
                builder.AppendLine();
                builder.AppendLine($"- Used by: {entry.UsedBy}");
                builder.AppendLine($"- License: {entry.LicenseName}");
                builder.AppendLine($"- Copyright: {entry.CopyrightNotice}");
                builder.AppendLine($"- Requirement: {entry.RequirementSummary}");
                builder.AppendLine("- Local files:");
                foreach (var file in entry.LocalFiles)
                {
                    builder.AppendLine($"  - {file}");
                }

                builder.AppendLine();
            }

            return builder.ToString();
        }
    }
}
