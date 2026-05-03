using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using PhialeTech.ComponentHost.Abstractions.State;
using PhialeTech.ComponentHost.State;

namespace PhialeGis.Library.Tests.ApplicationState
{
    [TestFixture]
    public class ApplicationStateManagerTests
    {
        [Test]
        public void RegisterAndSaveRegisteredState_ShouldPersistAndRestoreStateAcrossManagerInstances()
        {
            var rootDirectory = CreateTemporaryDirectory();
            const string stateKey = "Demo/Grid/Grouping";

            try
            {
                var initialState = new TestViewState
                {
                    ColumnWidth = 320d,
                    FilterText = "Krakow",
                    IsColumnVisible = false,
                };

                using (var firstManager = new ApplicationStateManager(new JsonApplicationStateStore("PhialeTech.Components", rootDirectory)))
                {
                    var firstComponent = new TestStatefulComponent();
                    using var registration = firstManager.Register(stateKey, firstComponent);
                    firstComponent.UpdateState(initialState);
                    firstManager.SaveRegisteredState(stateKey);
                }

                using (var secondManager = new ApplicationStateManager(new JsonApplicationStateStore("PhialeTech.Components", rootDirectory)))
                {
                    var restoredComponent = new TestStatefulComponent();
                    using var registration = secondManager.Register(stateKey, restoredComponent);

                    Assert.Multiple(() =>
                    {
                        Assert.That(registration.RestoredFromStore, Is.True);
                        Assert.That(restoredComponent.State.ColumnWidth, Is.EqualTo(320d));
                        Assert.That(restoredComponent.State.FilterText, Is.EqualTo("Krakow"));
                        Assert.That(restoredComponent.State.IsColumnVisible, Is.False);
                    });
                }
            }
            finally
            {
                DeleteDirectory(rootDirectory);
            }
        }

        [Test]
        public void DifferentStateKeys_ShouldRemainIsolated()
        {
            var store = new InMemoryApplicationStateStore();
            using var manager = new ApplicationStateManager(store);
            var first = new TestStatefulComponent();
            var second = new TestStatefulComponent();

            using var firstRegistration = manager.Register("Demo/Grid/Grouping", first);
            using var secondRegistration = manager.Register("Demo/Grid/Filtering", second);

            first.UpdateState(new TestViewState { FilterText = "Owner", ColumnWidth = 140d, IsColumnVisible = true });
            second.UpdateState(new TestViewState { FilterText = "Municipality", ColumnWidth = 240d, IsColumnVisible = false });

            manager.SaveRegisteredState("Demo/Grid/Grouping");
            manager.SaveRegisteredState("Demo/Grid/Filtering");

            Assert.Multiple(() =>
            {
                Assert.That(store.Payloads["Demo/Grid/Grouping"], Does.Contain("Owner"));
                Assert.That(store.Payloads["Demo/Grid/Filtering"], Does.Contain("Municipality"));
            });
        }

        [Test]
        public void ReusableViewScenario_ShouldPersistSameComponentTypeIndependentlyForTwoKeys()
        {
            var rootDirectory = CreateTemporaryDirectory();

            try
            {
                using (var manager = new ApplicationStateManager(new JsonApplicationStateStore("PhialeTech.Components", rootDirectory)))
                {
                    var dashboardGrid = new TestStatefulComponent();
                    var dialogGrid = new TestStatefulComponent();

                    using var dashboardRegistration = manager.Register("Dashboard/RecentItemsGrid", dashboardGrid);
                    using var dialogRegistration = manager.Register("Orders/Dialog/ItemsGrid", dialogGrid);

                    dashboardGrid.UpdateState(new TestViewState { FilterText = "Dashboard", ColumnWidth = 220d, IsColumnVisible = true });
                    dialogGrid.UpdateState(new TestViewState { FilterText = "Dialog", ColumnWidth = 180d, IsColumnVisible = false });

                    manager.SaveRegisteredState("Dashboard/RecentItemsGrid");
                    manager.SaveRegisteredState("Orders/Dialog/ItemsGrid");
                }

                using (var manager = new ApplicationStateManager(new JsonApplicationStateStore("PhialeTech.Components", rootDirectory)))
                {
                    var dashboardGrid = new TestStatefulComponent();
                    var dialogGrid = new TestStatefulComponent();

                    using var dashboardRegistration = manager.Register("Dashboard/RecentItemsGrid", dashboardGrid);
                    using var dialogRegistration = manager.Register("Orders/Dialog/ItemsGrid", dialogGrid);

                    Assert.Multiple(() =>
                    {
                        Assert.That(dashboardRegistration.RestoredFromStore, Is.True);
                        Assert.That(dialogRegistration.RestoredFromStore, Is.True);
                        Assert.That(dashboardGrid.State.FilterText, Is.EqualTo("Dashboard"));
                        Assert.That(dialogGrid.State.FilterText, Is.EqualTo("Dialog"));
                    });
                }
            }
            finally
            {
                DeleteDirectory(rootDirectory);
            }
        }

