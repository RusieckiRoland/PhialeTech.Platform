using System;

namespace PhialeTech.ComponentHost.Abstractions.Presentation
{
    public sealed class HostedSurfaceRequest : IHostedSurfaceRequest
    {
        private string _contentKey = string.Empty;
        private string _title = string.Empty;
        private string _payload = string.Empty;

        public HostedSurfaceKind SurfaceKind { get; set; }

        public string ContentKey
        {
            get => _contentKey;
            set => _contentKey = value ?? string.Empty;
        }

        public HostedPresentationMode PresentationMode { get; set; } = HostedPresentationMode.Inline;

        public HostedEntranceStyle EntranceStyle { get; set; } = HostedEntranceStyle.Directional;

        public HostedPresentationSize Size { get; set; } = HostedPresentationSize.Medium;

        public HostedSheetPlacement Placement { get; set; } = HostedSheetPlacement.Center;

        public bool CanDismiss { get; set; } = true;

        public string Title
        {
            get => _title;
            set => _title = value ?? string.Empty;
        }

        public string Payload
        {
            get => _payload;
            set => _payload = value ?? string.Empty;
        }

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(ContentKey))
            {
                throw new ArgumentException("Hosted surface content key is required.", nameof(ContentKey));
            }
        }
    }
}
