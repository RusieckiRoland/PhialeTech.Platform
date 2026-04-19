using System;
using System.Threading.Tasks;

namespace PhialeTech.ReportDesigner.Abstractions
{
    public interface IReportDesigner : IDisposable
    {
        ReportDesignerOptions Options { get; }

        bool IsInitialized { get; }

        bool IsReady { get; }

        ReportDesignerMode Mode { get; }

        string Locale { get; }

        string Theme { get; }

        event EventHandler<ReportDesignerReadyStateChangedEventArgs> ReadyStateChanged;

        event EventHandler<ReportDefinitionChangedEventArgs> DefinitionChanged;

        event EventHandler<ReportPreviewReadyEventArgs> PreviewReady;

        event EventHandler<ReportDesignerModeChangedEventArgs> ModeChanged;

        event EventHandler<ReportDesignerErrorEventArgs> ErrorOccurred;

        Task InitializeAsync();

        Task SetModeAsync(ReportDesignerMode mode);

        Task SetLocaleAsync(string locale);

        Task SetThemeAsync(string theme);

        Task LoadDefinitionAsync(ReportDefinition definition);

        Task<ReportDefinition> GetDefinitionAsync();

        Task SetDataSchemaAsync(ReportDataSchema schema);

        Task SetSampleDataAsync(string json);

        Task SetReportDataAsync(string json);

        Task RefreshPreviewAsync();

        Task PrintAsync();

        void FocusDesigner();
    }
}
