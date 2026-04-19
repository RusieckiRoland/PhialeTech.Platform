using NUnit.Framework;
using PhialeTech.ReportDesigner;
using PhialeTech.ReportDesigner.Abstractions;
using PhialeTech.WebHost.Abstractions.Ui.Web;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

namespace PhialeTech.ReportDesigner.Tests
{
    [TestFixture]
    public sealed class ReportDesignerRuntimeTests
    {
        [Test]
        public void NormalizeLocale_ReturnsExpectedLanguageCodes()
        {
            Assert.That(ReportDesignerShellTextCatalog.NormalizeLocale("pl-PL"), Is.EqualTo("pl"));
            Assert.That(ReportDesignerShellTextCatalog.NormalizeLocale("en_US"), Is.EqualTo("en"));
            Assert.That(ReportDesignerShellTextCatalog.NormalizeLocale("de-DE"), Is.EqualTo("en"));
        }

        [Test]
        public void GetText_ReturnsPolishTranslationWithDiacritics()
        {
            string text = ReportDesignerShellTextCatalog.GetText("pl-PL", "Error.Print");

            Assert.That(text, Is.EqualTo("Nie udało się uruchomić wydruku."));
            Assert.That(text, Does.Contain("się"));
        }

        [Test]
        public async Task SetLocaleAsync_PostsNormalizedLocaleMessage()
        {
            using var environment = CreateRuntimeEnvironment();

            await environment.Runtime.SetLocaleAsync("pl-PL");

            JsonElement message = environment.Host.SinglePostedMessage();
            Assert.That(message.GetProperty("type").GetString(), Is.EqualTo("reportDesigner.setLocale"));
            Assert.That(message.GetProperty("locale").GetString(), Is.EqualTo("pl"));
        }

        [Test]
        public async Task GetDefinitionAsync_CompletesFromDefinitionSnapshotMessage()
        {
            using var environment = CreateRuntimeEnvironment();

            Task<ReportDefinition> pending = environment.Runtime.GetDefinitionAsync();
            JsonElement requestMessage = environment.Host.SinglePostedMessage();
            string requestId = requestMessage.GetProperty("requestId").GetString();

            environment.Host.RaiseMessage(new
            {
                type = "reportDesigner.definitionSnapshot",
                requestId,
                definition = new ReportDefinition
                {
                    Blocks =
                    {
                        new ReportBlockDefinition
                        {
                            Id = "block-1",
                            Type = "Text",
                            Name = "Nagłówek"
                        }
                    }
                }
            });

            ReportDefinition definition = await pending;

            Assert.That(definition.Blocks.Count, Is.EqualTo(1));
            Assert.That(definition.Blocks[0].Name, Is.EqualTo("Nagłówek"));
        }

