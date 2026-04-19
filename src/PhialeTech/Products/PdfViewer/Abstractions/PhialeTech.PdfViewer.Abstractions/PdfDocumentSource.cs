using System;
using System.IO;

namespace PhialeTech.PdfViewer.Abstractions
{
    public sealed class PdfDocumentSource
    {
        private PdfDocumentSource()
        {
        }

        public Uri Uri { get; private set; }

        public string FilePath { get; private set; }

        public byte[] Bytes { get; private set; }

        public Stream Stream { get; private set; }

        public string FileName { get; private set; } = "document.pdf";

        public bool LeaveStreamOpen { get; private set; }

        public static PdfDocumentSource FromUri(Uri uri, string fileName = null)
        {
            if (uri == null) throw new ArgumentNullException(nameof(uri));

            return new PdfDocumentSource
            {
                Uri = uri,
                FileName = string.IsNullOrWhiteSpace(fileName)
                    ? GetSafeFileName(uri.Segments.Length > 0 ? uri.Segments[uri.Segments.Length - 1] : null)
                    : fileName
            };
        }

        public static PdfDocumentSource FromFilePath(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path must be provided.", nameof(filePath));

            return new PdfDocumentSource
            {
                FilePath = Path.GetFullPath(filePath),
                FileName = Path.GetFileName(filePath)
            };
        }

        public static PdfDocumentSource FromBytes(byte[] bytes, string fileName = "document.pdf")
        {
            if (bytes == null) throw new ArgumentNullException(nameof(bytes));

            return new PdfDocumentSource
            {
                Bytes = bytes,
                FileName = GetSafeFileName(fileName)
            };
        }

        public static PdfDocumentSource FromStream(Stream stream, string fileName = "document.pdf", bool leaveOpen = false)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            return new PdfDocumentSource
            {
                Stream = stream,
                FileName = GetSafeFileName(fileName),
                LeaveStreamOpen = leaveOpen
            };
        }

        private static string GetSafeFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return "document.pdf";

            return fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0
                ? "document.pdf"
                : fileName;
        }
    }
}