        [Test]
        public void Register_WhenJsonIsCorrupted_ShouldSkipRestoreWithoutThrowing()
        {
            var rootDirectory = CreateTemporaryDirectory();
            const string stateKey = "Demo/Grid/Filtering";

            try
            {
                var store = new JsonApplicationStateStore("PhialeTech.Components", rootDirectory);
                var filePath = store.GetFilePath(stateKey);
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                File.WriteAllText(filePath, "{ this is not valid json");

                using var manager = new ApplicationStateManager(store);
                var component = new TestStatefulComponent(new TestViewState
                {
                    ColumnWidth = 100d,
                    FilterText = string.Empty,
                    IsColumnVisible = true,
                });

                using var registration = manager.Register(stateKey, component);

                Assert.Multiple(() =>
                {
                    Assert.That(registration.RestoredFromStore, Is.False);
                    Assert.That(component.State.ColumnWidth, Is.EqualTo(100d));
                    Assert.That(component.State.FilterText, Is.Empty);
                });
            }
            finally
            {
                DeleteDirectory(rootDirectory);
            }
        }

        [Test]
        public void ComponentWithoutManager_ShouldNotPersistStateImplicitly()
        {
            var store = new InMemoryApplicationStateStore();
            var component = new TestStatefulComponent();

            component.UpdateState(new TestViewState
            {
                ColumnWidth = 260d,
                FilterText = "No manager",
                IsColumnVisible = true,
            });

            Assert.That(store.Payloads, Is.Empty);
        }

        [Test]
        public void Save_ShouldIncludeVersionMetadata()
        {
            var store = new InMemoryApplicationStateStore();
            using var manager = new ApplicationStateManager(store);

            manager.Save("Demo/Grid/ColumnChooser", new TestViewState
            {
                ColumnWidth = 180d,
                FilterText = "Owner",
                IsColumnVisible = false,
            });

            var payload = store.Payloads["Demo/Grid/ColumnChooser"];
            Assert.Multiple(() =>
            {
                Assert.That(payload, Does.Contain("\"Version\": 1"));
                Assert.That(payload, Does.Contain("\"StateType\""));
                Assert.That(payload, Does.Contain("\"State\""));
            });
        }

        [Test]
        public void Constructor_WhenStoreIsMissing_ShouldThrowInsteadOfUsingHiddenFallback()
        {
            Assert.That(
                () => new ApplicationStateManager(null),
                Throws.ArgumentNullException.With.Property("ParamName").EqualTo("store"));
        }

        [Test]
        public void JsonApplicationStateStore_DefaultRootDirectory_ShouldUseApplicationDirectoryWithoutImplicitUiStateSuffix()
        {
            var applicationName = "PhialeTech.Components.Tests." + Guid.NewGuid().ToString("N");
            var store = new JsonApplicationStateStore(applicationName);

            try
            {
                Assert.Multiple(() =>
                {
                    Assert.That(Path.GetFileName(store.RootDirectory), Is.EqualTo(applicationName));
                    Assert.That(store.RootDirectory, Does.Not.Contain("ui-state"));
                });
            }
            finally
            {
                DeleteDirectory(store.RootDirectory);
            }
        }

        [Test]
        public void Register_WhenStoredStateFailsDuringComponentApply_ShouldDeletePayloadAndSkipRestore()
        {
            var store = new InMemoryApplicationStateStore();
            using (var seedManager = new ApplicationStateManager(store))
            {
                seedManager.Save("Demo/Grid/Editing", new TestViewState
                {
                    ColumnWidth = 240d,
                    FilterText = "Persisted",
                    IsColumnVisible = true,
                });
            }

            using var manager = new ApplicationStateManager(store);
            var component = new ThrowingStatefulComponent();

            using var registration = manager.Register("Demo/Grid/Editing", component);

            Assert.Multiple(() =>
            {
                Assert.That(registration.RestoredFromStore, Is.False);
                Assert.That(component.ApplyCallCount, Is.EqualTo(1));
                Assert.That(store.Payloads.ContainsKey("Demo/Grid/Editing"), Is.False);
            });
        }

        [Test]
        public void TryRestoreRegisteredState_WhenStoredStateFailsDuringComponentApply_ShouldDeletePayloadAndReturnFalse()
        {
            var store = new InMemoryApplicationStateStore();
            using var manager = new ApplicationStateManager(store);
            var component = new TestStatefulComponent();
            using var registration = manager.Register("Demo/Grid/Editing", component);

            manager.Save("Demo/Grid/Editing", new TestViewState
            {
                ColumnWidth = 260d,
                FilterText = "Persisted",
                IsColumnVisible = false,
            });

            component.ThrowOnApply = true;

            var restored = manager.TryRestoreRegisteredState("Demo/Grid/Editing");

            Assert.Multiple(() =>
            {
                Assert.That(restored, Is.False);
                Assert.That(component.ApplyCallCount, Is.EqualTo(1));
                Assert.That(store.Payloads.ContainsKey("Demo/Grid/Editing"), Is.False);
            });
        }