        [Test]
        public void ReportDefinition_SerializesNewInvoiceFriendlyBlocksAndPaginationFlags()
        {
            var definition = new ReportDefinition
            {
                Sections =
                {
                    ReportHeader =
                    {
                        Blocks =
                        {
                            new ReportBlockDefinition
                            {
                                Id = "report-header-text",
                                Type = "Text",
                                Name = "Document title"
                            }
                        }
                    },
                    PageHeader =
                    {
                        SkipFirstPage = true,
                        Blocks =
                        {
                            new ReportBlockDefinition
                            {
                                Id = "page-header-text",
                                Type = "Text",
                                Name = "Repeated header"
                            }
                        }
                    },
                    Body =
                    {
                        Blocks =
                        {
                            new ReportBlockDefinition
                            {
                                Id = "columns-1",
                                Type = "Columns",
                                ColumnCount = 2,
                                ColumnGap = "18px",
                                KeepTogether = true,
                                Children =
                                {
                                    new ReportBlockDefinition
                                    {
                                        Id = "field-list-1",
                                        Type = "FieldList",
                                        Fields =
                                        {
                                            new ReportFieldListItemDefinition
                                            {
                                                Label = "Invoice number",
                                                Binding = "InvoiceNumber",
                                                Format = new ReportValueFormat
                                                {
                                                    Kind = "text"
                                                }
                                            }
                                        }
                                    }
                                }
                            },
                            new ReportBlockDefinition
                            {
                                Id = "table-1",
                                Type = "Table",
                                ItemsSource = "Items",
                                RepeatHeader = true,
                                PageBreakBefore = true,
                                Columns =
                                {
                                    new ReportTableColumnDefinition
                                    {
                                        Header = "Amount",
                                        Binding = "Total",
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
                            }
                        }
                    },
                    PageFooter =
                    {
                        SkipLastPage = true,
                        Blocks =
                        {
                            new ReportBlockDefinition
                            {
                                Id = "special-1",
                                Type = "SpecialField",
                                SpecialFieldKind = ReportSpecialFieldKinds.PageNumberOfTotalPages
                            }
                        }
                    }
                },
                Blocks =
                {
                    new ReportBlockDefinition { Id = "legacy-body", Type = "Text", Name = "Legacy body" }
                }
            };

            string json = JsonSerializer.Serialize(definition);
            ReportDefinition roundTrip = JsonSerializer.Deserialize<ReportDefinition>(json);

            Assert.That(roundTrip.Sections.ReportHeader.Blocks[0].Type, Is.EqualTo("Text"));
            Assert.That(roundTrip.Sections.PageHeader.SkipFirstPage, Is.True);
            Assert.That(roundTrip.Sections.Body.Blocks[0].Type, Is.EqualTo("Columns"));
            Assert.That(roundTrip.Sections.Body.Blocks[0].ColumnCount, Is.EqualTo(2));
            Assert.That(roundTrip.Sections.Body.Blocks[0].Children[0].Fields[0].Label, Is.EqualTo("Invoice number"));
            Assert.That(roundTrip.Sections.Body.Blocks[1].Columns[0].Width, Is.EqualTo("25%"));
            Assert.That(roundTrip.Sections.Body.Blocks[1].Columns[0].TextAlign, Is.EqualTo("right"));
            Assert.That(roundTrip.Sections.Body.Blocks[1].PageBreakBefore, Is.True);
            Assert.That(roundTrip.Sections.PageFooter.SkipLastPage, Is.True);
            Assert.That(roundTrip.Sections.PageFooter.Blocks[0].SpecialFieldKind, Is.EqualTo(ReportSpecialFieldKinds.PageNumberOfTotalPages));
            Assert.That(roundTrip.Blocks[0].Name, Is.EqualTo("Legacy body"));
        }

        private static RuntimeEnvironment CreateRuntimeEnvironment()
        {
            var host = new FakeWebComponentHost();
            var workspace = new ReportDesignerWorkspace(new ReportDesignerOptions());
            var runtime = new ReportDesignerRuntime(host, workspace, new ReportDesignerOptions());

            typeof(ReportDesignerRuntime)
                .GetField("_initializeTask", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(runtime, Task.CompletedTask);

            return new RuntimeEnvironment(runtime, host);
        }

        private sealed class RuntimeEnvironment : IDisposable
        {
            public RuntimeEnvironment(ReportDesignerRuntime runtime, FakeWebComponentHost host)
            {
                Runtime = runtime;
                Host = host;
            }

            public ReportDesignerRuntime Runtime { get; }

            public FakeWebComponentHost Host { get; }

            public void Dispose()
            {
                Runtime.Dispose();
            }
        }

        private sealed class FakeWebComponentHost : IWebComponentHost
        {
            private readonly List<string> _postedMessages = new List<string>();

            public WebComponentHostOptions Options { get; } = new WebComponentHostOptions();

            public bool IsInitialized { get; private set; } = true;

            public bool IsReady { get; private set; }

            public event EventHandler<WebComponentMessageEventArgs> MessageReceived;

            public event EventHandler<WebComponentReadyStateChangedEventArgs> ReadyStateChanged;

            public Task InitializeAsync()
            {
                IsInitialized = true;
                return Task.CompletedTask;
            }

            public Task LoadEntryPageAsync(string entryPageRelativePath) => Task.CompletedTask;

            public Task NavigateAsync(Uri uri) => Task.CompletedTask;

            public Task LoadHtmlAsync(string html, string baseUrl = null) => Task.CompletedTask;

            public Task PostMessageAsync(object message)
            {
                _postedMessages.Add(JsonSerializer.Serialize(message));
                return Task.CompletedTask;
            }

            public Task PostRawMessageAsync(string rawMessage)
            {
                _postedMessages.Add(rawMessage ?? string.Empty);
                return Task.CompletedTask;
            }

            public Task<string> ExecuteScriptAsync(string script) => Task.FromResult(string.Empty);

            public void FocusHost()
            {
            }

            public JsonElement SinglePostedMessage()
            {
                Assert.That(_postedMessages.Count, Is.EqualTo(1), "Expected exactly one posted message.");
                using var document = JsonDocument.Parse(_postedMessages[0]);
                return document.RootElement.Clone();
            }

            public void RaiseMessage(object payload)
            {
                string rawMessage = JsonSerializer.Serialize(payload);
                using var document = JsonDocument.Parse(rawMessage);
                string messageType = document.RootElement.TryGetProperty("type", out var typeValue)
                    ? typeValue.GetString() ?? string.Empty
                    : string.Empty;
                MessageReceived?.Invoke(this, new WebComponentMessageEventArgs(rawMessage, messageType));
            }

            public void RaiseReadyState(bool isInitialized, bool isReady)
            {
                IsInitialized = isInitialized;
                IsReady = isReady;
                ReadyStateChanged?.Invoke(this, new WebComponentReadyStateChangedEventArgs(isInitialized, isReady));
            }

            public void Dispose()
            {
            }
        }
    }
}
