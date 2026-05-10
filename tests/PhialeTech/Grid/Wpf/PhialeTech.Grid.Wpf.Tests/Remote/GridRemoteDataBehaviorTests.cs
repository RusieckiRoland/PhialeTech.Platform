using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using NUnit.Framework;
using PhialeGrid.Core.Query;
using PhialeTech.Components.Shared.Model;
using PhialeTech.Components.Shared.Services;
using PhialeTech.Components.Shared.ViewModels;
using PhialeTech.PhialeGrid.Wpf.Controls;
using PhialeGrid.Wpf.Tests.Surface;
using WpfGrid = PhialeTech.PhialeGrid.Wpf.Controls.PhialeGrid;

namespace PhialeGrid.Wpf.Tests.Remote
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public sealed class GridRemoteDataBehaviorTests
    {
        [Test]
        public void DemoBinding_WhenRemoteScenarioLoadsNextPrevAndRefresh_LiveGridTracksHttpPages()
        {
            using (var server = NodeMockServerHost.StartAsync().GetAwaiter().GetResult())
            using (var httpClient = new HttpClient())
            {
                var viewModel = new DemoShellViewModel("Wpf", remoteGridClient: new DemoRemoteGridHttpClient(httpClient, server.BaseAddress));
                var grid = CreateBoundDemoGrid(viewModel);
                    var window = GridSurfaceTestHost.CreateHostWindow(grid, width: 1280, height: 720);

                try
                {
                    window.Show();
                    GridSurfaceTestHost.FlushDispatcher(grid);

                    viewModel.SelectExample("remote-data");
                    viewModel.LoadCurrentRemotePageAsync().GetAwaiter().GetResult();
                    GridSurfaceTestHost.FlushDispatcher(grid);

                    var firstPageFirstId = viewModel.GridRecords[0].ObjectId;
                    var surfaceHost = GridSurfaceTestHost.FindSurfaceHost(grid);
                    Assert.That(surfaceHost.CurrentSnapshot.Rows.Count, Is.EqualTo(20));

                    viewModel.LoadNextRemotePageAsync().GetAwaiter().GetResult();
                    GridSurfaceTestHost.FlushDispatcher(grid);
                    var secondPageFirstId = viewModel.GridRecords[0].ObjectId;
                    var inspectionBeforeRefresh = viewModel.GridRecords[0].LastInspection;

                    viewModel.RefreshRemotePageAsync().GetAwaiter().GetResult();
                    GridSurfaceTestHost.FlushDispatcher(grid);
                    var inspectionAfterRefresh = viewModel.GridRecords[0].LastInspection;

                    viewModel.LoadPreviousRemotePageAsync().GetAwaiter().GetResult();
                    GridSurfaceTestHost.FlushDispatcher(grid);

                    Assert.Multiple(() =>
                    {
                        Assert.That(firstPageFirstId, Is.Not.EqualTo(secondPageFirstId));
                        Assert.That(viewModel.RemoteStatusText, Does.Contain("1"));
                        Assert.That(viewModel.GridRecords.Count, Is.EqualTo(20));
                        Assert.That(inspectionAfterRefresh, Is.GreaterThan(inspectionBeforeRefresh));
                        Assert.That(grid.RowsView.Cast<object>().Take(3).All(row => row is GridDataRowModel), Is.True);
                        Assert.That(surfaceHost.CurrentSnapshot.Rows.Count, Is.EqualTo(20));
                    });
                }
                finally
                {
                    window.Close();
                }
            }
        }

        [Test]
        public void DemoBinding_WhenRemoteScenarioUsesSorts_ServerPagingRespectsCurrentSortChain()
        {
            using (var server = NodeMockServerHost.StartAsync().GetAwaiter().GetResult())
            using (var httpClient = new HttpClient())
            {
                var viewModel = new DemoShellViewModel("Wpf", remoteGridClient: new DemoRemoteGridHttpClient(httpClient, server.BaseAddress));
                var grid = CreateBoundDemoGrid(viewModel);
                var window = GridSurfaceTestHost.CreateHostWindow(grid, width: 1280, height: 720);

                try
                {
                    window.Show();
                    GridSurfaceTestHost.FlushDispatcher(grid);

                    viewModel.SelectExample("remote-data");
                    viewModel.GridSorts = new[]
                    {
                        new GridSortDescriptor("LastInspection", GridSortDirection.Descending),
                        new GridSortDescriptor("ObjectId", GridSortDirection.Ascending),
                    };

                    viewModel.LoadCurrentRemotePageAsync().GetAwaiter().GetResult();
                    GridSurfaceTestHost.FlushDispatcher(grid);

                    var visibleRows = grid.RowsView.Cast<GridDataRowModel>().Take(5).ToArray();

                    Assert.Multiple(() =>
                    {
                        Assert.That(visibleRows.Length, Is.EqualTo(5));
                        Assert.That(((DateTime)visibleRows[0][nameof(DemoGisRecordViewModel.LastInspection)]), Is.GreaterThanOrEqualTo((DateTime)visibleRows[1][nameof(DemoGisRecordViewModel.LastInspection)]));
                        Assert.That(viewModel.GridSorts.Count, Is.EqualTo(2));
                    });
                }
                finally
                {
                    window.Close();
                }
            }
        }

        [Test]
        public void RemoteScenario_WhenRequestIsInFlight_ViewModelExposesLoadingState()
        {
            var completionSource = new TaskCompletionSource<DemoRemoteQueryResult>();
            var client = new DelegateRemoteGridClient((request, cancellationToken) => completionSource.Task);
            var viewModel = new DemoShellViewModel("Wpf", remoteGridClient: client);

            viewModel.SelectExample("remote-data");

            var loadTask = viewModel.LoadCurrentRemotePageAsync();

            Assert.Multiple(() =>
            {
                Assert.That(viewModel.IsRemoteBusy, Is.True);
                Assert.That(viewModel.RemoteDataState, Is.EqualTo(DemoRemoteDataStateKind.Loading));
                Assert.That(viewModel.RemoteStatusText, Does.Contain("Loading"));
            });

            completionSource.SetResult(new DemoRemoteQueryResult(0, 20, 1, 1, new[]
            {
                new DemoGisRecordViewModel("REMOTE-1", "Operations", "Viewport", "Polygon", "EPSG:2180", "Warsaw", "North", "Ready", 12m, 0m, DateTime.UtcNow.Date, "Remote", "Normal", true, true, "Ops", 1000, "remote"),
            }));

            loadTask.GetAwaiter().GetResult();

            Assert.That(viewModel.RemoteDataState, Is.EqualTo(DemoRemoteDataStateKind.Ready));
        }

        [Test]
        public void RemoteScenario_WhenResultIsEmpty_ViewModelExposesEmptyState()
        {
            var client = new DelegateRemoteGridClient((request, cancellationToken) =>
                Task.FromResult(new DemoRemoteQueryResult(request.Offset, request.Size, 0, 0, Array.Empty<DemoGisRecordViewModel>())));
            var viewModel = new DemoShellViewModel("Wpf", remoteGridClient: client);

            viewModel.SelectExample("remote-data");
            viewModel.LoadCurrentRemotePageAsync().GetAwaiter().GetResult();

            Assert.Multiple(() =>
            {
                Assert.That(viewModel.IsRemoteBusy, Is.False);
                Assert.That(viewModel.RemoteDataState, Is.EqualTo(DemoRemoteDataStateKind.Empty));
                Assert.That(viewModel.GridRecords, Is.Empty);
                Assert.That(viewModel.RemoteStatusText, Does.Contain("No remote rows"));
            });
        }

        [Test]
        public void RemoteScenario_WhenAccessIsForbidden_ViewModelExposesForbiddenState()
        {
            var client = new DelegateRemoteGridClient((request, cancellationToken) =>
                Task.FromException<DemoRemoteQueryResult>(new DemoRemoteQueryException(DemoRemoteQueryFailureKind.Forbidden, "Forbidden")));
            var viewModel = new DemoShellViewModel("Wpf", remoteGridClient: client);

            viewModel.SelectExample("remote-data");
            viewModel.LoadCurrentRemotePageAsync().GetAwaiter().GetResult();

            Assert.Multiple(() =>
            {
                Assert.That(viewModel.IsRemoteBusy, Is.False);
                Assert.That(viewModel.RemoteDataState, Is.EqualTo(DemoRemoteDataStateKind.Forbidden));
                Assert.That(viewModel.GridRecords, Is.Empty);
                Assert.That(viewModel.RemoteStatusText, Does.Contain("denied"));
            });
        }

        [Test]
        public void RemoteScenario_WhenRequestFails_ViewModelExposesErrorState()
        {
            var client = new DelegateRemoteGridClient((request, cancellationToken) =>
                Task.FromException<DemoRemoteQueryResult>(new InvalidOperationException("Socket timeout")));
            var viewModel = new DemoShellViewModel("Wpf", remoteGridClient: client);

            viewModel.SelectExample("remote-data");
            viewModel.LoadCurrentRemotePageAsync().GetAwaiter().GetResult();

            Assert.Multiple(() =>
            {
                Assert.That(viewModel.IsRemoteBusy, Is.False);
                Assert.That(viewModel.RemoteDataState, Is.EqualTo(DemoRemoteDataStateKind.Error));
                Assert.That(viewModel.GridRecords, Is.Empty);
                Assert.That(viewModel.RemoteStatusText, Does.Contain("Socket timeout"));
            });
        }

        private static WpfGrid CreateBoundDemoGrid(DemoShellViewModel viewModel)
        {
            var grid = new WpfGrid
            {
                Width = 1280,
                Height = 720,
                DataContext = viewModel,
                LanguageDirectory = global::PhialeGrid.Wpf.Tests.GridTestRepositoryPaths.GridLanguagesDirectory
            };

            BindingOperations.SetBinding(grid, WpfGrid.ItemsSourceProperty, new Binding(nameof(DemoShellViewModel.GridRecords)));
            BindingOperations.SetBinding(grid, WpfGrid.ColumnsProperty, new Binding(nameof(DemoShellViewModel.GridColumns)));
            BindingOperations.SetBinding(grid, WpfGrid.EditSessionContextProperty, new Binding(nameof(DemoShellViewModel.GridEditSessionContext)));
            BindingOperations.SetBinding(grid, WpfGrid.GroupsProperty, new Binding(nameof(DemoShellViewModel.GridGroups)) { Mode = BindingMode.TwoWay });
            BindingOperations.SetBinding(grid, WpfGrid.SortsProperty, new Binding(nameof(DemoShellViewModel.GridSorts)) { Mode = BindingMode.TwoWay });
            BindingOperations.SetBinding(grid, WpfGrid.SummariesProperty, new Binding(nameof(DemoShellViewModel.GridSummaries)));
            BindingOperations.SetBinding(grid, WpfGrid.IsGridReadOnlyProperty, new Binding(nameof(DemoShellViewModel.IsGridReadOnly)));
            return grid;
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
                    WorkingDirectory = global::PhialeGrid.Wpf.Tests.GridTestRepositoryPaths.GridMockServerJsDirectory,
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

        private sealed class DelegateRemoteGridClient : IDemoRemoteGridClient
        {
            private readonly Func<DemoRemoteQueryRequest, CancellationToken, Task<DemoRemoteQueryResult>> _handler;

            public DelegateRemoteGridClient(Func<DemoRemoteQueryRequest, CancellationToken, Task<DemoRemoteQueryResult>> handler)
            {
                _handler = handler;
            }

            public Task<DemoRemoteQueryResult> QueryAsync(DemoRemoteQueryRequest request, CancellationToken cancellationToken = default)
            {
                return _handler(request, cancellationToken);
            }
        }
    }
}