        [Test]
        public void Preload_WhenStateIsRegisteredLater_ShouldRestoreAndConsumePreloadedState()
        {
            var store = new InMemoryApplicationStateStore();
            using var manager = new ApplicationStateManager(store);
            const string stateKey = "Demo/Grid/Editing";

            manager.Save(stateKey, new TestViewState
            {
                ColumnWidth = 260d,
                FilterText = "Persisted",
                IsColumnVisible = false,
            });

            var preloaded = manager.Preload<TestViewState>(stateKey);
            var component = new MutatingApplyStatefulComponent();
            using var registration = manager.Register(stateKey, component);

            manager.TryLoad<TestViewState>(stateKey, out var loadedState);

            Assert.Multiple(() =>
            {
                Assert.That(preloaded, Is.True);
                Assert.That(registration.RestoredFromStore, Is.True);
                Assert.That(component.ApplyCallCount, Is.EqualTo(1));
                Assert.That(component.State.FilterText, Is.EqualTo("Mutated by component"));
                Assert.That(loadedState.FilterText, Is.EqualTo("Persisted"));
            });
        }

        [Test]
        public void Preload_WhenStateDoesNotExist_ShouldReturnFalseWithoutCreatingPayload()
        {
            var store = new InMemoryApplicationStateStore();
            using var manager = new ApplicationStateManager(store);

            var preloaded = manager.Preload<TestViewState>("Demo/Grid/Missing");

            Assert.Multiple(() =>
            {
                Assert.That(preloaded, Is.False);
                Assert.That(store.Payloads, Is.Empty);
            });
        }

        private static string CreateTemporaryDirectory()
        {
            var path = Path.Combine(Path.GetTempPath(), "PhialeTech.ComponentHost.Tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(path);
            return path;
        }

        private static void DeleteDirectory(string path)
        {
            if (!string.IsNullOrWhiteSpace(path) && Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }
        }

        private sealed class TestViewState
        {
            public double ColumnWidth { get; set; }

            public string FilterText { get; set; }

            public bool IsColumnVisible { get; set; }
        }

        private sealed class TestStatefulComponent : IStatefulComponent<TestViewState>
        {
            public TestStatefulComponent(TestViewState initialState = null)
            {
                State = initialState ?? new TestViewState();
            }

            public event EventHandler StateChanged;

            public TestViewState State { get; private set; }

            public int ApplyCallCount { get; private set; }

            public bool ThrowOnApply { get; set; }

            public TestViewState ExportState()
            {
                return new TestViewState
                {
                    ColumnWidth = State.ColumnWidth,
                    FilterText = State.FilterText,
                    IsColumnVisible = State.IsColumnVisible,
                };
            }

            public void ApplyState(TestViewState state)
            {
                ApplyCallCount++;
                if (ThrowOnApply)
                {
                    throw new InvalidOperationException("Simulated apply failure.");
                }

                State = state ?? new TestViewState();
            }

            public void UpdateState(TestViewState state)
            {
                ApplyState(state);
                StateChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private sealed class ThrowingStatefulComponent : IStatefulComponent<TestViewState>
        {
            public event EventHandler StateChanged;

            public int ApplyCallCount { get; private set; }

            public TestViewState ExportState()
            {
                return new TestViewState();
            }

            public void ApplyState(TestViewState state)
            {
                ApplyCallCount++;
                throw new InvalidOperationException("Simulated apply failure.");
            }
        }

        private sealed class MutatingApplyStatefulComponent : IStatefulComponent<TestViewState>
        {
            public event EventHandler StateChanged;

            public TestViewState State { get; private set; }

            public int ApplyCallCount { get; private set; }

            public TestViewState ExportState()
            {
                return State ?? new TestViewState();
            }

            public void ApplyState(TestViewState state)
            {
                ApplyCallCount++;
                State = state ?? new TestViewState();
                State.FilterText = "Mutated by component";
            }
        }

        private sealed class InMemoryApplicationStateStore : IApplicationStateStore
        {
            public Dictionary<string, string> Payloads { get; } = new Dictionary<string, string>(StringComparer.Ordinal);

            public void Save(string stateKey, string payload)
            {
                Payloads[stateKey] = payload ?? string.Empty;
            }

            public string Load(string stateKey)
            {
                return Payloads.TryGetValue(stateKey, out var payload) ? payload : null;
            }

            public void Delete(string stateKey)
            {
                Payloads.Remove(stateKey);
            }
        }
    }
}
