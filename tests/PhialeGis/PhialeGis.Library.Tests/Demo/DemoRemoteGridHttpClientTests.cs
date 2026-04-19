using System;
using System.Diagnostics;
using System.Globalization;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using PhialeGrid.Core.Query;
using PhialeGis.Library.Tests.Support;
using PhialeTech.Components.Shared.Services;
using PhialeTech.Components.Shared.ViewModels;

namespace PhialeGis.Library.Tests.Demo
{
    [TestFixture]
    public sealed class DemoRemoteGridHttpClientTests
    {
        [Test]
        public async Task QueryAsync_AgainstNodeMockServer_ReturnsPageAndMapsRecords()
        {
            using (var server = await NodeMockServerHost.StartAsync().ConfigureAwait(false))
            using (var httpClient = new HttpClient())
            {
                var client = new DemoRemoteGridHttpClient(httpClient, server.BaseAddress);

                var result = await client.QueryAsync(
                    new DemoRemoteQueryRequest(
                        offset: 0,
                        size: 20,
                        sorts: Array.Empty<GridSortDescriptor>(),
                        filterGroup: GridFilterGroup.EmptyAnd(),
                        refreshGeneration: 0)).ConfigureAwait(false);

                Assert.Multiple(() =>
                {
                    Assert.That(result.TotalCount, Is.EqualTo(530));
                    Assert.That(result.ReturnedCount, Is.EqualTo(20));
                    Assert.That(result.Items.Count, Is.EqualTo(20));
                    Assert.That(result.Items[0].ObjectId, Is.EqualTo("DZ-KRA-STA-0001"));
                    Assert.That(result.Items[0].Category, Is.EqualTo("Parcel"));
                });
            }
        }

        [Test]
        public async Task ViewModel_WhenBackedByHttpClient_PagesRefreshesAndUsesServerSorting()
        {
            using (var server = await NodeMockServerHost.StartAsync().ConfigureAwait(false))
            using (var httpClient = new HttpClient())
            {
                var client = new DemoRemoteGridHttpClient(httpClient, server.BaseAddress);
                var viewModel = new DemoShellViewModel("Wpf", remoteGridClient: client);

                viewModel.SelectExample("remote-data");
                await viewModel.LoadCurrentRemotePageAsync().ConfigureAwait(false);
                var firstPageFirstId = viewModel.GridRecords[0].ObjectId;

                await viewModel.LoadNextRemotePageAsync().ConfigureAwait(false);
                var secondPageFirstId = viewModel.GridRecords[0].ObjectId;
                var inspectionBeforeRefresh = viewModel.GridRecords[0].LastInspection;

                viewModel.GridSorts = new[]
                {
                    new GridSortDescriptor("LastInspection", GridSortDirection.Descending),
                    new GridSortDescriptor("ObjectId", GridSortDirection.Ascending),
                };

                await viewModel.RefreshRemotePageAsync().ConfigureAwait(false);

                Assert.Multiple(() =>
                {
                    Assert.That(firstPageFirstId, Is.Not.EqualTo(secondPageFirstId));
                    Assert.That(viewModel.GridRecords[0].LastInspection, Is.GreaterThan(inspectionBeforeRefresh));
                    Assert.That(viewModel.RemoteStatusText, Does.Contain("2"));
                    Assert.That(viewModel.GridRecords[0].LastInspection, Is.GreaterThanOrEqualTo(viewModel.GridRecords[1].LastInspection));
                });
            }
        }

        private sealed class NodeMockServerHost : IDisposable
        {
            private readonly Process _process;
            private readonly StringBuilder _output;

            private NodeMockServerHost(Process process, string baseAddress, StringBuilder output)
            {
                _process = process;
                BaseAddress = baseAddress;
                _output = output;
            }

            public string BaseAddress { get; }

            public static async Task<NodeMockServerHost> StartAsync()
            {
                var port = GetFreePort();
                var output = new StringBuilder();
                var startInfo = new ProcessStartInfo
                {
                    FileName = "node",
                    Arguments = "src/index.js",
                    WorkingDirectory = System.IO.Path.Combine(
                        RepositoryPaths.GetRepositoryRoot(),
                        "src",
                        "PhialeTech",
                        "Products",
                        "Grid",
                        "MockServer",
                        "Js"),
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
                startInfo.Environment["PORT"] = port.ToString(CultureInfo.InvariantCulture);

                var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
                process.OutputDataReceived += (sender, args) =>
                {
                    if (args.Data != null)
                    {
                        output.AppendLine(args.Data);
                    }
                };
                process.ErrorDataReceived += (sender, args) =>
                {
                    if (args.Data != null)
                    {
                        output.AppendLine(args.Data);
                    }
                };

                if (!process.Start())
                {
                    throw new InvalidOperationException("Failed to start node mock server process.");
                }

                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                var host = new NodeMockServerHost(process, "http://127.0.0.1:" + port.ToString(CultureInfo.InvariantCulture) + "/", output);
                await host.WaitUntilReadyAsync().ConfigureAwait(false);
                return host;
            }

            public void Dispose()
            {
                try
                {
                    if (!_process.HasExited)
                    {
                        _process.Kill(entireProcessTree: true);
                        _process.WaitForExit(5000);
                    }
                }
                finally
                {
                    _process.Dispose();
                }
            }

            private async Task WaitUntilReadyAsync()
            {
                using (var httpClient = new HttpClient())
                {
                    var deadline = DateTime.UtcNow.AddSeconds(10);
                    Exception lastError = null;

                    while (DateTime.UtcNow < deadline)
                    {
                        try
                        {
                            var response = await httpClient.GetAsync(BaseAddress + "api/phialegrid/health").ConfigureAwait(false);
                            if (response.IsSuccessStatusCode)
                            {
                                return;
                            }
                        }
                        catch (Exception ex)
                        {
                            lastError = ex;
                        }

                        await Task.Delay(150).ConfigureAwait(false);
                    }

                    throw new InvalidOperationException("Node mock server did not become ready. Output:" + Environment.NewLine + _output, lastError);
                }
            }

            private static int GetFreePort()
            {
                var listener = new TcpListener(System.Net.IPAddress.Loopback, 0);
                try
                {
                    listener.Start();
                    return ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
                }
                finally
                {
                    listener.Stop();
                }
            }
        }
    }
}
