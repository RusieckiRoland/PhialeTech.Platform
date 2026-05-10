using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using NUnit.Framework;
using PhialeGrid.Core;
using PhialeGrid.Core.Columns;
using PhialeGrid.Core.Query;
using PhialeGrid.Core.Regions;
using PhialeGrid.Localization;
using PhialeTech.Components.Shared.Model;
using PhialeTech.Components.Shared.ViewModels;
using PhialeTech.Components.Wpf;
using PhialeTech.PhialeGrid.Wpf.Controls;
using PhialeTech.PhialeGrid.Wpf.Controls.Editing;
using PhialeTech.PhialeGrid.Wpf.Surface;
using PhialeTech.PhialeGrid.Wpf.Surface.Presenters;
using PhialeGrid.Wpf.Tests.Surface;
using WpfGrid = PhialeTech.PhialeGrid.Wpf.Controls.PhialeGrid;

namespace PhialeGrid.Wpf.Tests.Editing
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class GridEditingBehaviorTests
    {
        [Test]
        public void GridDataRowModel_WhenSourceRowChanges_RaisesIndexerNotifications()
        {
            var grid = new WpfGrid();
            var row = CreateRow("GIS-1", "Valve A", "Operations");
            var model = new GridDataRowModel(grid, row);
            var notifications = new List<string>();

            model.PropertyChanged += (sender, args) => notifications.Add(args.PropertyName);

            row.ObjectName = "Valve A Updated";

            Assert.That(notifications, Does.Contain("Item[]"));
            Assert.That(notifications, Does.Contain(nameof(DemoGisRecordViewModel.ObjectName)));
        }

        [Test]
        public void GridStatusProperties_WhenReadImmediatelyAfterConstruction_DoNotThrow()
        {
            Assert.DoesNotThrow(() =>
            {
                var grid = new WpfGrid();

                Assert.Multiple(() =>
                {
                    Assert.That(grid.PendingEditCount, Is.EqualTo(0));
                    Assert.That(grid.PendingEditRowIds, Is.Empty);
                    Assert.That(grid.PendingEditRowIdsWithoutValidation, Is.Empty);
                    Assert.That(grid.ValidationIssueRowIds, Is.Empty);
                    Assert.That(grid.HasPendingEdits, Is.False);
                    Assert.That(grid.HasValidationIssues, Is.False);
                    Assert.That(grid.EditStatusText, Is.Not.Null);
                    Assert.That(grid.PendingEditBannerText, Is.Not.Null);
                });
            });
        }

        [Test]
        public void CancelEdits_RestoresOriginalValues_ForMultipleRows_AndNotifiesVisibleRowModels()
        {
            var grid = new WpfGrid();
            grid.LanguageDirectory = GetLanguageDirectory();
            var row1 = CreateRow("GIS-1", "Valve A", "Operations");
            var row2 = CreateRow("GIS-2", "Valve B", "Infrastructure");
            grid.ItemsSource = new[] { row1, row2 };

            var rowModel1 = new GridDataRowModel(grid, row1);
            var rowModel2 = new GridDataRowModel(grid, row2);
            var notifications = new List<string>();
            rowModel1.PropertyChanged += (sender, args) => notifications.Add("r1:" + args.PropertyName);
            rowModel2.PropertyChanged += (sender, args) => notifications.Add("r2:" + args.PropertyName);

            Assert.That(grid.EditSessionContext.TrySetFieldValue(row1.ObjectId, nameof(DemoGisRecordViewModel.ObjectName), "Valve A Updated"), Is.True);
            Assert.That(grid.EditSessionContext.TrySetFieldValue(row1.ObjectId, nameof(DemoGisRecordViewModel.Owner), "Updated owner"), Is.True);
            Assert.That(grid.EditSessionContext.TrySetFieldValue(row2.ObjectId, nameof(DemoGisRecordViewModel.Status), "UnderMaintenance"), Is.True);
            Assert.That(grid.EditSessionContext.TrySetFieldValue(row2.ObjectId, nameof(DemoGisRecordViewModel.Owner), "Field team"), Is.True);

            Assert.That(grid.HasPendingEdits, Is.True);
            Assert.That(grid.PendingEditCount, Is.EqualTo(2));
            Assert.That(grid.PendingEditBannerText, Does.Contain("Unsaved edits detected"));

            grid.CancelEdits();

            Assert.Multiple(() =>
            {
                Assert.That(row1.ObjectName, Is.EqualTo("Valve A"));
                Assert.That(row1.Owner, Is.EqualTo("Operations"));
                Assert.That(row2.Status, Is.EqualTo("Active"));
                Assert.That(row2.Owner, Is.EqualTo("Infrastructure"));
                Assert.That(notifications, Does.Contain("r1:Item[]"));
                Assert.That(notifications, Does.Contain("r2:Item[]"));
                Assert.That(rowModel1[nameof(DemoGisRecordViewModel.ObjectName)], Is.EqualTo("Valve A"));
                Assert.That(rowModel2[nameof(DemoGisRecordViewModel.Owner)], Is.EqualTo("Infrastructure"));
                Assert.That(grid.HasPendingEdits, Is.False);
                Assert.That(grid.PendingEditCount, Is.EqualTo(0));
            });
        }

        [Test]
        public void CommitEdits_WhenAnyChangedRowIsInvalid_RemainsAtomicAndKeepsAllPendingChanges()
        {
            var grid = new WpfGrid();
            grid.LanguageDirectory = GetLanguageDirectory();
            var row1 = CreateRow("GIS-1", "Valve A", "Operations");
            var row2 = CreateRow("GIS-2", "Valve B", "Infrastructure");
            grid.ItemsSource = new[] { row1, row2 };

            Assert.That(grid.EditSessionContext.TrySetFieldValue(row1.ObjectId, nameof(DemoGisRecordViewModel.Owner), "Updated owner"), Is.True);
            Assert.That(grid.EditSessionContext.TrySetFieldValue(row2.ObjectId, nameof(DemoGisRecordViewModel.Owner), string.Empty), Is.True);

            Assert.That(grid.PendingEditCount, Is.EqualTo(2));

            grid.CommitEdits();

            Assert.Multiple(() =>
            {
                Assert.That(row1.Owner, Is.EqualTo("Updated owner"));
                Assert.That(row2.Owner, Is.EqualTo(string.Empty));
                Assert.That(grid.HasPendingEdits, Is.True);
                Assert.That(grid.PendingEditCount, Is.EqualTo(2));
                Assert.That(grid.HasValidationIssues, Is.True);
                Assert.That(grid.PendingEditBannerText, Does.Contain("validation").IgnoreCase);
            });
        }

        [Test]
        public void ChangePanelItems_WhenRowsHavePendingEdits_ExposeSummaryCardsAndNavigationTargets()
        {
            var grid = new WpfGrid
            {
                LanguageDirectory = GetLanguageDirectory(),
                Width = 900,
                Height = 600,
            };
            var row1 = CreateRow("GIS-1", "Valve A", "Operations");
            var row2 = CreateRow("GIS-2", "Valve B", "Infrastructure");
            grid.ItemsSource = new[] { row1, row2 };
            var window = GridSurfaceTestHost.CreateHostWindow(grid, width: 900, height: 600);

            try
            {
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(grid);

                Assert.That(grid.EditSessionContext.TrySetFieldValue(row2.ObjectId, nameof(DemoGisRecordViewModel.Owner), "Field team"), Is.True);
                GridSurfaceTestHost.FlushDispatcher(grid);

                var item = grid.ChangePanelItems.Single();
                Assert.Multiple(() =>
                {
                    Assert.That(grid.ChangeSummaryText, Is.EqualTo("1 changed row"));
                    Assert.That(item.RowId, Is.EqualTo(row2.ObjectId));
                    Assert.That(item.NavigationColumnId, Is.EqualTo("ObjectName"));
                    Assert.That(item.Title, Is.EqualTo("Row 2"));
                    Assert.That(item.Description, Is.EqualTo("Valve B"));
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void ChangePanel_WhenHostedAsWorkspaceContent_RendersSummaryAndCardText()
        {
            var grid = new WpfGrid
            {
                LanguageDirectory = GetLanguageDirectory(),
                Width = 900,
                Height = 600,
                ChangePanelContent = new PhialeChangePanel(),
            };
            var row = CreateRow("GIS-2", "Valve B", "Infrastructure");
            grid.ItemsSource = new[] { row };
            var window = GridSurfaceTestHost.CreateHostWindow(grid, width: 900, height: 600);

            try
            {
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(grid);

                Assert.That(grid.EditSessionContext.TrySetFieldValue(row.ObjectId, nameof(DemoGisRecordViewModel.Owner), "Field team"), Is.True);
                grid.OpenWorkspacePanel(GridRegionKind.ChangePanelRegion);
                GridSurfaceTestHost.FlushDispatcher(grid);

                var visibleTexts = GridSurfaceTestHost.FindVisualChildren<TextBlock>(grid)
                    .Select(block => block.Text)
                    .Where(text => !string.IsNullOrWhiteSpace(text))
                    .ToArray();

                Assert.Multiple(() =>
                {
                    Assert.That(visibleTexts, Does.Contain("1 changed row"));
                    Assert.That(visibleTexts, Does.Contain("Row 1"));
                    Assert.That(visibleTexts, Does.Contain("Valve B"));
                    Assert.That(GridSurfaceTestHost.FindVisualChildren<Button>(grid).Any(button => Equals(button.Content, "Go to row")), Is.True);
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void ChangePanelFilter_WhenToggledByButton_FiltersRowsAndClearsWhenPanelIsHidden()
        {
            var grid = new WpfGrid
            {
                LanguageDirectory = GetLanguageDirectory(),
                Width = 900,
                Height = 600,
                ChangePanelContent = new PhialeChangePanel(),
                ValidationPanelContent = new PhialeValidationPanel(),
            };
            var row1 = CreateRow("GIS-1", "Valve A", "Operations");
            var row2 = CreateRow("GIS-2", "Valve B", "Infrastructure");
            var row3 = CreateRow("GIS-3", "Valve C", "Field");
            grid.ItemsSource = new[] { row1, row2, row3 };
            var window = GridSurfaceTestHost.CreateHostWindow(grid, width: 900, height: 600);

            try
            {
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(grid);

                Assert.That(grid.EditSessionContext.TrySetFieldValue(row1.ObjectId, nameof(DemoGisRecordViewModel.Owner), "Field team"), Is.True);
                Assert.That(grid.EditSessionContext.TrySetFieldValue(row3.ObjectId, nameof(DemoGisRecordViewModel.Owner), "Depot"), Is.True);
                grid.OpenWorkspacePanel(GridRegionKind.ChangePanelRegion);
                GridSurfaceTestHost.FlushDispatcher(grid);

                var toggleButton = GridSurfaceTestHost.FindVisualChildren<Button>(grid)
                    .FirstOrDefault(button => Equals(button.Content, "Show only changed rows"));
                Assert.That(toggleButton, Is.Not.Null);

                toggleButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                GridSurfaceTestHost.FlushDispatcher(grid);

                Assert.Multiple(() =>
                {
                    Assert.That(grid.IsChangePanelChangedRowsFilterActive, Is.True);
                    Assert.That(CountVisibleDataRows(grid), Is.EqualTo(2));
                    Assert.That(GridSurfaceTestHost.FindVisualChildren<Button>(grid).Any(button => Equals(button.Content, "Show all rows")), Is.True);
                });

                grid.OpenWorkspacePanel(GridRegionKind.ValidationPanelRegion);
                GridSurfaceTestHost.FlushDispatcher(grid);

                Assert.Multiple(() =>
                {
                    Assert.That(grid.IsChangePanelChangedRowsFilterActive, Is.False);
                    Assert.That(CountVisibleDataRows(grid), Is.EqualTo(3));
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void ChangePanelFilter_WhenChangePanelIsClosed_RestoresAllRows()
        {
            var grid = new WpfGrid
            {
                LanguageDirectory = GetLanguageDirectory(),
                Width = 900,
                Height = 600,
                ChangePanelContent = new PhialeChangePanel(),
            };
            var row1 = CreateRow("GIS-1", "Valve A", "Operations");
            var row2 = CreateRow("GIS-2", "Valve B", "Infrastructure");
            var row3 = CreateRow("GIS-3", "Valve C", "Field");
            grid.ItemsSource = new[] { row1, row2, row3 };
            var window = GridSurfaceTestHost.CreateHostWindow(grid, width: 900, height: 600);

            try
            {
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(grid);

                Assert.That(grid.EditSessionContext.TrySetFieldValue(row1.ObjectId, nameof(DemoGisRecordViewModel.Owner), "Field team"), Is.True);
                Assert.That(grid.EditSessionContext.TrySetFieldValue(row3.ObjectId, nameof(DemoGisRecordViewModel.Owner), "Depot"), Is.True);
                grid.OpenWorkspacePanel(GridRegionKind.ChangePanelRegion);
                GridSurfaceTestHost.FlushDispatcher(grid);

                var toggleButton = GridSurfaceTestHost.FindVisualChildren<Button>(grid)
                    .FirstOrDefault(button => Equals(button.Content, "Show only changed rows"));
                Assert.That(toggleButton, Is.Not.Null);
                toggleButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                GridSurfaceTestHost.FlushDispatcher(grid);
                Assert.That(CountVisibleDataRows(grid), Is.EqualTo(2));

                grid.SetRegionVisibility(GridRegionKind.ChangePanelRegion, false);
                GridSurfaceTestHost.FlushDispatcher(grid);

                Assert.Multiple(() =>
                {
                    Assert.That(grid.IsChangePanelChangedRowsFilterActive, Is.False);
                    Assert.That(CountVisibleDataRows(grid), Is.EqualTo(3));
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void ChangePanelFilter_WhenShowAllRowsIsClicked_RestoresAllRowsAndLeavesViewportAtTop()
        {
            var grid = new WpfGrid
            {
                LanguageDirectory = GetLanguageDirectory(),
                Width = 900,
                Height = 600,
                ChangePanelContent = new PhialeChangePanel(),
            };
            var rows = Enumerable.Range(1, 12)
                .Select(index => CreateRow("GIS-" + index.ToString(CultureInfo.InvariantCulture), "Valve " + index.ToString(CultureInfo.InvariantCulture), "Operations"))
                .ToArray();
            grid.ItemsSource = rows;
            var window = GridSurfaceTestHost.CreateHostWindow(grid, width: 900, height: 600);

            try
            {
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(grid);

                Assert.That(grid.EditSessionContext.TrySetFieldValue(rows[4].ObjectId, nameof(DemoGisRecordViewModel.Owner), "Field team"), Is.True);
                Assert.That(grid.EditSessionContext.TrySetFieldValue(rows[9].ObjectId, nameof(DemoGisRecordViewModel.Owner), "Depot"), Is.True);
                grid.OpenWorkspacePanel(GridRegionKind.ChangePanelRegion);
                GridSurfaceTestHost.FlushDispatcher(grid);

                var showOnlyButton = GridSurfaceTestHost.FindVisualChildren<Button>(grid)
                    .FirstOrDefault(button => Equals(button.Content, "Show only changed rows"));
                Assert.That(showOnlyButton, Is.Not.Null);
                showOnlyButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                GridSurfaceTestHost.FlushDispatcher(grid);

                Assert.Multiple(() =>
                {
                    Assert.That(grid.IsChangePanelChangedRowsFilterActive, Is.True);
                    Assert.That(CountVisibleDataRows(grid), Is.EqualTo(2));
                });

                var showAllButton = GridSurfaceTestHost.FindVisualChildren<Button>(grid)
                    .FirstOrDefault(button => Equals(button.Content, "Show all rows"));
                Assert.That(showAllButton, Is.Not.Null);
                showAllButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                GridSurfaceTestHost.FlushDispatcher(grid);

                var surfaceHost = GridSurfaceTestHost.FindVisualChildren<GridSurfaceHost>(grid).First();
                Assert.Multiple(() =>
                {
                    Assert.That(grid.IsChangePanelChangedRowsFilterActive, Is.False);
                    Assert.That(grid.ChangePanelRowsFilterToggleText, Is.EqualTo("Show only changed rows"));
                    Assert.That(CountVisibleDataRows(grid), Is.EqualTo(rows.Length));
                    Assert.That(surfaceHost.CurrentSnapshot.ViewportState.VerticalOffset, Is.EqualTo(0d));
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void ValidationIssueItems_WhenCommitFindsInvalidCell_ExposePanelCardDetailsAndNavigationTarget()
        {
            var grid = new WpfGrid();
            grid.LanguageDirectory = GetLanguageDirectory();
            var row = CreateRow("GIS-2", "Valve B", "Infrastructure");
            grid.ItemsSource = new[] { row };

            Assert.That(grid.EditSessionContext.TrySetFieldValue(row.ObjectId, nameof(DemoGisRecordViewModel.Owner), string.Empty), Is.True);

            grid.CommitEdits();

            var item = grid.ValidationIssueItems.Single();
            Assert.Multiple(() =>
            {
                Assert.That(item.RowId, Is.EqualTo(row.ObjectId));
                Assert.That(item.ColumnId, Is.EqualTo(nameof(DemoGisRecordViewModel.Owner)));
                Assert.That(item.ColumnDisplayName, Is.EqualTo("Owner"));
                Assert.That(item.Message, Is.EqualTo("Owner is required."));
                Assert.That(item.Title, Is.EqualTo("Row GIS-2 - Owner"));
            });
        }

        [Test]
        public void ValidationIssueCount_WhenInvalidCellIsVisible_CountsPanelIssueOnce()
        {
            const string invalidRowId = "BLD-WRO-FAB-0002";
            var viewModel = new DemoShellViewModel("Wpf");
            var grid = CreateBoundDemoGrid(viewModel);
            var window = GridSurfaceTestHost.CreateHostWindow(grid, width: 1400, height: 760);

            try
            {
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(grid);

                viewModel.SelectExample("editing");
                GridSurfaceTestHost.FlushDispatcher(grid);

                Assert.That(grid.SetRowValueForDemo(invalidRowId, "ObjectName", "Building Fabryczna 2 (edited)"), Is.True);
                Assert.That(grid.SetRowValueForDemo(invalidRowId, "Owner", string.Empty), Is.True);
                Assert.That(grid.FocusRow(invalidRowId, "Owner"), Is.True);
                GridSurfaceTestHost.FlushDispatcher(grid);

                var surfaceHost = GridSurfaceTestHost.FindSurfaceHost(grid);
                var invalidCell = surfaceHost.CurrentSnapshot.Cells.First(cell =>
                    string.Equals(cell.RowKey, invalidRowId, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(cell.ColumnKey, "Owner", StringComparison.OrdinalIgnoreCase));

                Assert.Multiple(() =>
                {
                    Assert.That(invalidCell.HasValidationError, Is.True);
                    Assert.That(grid.ValidationIssueItems, Has.Count.EqualTo(1));
                    Assert.That(grid.ValidationIssueCount, Is.EqualTo(grid.ValidationIssueItems.Count));
                    Assert.That(grid.ValidationIssueSummaryText, Is.EqualTo("1 validation issue"));
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void ValidationPanel_WhenHostedAsWorkspaceContent_RendersIssueSummaryAndCardText()
        {
            var grid = new WpfGrid
            {
                LanguageDirectory = GetLanguageDirectory(),
                Width = 900,
                Height = 600,
                ValidationPanelContent = new PhialeValidationPanel(),
            };
            var row = CreateRow("GIS-2", "Valve B", "Infrastructure");
            grid.ItemsSource = new[] { row };
            var window = GridSurfaceTestHost.CreateHostWindow(grid, width: 900, height: 600);

            try
            {
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(grid);

                Assert.That(grid.EditSessionContext.TrySetFieldValue(row.ObjectId, nameof(DemoGisRecordViewModel.Owner), string.Empty), Is.True);
                grid.CommitEdits();
                grid.OpenWorkspacePanel(GridRegionKind.ValidationPanelRegion);
                GridSurfaceTestHost.FlushDispatcher(grid);

                var visibleTexts = GridSurfaceTestHost.FindVisualChildren<TextBlock>(grid)
                    .Select(block => block.Text)
                    .Where(text => !string.IsNullOrWhiteSpace(text))
                    .ToArray();

                Assert.Multiple(() =>
                {
                    Assert.That(visibleTexts, Does.Contain("1 validation issue"));
                    Assert.That(visibleTexts, Does.Contain("Row GIS-2 - Owner"));
                    Assert.That(visibleTexts, Does.Contain("Owner is required."));
                    Assert.That(GridSurfaceTestHost.FindVisualChildren<Button>(grid).Any(button => Equals(button.Content, "Go to cell")), Is.True);
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void SettingGroups_AfterLoad_RendersGroupHeaderRowsInRowsViewAndSurface()
        {
            var grid = CreateInlineGroupingGrid();
            var window = GridSurfaceTestHost.CreateHostWindow(grid, width: 900, height: 600);

            try
            {
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(grid);

                var initialRows = grid.RowsView.Cast<object>().ToArray();
                Assert.That(initialRows.Any(row => row is GridDataRowModel), Is.True);

                grid.SetGroups(new[] { new GridGroupDescriptor("Category", GridSortDirection.Ascending) });
                GridSurfaceTestHost.FlushDispatcher(grid);

                var rows = grid.RowsView.Cast<object>().Take(2).ToArray();
                var surfaceHost = GridSurfaceTestHost.FindSurfaceHost(grid);

                Assert.That(rows.Length, Is.EqualTo(2));
                Assert.That(rows.All(row => row is GridGroupHeaderRowModel), Is.True);
                Assert.That(surfaceHost.CurrentSnapshot.Rows.Take(2).All(row => row.IsGroupHeader), Is.True);
                Assert.That(((GridGroupHeaderRowModel)rows[0]).GroupColumnId, Is.EqualTo("Category"));
                Assert.That(grid.HasGroups, Is.True);
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void SettingGroups_BeforeLoad_RendersGroupHeaderRowsInRowsViewAndSurface()
        {
            var grid = CreateInlineGroupingGrid();
            grid.Groups = new[] { new GridGroupDescriptor("Category", GridSortDirection.Ascending) };
            var window = GridSurfaceTestHost.CreateHostWindow(grid, width: 900, height: 600);

            try
            {
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(grid);

                var rows = grid.RowsView.Cast<object>().Take(2).ToArray();
                var surfaceHost = GridSurfaceTestHost.FindSurfaceHost(grid);

                Assert.That(rows.Length, Is.EqualTo(2));
                Assert.That(rows.All(row => row is GridGroupHeaderRowModel), Is.True);
                Assert.That(surfaceHost.CurrentSnapshot.Rows.Take(2).All(row => row.IsGroupHeader), Is.True);
                Assert.That(grid.HasGroups, Is.True);
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void DemoBinding_WhenGroupingScenarioIsSelected_RendersGroupHeaderRows()
        {
            var viewModel = new DemoShellViewModel("Wpf");
            var grid = CreateBoundDemoGrid(viewModel);
            var window = GridSurfaceTestHost.CreateHostWindow(grid, width: 1200, height: 700);

            try
            {
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(grid);

                viewModel.SelectExample("grouping");
                GridSurfaceTestHost.FlushDispatcher(grid);

                var rows = grid.RowsView.Cast<object>().Take(3).ToArray();
                var surfaceHost = GridSurfaceTestHost.FindSurfaceHost(grid);

                Assert.That(viewModel.GridGroups.Count, Is.EqualTo(1));
                Assert.That(grid.HasGroups, Is.True);
                Assert.That(rows.Length, Is.GreaterThan(0));
                Assert.That(rows[0], Is.InstanceOf<GridGroupHeaderRowModel>());
                Assert.That(surfaceHost.CurrentSnapshot.Rows[0].IsGroupHeader, Is.True);
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void DemoBinding_WhenEditingScenarioIsSelected_DoubleClickStartsSurfaceEditAndCreatesTextBoxEditor()
        {
            var viewModel = new DemoShellViewModel("Wpf");
            var grid = CreateBoundDemoGrid(viewModel);
            var window = GridSurfaceTestHost.CreateHostWindow(grid, width: 1200, height: 700);

            try
            {
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(grid);

                viewModel.SelectExample("editing");
                GridSurfaceTestHost.FlushDispatcher(grid);

                var surfaceHost = GridSurfaceTestHost.FindSurfaceHost(grid);
                var cell = FindEditableCell(surfaceHost, "ObjectName");

                GridSurfaceTestHost.DoubleClickPoint(surfaceHost, cell.Bounds.X + 10d, cell.Bounds.Y + (cell.Bounds.Height / 2d));
                GridSurfaceTestHost.FlushDispatcher(grid);

                var editingPresenter = GridSurfaceTestHost.FindVisualChildren<GridCellPresenter>(surfaceHost)
                    .FirstOrDefault(presenter => presenter.CellData?.RowKey == cell.RowKey && presenter.CellData.ColumnKey == "ObjectName");

                Assert.Multiple(() =>
                {
                    Assert.That(grid.IsGridReadOnly, Is.False);
                    Assert.That(surfaceHost.CurrentSnapshot.ViewportState.IsInEditMode, Is.True);
                    Assert.That(surfaceHost.CurrentSnapshot.Cells.Single(item => item.RowKey == cell.RowKey && item.ColumnKey == "ObjectName").IsEditing, Is.True);
                    Assert.That(editingPresenter, Is.Not.Null);
                    Assert.That(editingPresenter.Content, Is.InstanceOf<Border>());
                    Assert.That(((Border)editingPresenter.Content).Child, Is.InstanceOf<TextBox>());
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void DemoBinding_WhenEditingScenarioCommitsValue_PendingStateIsTrackedAndToolbarCommitPersistsChange()
        {
            var viewModel = new DemoShellViewModel("Wpf");
            var grid = CreateBoundDemoGrid(viewModel);
            var window = GridSurfaceTestHost.CreateHostWindow(grid, width: 1200, height: 700);

            try
            {
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(grid);

                viewModel.SelectExample("editing");
                GridSurfaceTestHost.FlushDispatcher(grid);

                var surfaceHost = GridSurfaceTestHost.FindSurfaceHost(grid);
                var rowModel = grid.RowsView.Cast<GridDataRowModel>().First();
                var sourceRow = (DemoGisRecordViewModel)rowModel.SourceRow;
                var originalValue = sourceRow.ObjectName;
                var editedValue = originalValue + " :: edited";
                var cell = FindEditableCell(surfaceHost, "ObjectName");

                GridSurfaceTestHost.DoubleClickPoint(surfaceHost, cell.Bounds.X + 10d, cell.Bounds.Y + (cell.Bounds.Height / 2d));
                AppendText(surfaceHost, editedValue.Substring(originalValue.Length));
                GridSurfaceTestHost.SendKey(surfaceHost, "ENTER");
                GridSurfaceTestHost.FlushDispatcher(grid);

                Assert.Multiple(() =>
                {
                    Assert.That(sourceRow.ObjectName, Is.EqualTo(editedValue));
                    Assert.That(rowModel["ObjectName"], Is.EqualTo(editedValue));
                    Assert.That(grid.HasPendingEdits, Is.True);
                    Assert.That(grid.PendingEditCount, Is.EqualTo(1));
                    Assert.That(grid.HasValidationIssues, Is.False);
                });

                grid.CommitEdits();
                GridSurfaceTestHost.FlushDispatcher(grid);

                Assert.Multiple(() =>
                {
                    Assert.That(sourceRow.ObjectName, Is.EqualTo(editedValue));
                    Assert.That(rowModel["ObjectName"], Is.EqualTo(editedValue));
                    Assert.That(grid.HasPendingEdits, Is.False);
                    Assert.That(grid.PendingEditCount, Is.EqualTo(0));
                    Assert.That(grid.HasValidationIssues, Is.False);
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void DemoBinding_WhenEditingScenarioAndTabIsPressed_MovesToNextEditableCell()
        {
            var viewModel = new DemoShellViewModel("Wpf");
            var grid = CreateBoundDemoGrid(viewModel);
            var window = GridSurfaceTestHost.CreateHostWindow(grid, width: 1400, height: 760);

            try
            {
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(grid);

                viewModel.SelectExample("editing");
                GridSurfaceTestHost.FlushDispatcher(grid);

                var surfaceHost = GridSurfaceTestHost.FindSurfaceHost(grid);
                var objectNameCell = FindEditableCell(surfaceHost, "ObjectName");

                GridSurfaceTestHost.DoubleClickPoint(surfaceHost, objectNameCell.Bounds.X + 10d, objectNameCell.Bounds.Y + (objectNameCell.Bounds.Height / 2d));
                GridSurfaceTestHost.FlushDispatcher(grid);
                GridSurfaceTestHost.SendKey(surfaceHost, "TAB");
                GridSurfaceTestHost.FlushDispatcher(grid);

                Assert.Multiple(() =>
                {
                    Assert.That(surfaceHost.CurrentSnapshot.ViewportState.IsInEditMode, Is.True);
                    Assert.That(surfaceHost.CurrentSnapshot.CurrentCell.ColumnKey, Is.EqualTo("Status"));
                    Assert.That(surfaceHost.CurrentSnapshot.Cells.Single(item => item.RowKey == objectNameCell.RowKey && item.ColumnKey == "Status").IsEditing, Is.True);
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void DemoBinding_WhenRichEditorsScenarioIsSelected_ShouldRenderComboDateAndMaskedEditors()
        {
            var viewModel = new DemoShellViewModel("Wpf");
            var grid = CreateBoundDemoGrid(viewModel);
            var window = GridSurfaceTestHost.CreateHostWindow(grid, width: 1400, height: 760);

            try
            {
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(grid);

                viewModel.SelectExample("rich-editors");
                GridSurfaceTestHost.FlushDispatcher(grid);

                var surfaceHost = GridSurfaceTestHost.FindSurfaceHost(grid);

                var statusCell = FindEditableCell(surfaceHost, "Status");
                GridSurfaceTestHost.DoubleClickPoint(surfaceHost, statusCell.Bounds.X + 10d, statusCell.Bounds.Y + (statusCell.Bounds.Height / 2d));
                GridSurfaceTestHost.FlushDispatcher(grid);

                var statusPresenter = GridSurfaceTestHost.FindVisualChildren<GridCellPresenter>(surfaceHost)
                    .FirstOrDefault(presenter => presenter.CellData?.RowKey == statusCell.RowKey && presenter.CellData.ColumnKey == "Status");
                Assert.That(statusPresenter?.Content, Is.InstanceOf<Border>());
                Assert.That((statusPresenter?.Content as Border)?.Child, Is.InstanceOf<ComboBox>());

                grid.CancelEdits();
                grid.ScrollColumnIntoView("LastInspection", GridScrollAlignment.End);
                GridSurfaceTestHost.FlushDispatcher(grid);

                var inspectionCell = FindEditableCell(surfaceHost, "LastInspection");
                GridSurfaceTestHost.DoubleClickPoint(surfaceHost, inspectionCell.Bounds.X + 10d, inspectionCell.Bounds.Y + (inspectionCell.Bounds.Height / 2d));
                GridSurfaceTestHost.FlushDispatcher(grid);

                var inspectionPresenter = GridSurfaceTestHost.FindVisualChildren<GridCellPresenter>(surfaceHost)
                    .FirstOrDefault(presenter => presenter.CellData?.RowKey == inspectionCell.RowKey && presenter.CellData.ColumnKey == "LastInspection");
                Assert.That(inspectionPresenter?.Content, Is.InstanceOf<Border>());
                Assert.That((inspectionPresenter?.Content as Border)?.Child, Is.InstanceOf<DatePicker>());

                grid.CancelEdits();
                grid.ScrollColumnIntoView("ScaleHint", GridScrollAlignment.End);
                GridSurfaceTestHost.FlushDispatcher(grid);

                var scaleCell = FindEditableCell(surfaceHost, "ScaleHint");
                GridSurfaceTestHost.DoubleClickPoint(surfaceHost, scaleCell.Bounds.X + 10d, scaleCell.Bounds.Y + (scaleCell.Bounds.Height / 2d));
                GridSurfaceTestHost.FlushDispatcher(grid);

                var scalePresenter = GridSurfaceTestHost.FindVisualChildren<GridCellPresenter>(surfaceHost)
                    .FirstOrDefault(presenter => presenter.CellData?.RowKey == scaleCell.RowKey && presenter.CellData.ColumnKey == "ScaleHint");

                Assert.Multiple(() =>
                {
                    Assert.That(grid.VisibleColumns.Any(column => column.ColumnId == "UpdatedAt"), Is.True);
                    Assert.That(grid.VisibleColumns.Any(column => column.ColumnId == "MaintenanceBudget"), Is.True);
                    Assert.That(grid.VisibleColumns.Any(column => column.ColumnId == "CompletionPercent"), Is.True);
                    Assert.That(grid.VisibleColumns.Any(column => column.ColumnId == "Visible"), Is.True);
                    Assert.That(grid.VisibleColumns.Any(column => column.ColumnId == "EditableFlag"), Is.True);
                    Assert.That(grid.VisibleColumns.Any(column => column.ColumnId == "ScaleHint"), Is.True);
                    Assert.That(scalePresenter?.Content, Is.InstanceOf<Border>());
                    Assert.That((scalePresenter?.Content as Border)?.Child, Is.InstanceOf<TextBox>());
                });

                grid.CancelEdits();
                grid.ScrollColumnIntoView("Visible", GridScrollAlignment.End);
                GridSurfaceTestHost.FlushDispatcher(grid);

                var visiblePresenter = GridSurfaceTestHost.FindVisualChildren<GridCellPresenter>(surfaceHost)
                    .FirstOrDefault(presenter => presenter.CellData?.ColumnKey == "Visible");

                Assert.That(visiblePresenter?.Content, Is.InstanceOf<CheckBox>());
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void DemoBinding_WhenRichEditorsBooleanCellClicked_TogglesValueAndMarksPendingEdit()
        {
            var viewModel = new DemoShellViewModel("Wpf");
            var grid = CreateBoundDemoGrid(viewModel);
            var window = GridSurfaceTestHost.CreateHostWindow(grid, width: 1400, height: 760);

            try
            {
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(grid);

                viewModel.SelectExample("rich-editors");
                GridSurfaceTestHost.FlushDispatcher(grid);

                var surfaceHost = GridSurfaceTestHost.FindSurfaceHost(grid);
                var booleanCell = FindEditableCell(surfaceHost, "Visible");
                var row = viewModel.GridRecords.First(record => string.Equals(record.ObjectId, booleanCell.RowKey, StringComparison.OrdinalIgnoreCase));
                var before = row.Visible;

                GridSurfaceTestHost.DoubleClickPoint(surfaceHost, booleanCell.Bounds.X + (booleanCell.Bounds.Width / 2d), booleanCell.Bounds.Y + (booleanCell.Bounds.Height / 2d));
                GridSurfaceTestHost.FlushDispatcher(grid);

                var presenter = GridSurfaceTestHost.FindVisualChildren<GridCellPresenter>(surfaceHost)
                    .FirstOrDefault(candidate => candidate.CellData?.RowKey == booleanCell.RowKey && candidate.CellData.ColumnKey == "Visible");
                var editor = (presenter?.Content as Border)?.Child as CheckBox;
                Assert.That(editor, Is.Not.Null);
                editor.IsChecked = !before;
                GridSurfaceTestHost.SendKey(surfaceHost, "ENTER");
                GridSurfaceTestHost.FlushDispatcher(grid);

                Assert.Multiple(() =>
                {
                    Assert.That(row.Visible, Is.EqualTo(!before));
                    Assert.That(grid.PendingEditCount, Is.GreaterThanOrEqualTo(1));
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void DemoBinding_WhenRichEditorsStatusSelectionChanges_CommitsSelectedComboValue()
        {
            var viewModel = new DemoShellViewModel("Wpf");
            var grid = CreateBoundDemoGrid(viewModel);
            var window = GridSurfaceTestHost.CreateHostWindow(grid, width: 1400, height: 760);

            try
            {
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(grid);

                viewModel.SelectExample("rich-editors");
                GridSurfaceTestHost.FlushDispatcher(grid);

                var surfaceHost = GridSurfaceTestHost.FindSurfaceHost(grid);
                var statusCell = FindEditableCell(surfaceHost, "Status");
                var row = viewModel.GridRecords.First(record => string.Equals(record.ObjectId, statusCell.RowKey, StringComparison.OrdinalIgnoreCase));

                GridSurfaceTestHost.DoubleClickPoint(surfaceHost, statusCell.Bounds.X + 10d, statusCell.Bounds.Y + (statusCell.Bounds.Height / 2d));
                GridSurfaceTestHost.FlushDispatcher(grid);

                var presenter = GridSurfaceTestHost.FindVisualChildren<GridCellPresenter>(surfaceHost)
                    .FirstOrDefault(candidate => candidate.CellData?.RowKey == statusCell.RowKey && candidate.CellData.ColumnKey == "Status");
                var editor = (presenter?.Content as Border)?.Child as ComboBox;
                Assert.That(editor, Is.Not.Null);

                editor!.SelectedItem = "Retired";
                GridSurfaceTestHost.SendKey(surfaceHost, "ENTER");
                GridSurfaceTestHost.FlushDispatcher(grid);

                Assert.Multiple(() =>
                {
                    Assert.That(row.Status, Is.EqualTo("Retired"));
                    Assert.That(grid.PendingEditCount, Is.GreaterThanOrEqualTo(1));
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void DemoBinding_WhenEditingOwnerToggleIsClicked_OpensAutocompletePopupOnFirstAttempt()
        {
            var viewModel = new DemoShellViewModel("Wpf");
            var grid = CreateBoundDemoGrid(viewModel);
            var window = GridSurfaceTestHost.CreateHostWindow(grid, width: 1400, height: 760);

            try
            {
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(grid);

                viewModel.SelectExample("editing");
                GridSurfaceTestHost.FlushDispatcher(grid);

                var surfaceHost = GridSurfaceTestHost.FindSurfaceHost(grid);
                grid.ScrollColumnIntoView("Owner", GridScrollAlignment.End);
                GridSurfaceTestHost.FlushDispatcher(grid);

                var ownerCell = FindEditableCell(surfaceHost, "Owner");
                GridSurfaceTestHost.DoubleClickPoint(surfaceHost, ownerCell.Bounds.X + 10d, ownerCell.Bounds.Y + (ownerCell.Bounds.Height / 2d));
                GridSurfaceTestHost.FlushDispatcher(grid);

                var presenter = GridSurfaceTestHost.FindVisualChildren<GridCellPresenter>(surfaceHost)
                    .FirstOrDefault(candidate => candidate.CellData?.RowKey == ownerCell.RowKey && candidate.CellData.ColumnKey == "Owner");
                var editor = (presenter?.Content as Border)?.Child as ComboBox;
                Assert.That(editor, Is.Not.Null);

                editor!.ApplyTemplate();
                var toggle = editor.Template?.FindName("DropDownToggle", editor) as ToggleButton;
                var popup = editor.Template?.FindName("PART_Popup", editor) as Popup;

                Assert.That(toggle, Is.Not.Null);
                Assert.That(popup, Is.Not.Null);

                ClickElement(toggle!);
                GridSurfaceTestHost.FlushDispatcher(grid);

                Assert.Multiple(() =>
                {
                    Assert.That(editor.IsDropDownOpen, Is.True, "Owner autocomplete should open immediately after the first toggle click.");
                    Assert.That(popup!.IsOpen, Is.True, "Owner autocomplete popup should become visible after the first toggle click.");
                    Assert.That(editor.ItemContainerGenerator.ContainerFromItem("Municipality"), Is.Not.Null, "Owner autocomplete should materialize its suggestion items when opened.");
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void DemoBinding_WhenEditingOwnerToggleSlotIsClickedAwayFromChevron_StillOpensAutocompletePopup()
        {
            var viewModel = new DemoShellViewModel("Wpf");
            var grid = CreateBoundDemoGrid(viewModel);
            var window = GridSurfaceTestHost.CreateHostWindow(grid, width: 1400, height: 760);

            try
            {
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(grid);

                viewModel.SelectExample("editing");
                GridSurfaceTestHost.FlushDispatcher(grid);

                var surfaceHost = GridSurfaceTestHost.FindSurfaceHost(grid);
                grid.ScrollColumnIntoView("Owner", GridScrollAlignment.End);
                GridSurfaceTestHost.FlushDispatcher(grid);

                var ownerCell = FindEditableCell(surfaceHost, "Owner");
                GridSurfaceTestHost.DoubleClickPoint(surfaceHost, ownerCell.Bounds.X + 10d, ownerCell.Bounds.Y + (ownerCell.Bounds.Height / 2d));
                GridSurfaceTestHost.FlushDispatcher(grid);

                var presenter = GridSurfaceTestHost.FindVisualChildren<GridCellPresenter>(surfaceHost)
                    .FirstOrDefault(candidate => candidate.CellData?.RowKey == ownerCell.RowKey && candidate.CellData.ColumnKey == "Owner");
                var editor = (presenter?.Content as Border)?.Child as ComboBox;
                Assert.That(editor, Is.Not.Null);

                editor!.ApplyTemplate();
                var toggle = editor.Template?.FindName("DropDownToggle", editor) as ToggleButton;
                var popup = editor.Template?.FindName("PART_Popup", editor) as Popup;

                Assert.That(toggle, Is.Not.Null);
                Assert.That(popup, Is.Not.Null);

                editor.UpdateLayout();
                toggle!.UpdateLayout();

                var hitPoint = toggle.TranslatePoint(new Point(2d, Math.Max(1d, toggle.ActualHeight / 2d)), editor);
                var hitElement = ResolveHitTestUiElement(editor, hitPoint);

                Assert.That(hitElement, Is.Not.Null, "Owner autocomplete should expose a clickable hit target across the whole toggle slot.");
                Assert.That(IsDescendantOf(hitElement, toggle), Is.True, "Owner autocomplete should hit-test the toggle slot itself, not only the chevron path.");

                ClickElement(toggle);
                GridSurfaceTestHost.FlushDispatcher(grid);

                Assert.Multiple(() =>
                {
                    Assert.That(editor.IsDropDownOpen, Is.True, "Owner autocomplete should open from a click inside the toggle slot, not only on the chevron.");
                    Assert.That(popup!.IsOpen, Is.True, "Owner autocomplete popup should become visible after clicking inside the wider toggle slot.");
                    Assert.That(editor.ItemContainerGenerator.ContainerFromItem("Municipality"), Is.Not.Null, "Owner autocomplete should materialize items after a slot click away from the chevron.");
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void DemoBinding_WhenRichEditorsOwnerSelectionChanges_CommitsSelectedAutocompleteValue()
        {
            var viewModel = new DemoShellViewModel("Wpf");
            var grid = CreateBoundDemoGrid(viewModel);
            var window = GridSurfaceTestHost.CreateHostWindow(grid, width: 1400, height: 760);

            try
            {
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(grid);

                viewModel.SelectExample("rich-editors");
                GridSurfaceTestHost.FlushDispatcher(grid);

                var surfaceHost = GridSurfaceTestHost.FindSurfaceHost(grid);
                grid.ScrollColumnIntoView("Owner", GridScrollAlignment.End);
                GridSurfaceTestHost.FlushDispatcher(grid);

                var ownerCell = FindEditableCell(surfaceHost, "Owner");
                var row = viewModel.GridRecords.First(record => string.Equals(record.ObjectId, ownerCell.RowKey, StringComparison.OrdinalIgnoreCase));

                GridSurfaceTestHost.DoubleClickPoint(surfaceHost, ownerCell.Bounds.X + 10d, ownerCell.Bounds.Y + (ownerCell.Bounds.Height / 2d));
                GridSurfaceTestHost.FlushDispatcher(grid);

                var presenter = GridSurfaceTestHost.FindVisualChildren<GridCellPresenter>(surfaceHost)
                    .FirstOrDefault(candidate => candidate.CellData?.RowKey == ownerCell.RowKey && candidate.CellData.ColumnKey == "Owner");
                var editor = (presenter?.Content as Border)?.Child as ComboBox;
                Assert.That(editor, Is.Not.Null);
                Assert.That(editor!.IsEditable, Is.False);
                Assert.That(editor.SelectedItem, Is.EqualTo(row.Owner));

                editor.IsDropDownOpen = true;
                GridSurfaceTestHost.FlushDispatcher(grid);
                var municipalityItem = editor.ItemContainerGenerator.ContainerFromItem("Municipality") as ComboBoxItem;
                Assert.That(municipalityItem, Is.Not.Null);
                ClickElement(municipalityItem!);
                GridSurfaceTestHost.SendKey(surfaceHost, "ENTER");
                GridSurfaceTestHost.FlushDispatcher(grid);

                Assert.Multiple(() =>
                {
                    Assert.That(row.Owner, Is.EqualTo("Municipality"));
                    Assert.That(grid.PendingEditCount, Is.GreaterThanOrEqualTo(1));
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void DemoBinding_WhenEditingScenarioInvalidOwnerSelectsAutocompleteItem_ClickingPopupItemUpdatesEditingText()
        {
            const string invalidRowId = "BLD-WRO-FAB-0002";
            var viewModel = new DemoShellViewModel("Wpf");
            var grid = CreateBoundDemoGrid(viewModel);
            var window = GridSurfaceTestHost.CreateHostWindow(grid, width: 1400, height: 760);

            try
            {
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(grid);

                viewModel.SelectExample("editing");
                GridSurfaceTestHost.FlushDispatcher(grid);

                var surfaceHost = GridSurfaceTestHost.FindSurfaceHost(grid);
                grid.ScrollColumnIntoView("Owner", GridScrollAlignment.End);
                grid.ScrollCellIntoView(invalidRowId, "Owner", GridScrollAlignment.Start, setCurrentCell: true);
                GridSurfaceTestHost.FlushDispatcher(grid);

                var ownerCell = surfaceHost.CurrentSnapshot.Cells.First(cell =>
                    string.Equals(cell.RowKey, invalidRowId, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(cell.ColumnKey, "Owner", StringComparison.OrdinalIgnoreCase));

                GridSurfaceTestHost.DoubleClickPoint(surfaceHost, ownerCell.Bounds.X + 10d, ownerCell.Bounds.Y + (ownerCell.Bounds.Height / 2d));
                GridSurfaceTestHost.FlushDispatcher(grid);

                var presenter = GridSurfaceTestHost.FindVisualChildren<GridCellPresenter>(surfaceHost)
                    .FirstOrDefault(candidate => candidate.CellData?.RowKey == invalidRowId && candidate.CellData.ColumnKey == "Owner");
                var editor = (presenter?.Content as Border)?.Child as ComboBox;
                Assert.That(editor, Is.Not.Null);

                editor!.IsDropDownOpen = true;
                GridSurfaceTestHost.FlushDispatcher(grid);
                var municipalityItem = editor.ItemContainerGenerator.ContainerFromItem("Municipality") as ComboBoxItem;
                Assert.That(municipalityItem, Is.Not.Null);

                ClickElement(municipalityItem!);
                GridSurfaceTestHost.FlushDispatcher(grid);

                presenter = GridSurfaceTestHost.FindVisualChildren<GridCellPresenter>(surfaceHost)
                    .FirstOrDefault(candidate => candidate.CellData?.RowKey == invalidRowId && candidate.CellData.ColumnKey == "Owner");

                Assert.Multiple(() =>
                {
                    Assert.That(presenter, Is.Not.Null);
                    Assert.That(presenter!.CellData.IsEditing, Is.True);
                    Assert.That(presenter.CellData.EditingText, Is.EqualTo("Municipality"));
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void DemoBinding_WhenEditingScenarioInvalidOwnerSelectsAutocompleteItem_EnterCommitsValueAndClearsInvalidState()
        {
            const string invalidRowId = "BLD-WRO-FAB-0002";
            var viewModel = new DemoShellViewModel("Wpf");
            var grid = CreateBoundDemoGrid(viewModel);
            var window = GridSurfaceTestHost.CreateHostWindow(grid, width: 1400, height: 760);

            try
            {
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(grid);

                viewModel.SelectExample("editing");
                GridSurfaceTestHost.FlushDispatcher(grid);

                var surfaceHost = GridSurfaceTestHost.FindSurfaceHost(grid);
                grid.ScrollColumnIntoView("Owner", GridScrollAlignment.End);
                grid.ScrollCellIntoView(invalidRowId, "Owner", GridScrollAlignment.Start, setCurrentCell: true);
                GridSurfaceTestHost.FlushDispatcher(grid);

                var row = viewModel.GridRecords.First(record => string.Equals(record.ObjectId, invalidRowId, StringComparison.OrdinalIgnoreCase));
                var originalOwner = row.Owner;

                var ownerCell = surfaceHost.CurrentSnapshot.Cells.First(cell =>
                    string.Equals(cell.RowKey, invalidRowId, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(cell.ColumnKey, "Owner", StringComparison.OrdinalIgnoreCase));

                GridSurfaceTestHost.DoubleClickPoint(surfaceHost, ownerCell.Bounds.X + 10d, ownerCell.Bounds.Y + (ownerCell.Bounds.Height / 2d));
                GridSurfaceTestHost.FlushDispatcher(grid);

                var presenter = GridSurfaceTestHost.FindVisualChildren<GridCellPresenter>(surfaceHost)
                    .FirstOrDefault(candidate => candidate.CellData?.RowKey == invalidRowId && candidate.CellData.ColumnKey == "Owner");
                var editor = (presenter?.Content as Border)?.Child as ComboBox;
                Assert.That(editor, Is.Not.Null);

                editor!.IsDropDownOpen = true;
                GridSurfaceTestHost.FlushDispatcher(grid);

                var municipalityItem = editor.ItemContainerGenerator.ContainerFromItem("Municipality") as ComboBoxItem;
                Assert.That(municipalityItem, Is.Not.Null);

                ClickElement(municipalityItem!);
                GridSurfaceTestHost.FlushDispatcher(grid);
                GridSurfaceTestHost.SendKey(surfaceHost, "ENTER");
                GridSurfaceTestHost.FlushDispatcher(grid);

                Assert.Multiple(() =>
                {
                    Assert.That(originalOwner, Is.Not.EqualTo("Municipality"));
                    Assert.That(row.Owner, Is.EqualTo("Municipality"));
                    Assert.That(grid.EditSessionContext.InvalidRecordIds, Does.Not.Contain(invalidRowId));
                    Assert.That(grid.PendingEditCount, Is.GreaterThanOrEqualTo(1));
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void DemoBinding_WhenEditingScenarioInvalidOwnerDropdownOpens_DoesNotShiftHorizontalViewport()
        {
            const string invalidRowId = "BLD-WRO-FAB-0002";
            var viewModel = new DemoShellViewModel("Wpf");
            var grid = CreateBoundDemoGrid(viewModel);
            var window = GridSurfaceTestHost.CreateHostWindow(grid, width: 1400, height: 760);

            try
            {
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(grid);

                viewModel.SelectExample("editing");
                GridSurfaceTestHost.FlushDispatcher(grid);

                var surfaceHost = GridSurfaceTestHost.FindSurfaceHost(grid);
                grid.ScrollCellIntoView(invalidRowId, "Owner", GridScrollAlignment.Center, setCurrentCell: true);
                GridSurfaceTestHost.FlushDispatcher(grid);

                var ownerCell = surfaceHost.CurrentSnapshot.Cells.First(cell =>
                    string.Equals(cell.RowKey, invalidRowId, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(cell.ColumnKey, "Owner", StringComparison.OrdinalIgnoreCase));

                GridSurfaceTestHost.DoubleClickPoint(surfaceHost, ownerCell.Bounds.X + 10d, ownerCell.Bounds.Y + (ownerCell.Bounds.Height / 2d));
                GridSurfaceTestHost.FlushDispatcher(grid);

                var protectedOffset = surfaceHost.CurrentSnapshot.ViewportState.HorizontalOffset;
                var presenter = GridSurfaceTestHost.FindVisualChildren<GridCellPresenter>(surfaceHost)
                    .FirstOrDefault(candidate => candidate.CellData?.RowKey == invalidRowId && candidate.CellData.ColumnKey == "Owner");
                var editor = (presenter?.Content as Border)?.Child as ComboBox;
                Assert.That(editor, Is.Not.Null);

                editor!.IsDropDownOpen = true;
                GridSurfaceTestHost.FlushDispatcher(grid);

                Assert.Multiple(() =>
                {
                    Assert.That(editor.IsDropDownOpen, Is.True);
                    Assert.That(surfaceHost.CurrentSnapshot.ViewportState.HorizontalOffset, Is.EqualTo(protectedOffset).Within(0.1d));
                    Assert.That(surfaceHost.HorizontalOffset, Is.EqualTo(protectedOffset).Within(0.1d));
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void MainWindow_WhenEditingOwnerPopupItemIsClicked_ShouldUpdateBothEditingTextAndDisplayedSelection()
        {
            const string invalidRowId = "BLD-WRO-FAB-0002";
            var window = new MainWindow();

            try
            {
                window.Show();
                FlushDispatcher(window);

                var viewModel = (DemoShellViewModel)window.DataContext;
                viewModel.SelectExample("editing");
                FlushDispatcher(window);

                var grid = (WpfGrid)window.FindName("DemoGrid");
                Assert.That(grid, Is.Not.Null);

                var surfaceHost = GridSurfaceTestHost.FindSurfaceHost(grid);
                grid.ScrollColumnIntoView("Owner", GridScrollAlignment.End);
                grid.ScrollCellIntoView(invalidRowId, "Owner", GridScrollAlignment.Start, setCurrentCell: true);
                FlushDispatcher(window);

                var ownerCell = surfaceHost.CurrentSnapshot.Cells.First(cell =>
                    string.Equals(cell.RowKey, invalidRowId, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(cell.ColumnKey, "Owner", StringComparison.OrdinalIgnoreCase));

                GridSurfaceTestHost.DoubleClickPoint(surfaceHost, ownerCell.Bounds.X + 10d, ownerCell.Bounds.Y + (ownerCell.Bounds.Height / 2d));
                FlushDispatcher(window);

                var presenter = GridSurfaceTestHost.FindVisualChildren<GridCellPresenter>(surfaceHost)
                    .FirstOrDefault(candidate => candidate.CellData?.RowKey == invalidRowId && candidate.CellData.ColumnKey == "Owner");
                var editor = (presenter?.Content as Border)?.Child as ComboBox;
                Assert.That(editor, Is.Not.Null);

                editor!.IsDropDownOpen = true;
                FlushDispatcher(window);

                var municipalityItem = editor.ItemContainerGenerator.ContainerFromItem("Municipality") as ComboBoxItem;
                Assert.That(municipalityItem, Is.Not.Null);

                ClickElement(municipalityItem!);
                FlushDispatcher(window);
                FlushDispatcher(window);

                presenter = GridSurfaceTestHost.FindVisualChildren<GridCellPresenter>(surfaceHost)
                    .FirstOrDefault(candidate => candidate.CellData?.RowKey == invalidRowId && candidate.CellData.ColumnKey == "Owner");
                editor = (presenter?.Content as Border)?.Child as ComboBox;
                var displayedText = ReadDisplayedText(editor);

                Assert.Multiple(() =>
                {
                    Assert.That(editor, Is.Not.Null);
                    Assert.That(editor!.SelectedItem, Is.EqualTo("Municipality"));
                    Assert.That(editor.IsDropDownOpen, Is.False);
                    Assert.That(displayedText, Is.EqualTo("Municipality"));
                    Assert.That(presenter, Is.Not.Null);
                    Assert.That(presenter!.CellData.EditingText, Is.EqualTo("Municipality"));
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void DemoBinding_WhenRichEditorsOwnerEditorReceivesTyping_DoesNotAcceptArbitraryText()
        {
            var viewModel = new DemoShellViewModel("Wpf");
            var grid = CreateBoundDemoGrid(viewModel);
            var window = GridSurfaceTestHost.CreateHostWindow(grid, width: 1400, height: 760);

            try
            {
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(grid);

                viewModel.SelectExample("rich-editors");
                GridSurfaceTestHost.FlushDispatcher(grid);

                var surfaceHost = GridSurfaceTestHost.FindSurfaceHost(grid);
                grid.ScrollColumnIntoView("Owner", GridScrollAlignment.End);
                GridSurfaceTestHost.FlushDispatcher(grid);

                var ownerCell = FindEditableCell(surfaceHost, "Owner");
                var row = viewModel.GridRecords.First(record => string.Equals(record.ObjectId, ownerCell.RowKey, StringComparison.OrdinalIgnoreCase));

                GridSurfaceTestHost.DoubleClickPoint(surfaceHost, ownerCell.Bounds.X + 10d, ownerCell.Bounds.Y + (ownerCell.Bounds.Height / 2d));
                GridSurfaceTestHost.FlushDispatcher(grid);

                var presenter = GridSurfaceTestHost.FindVisualChildren<GridCellPresenter>(surfaceHost)
                    .FirstOrDefault(candidate => candidate.CellData?.RowKey == ownerCell.RowKey && candidate.CellData.ColumnKey == "Owner");
                var editor = (presenter?.Content as Border)?.Child as ComboBox;
                Assert.That(editor, Is.Not.Null);
                Assert.That(editor!.IsEditable, Is.False);
                var originalOwner = row.Owner;

                GridSurfaceTestHost.SendText(surfaceHost, "Z");
                GridSurfaceTestHost.FlushDispatcher(grid);

                presenter = GridSurfaceTestHost.FindVisualChildren<GridCellPresenter>(surfaceHost)
                    .FirstOrDefault(candidate => candidate.CellData?.RowKey == ownerCell.RowKey && candidate.CellData.ColumnKey == "Owner");
                editor = (presenter?.Content as Border)?.Child as ComboBox;

                Assert.Multiple(() =>
                {
                    Assert.That(editor, Is.Not.Null);
                    Assert.That(editor!.IsEditable, Is.False);
                    Assert.That(presenter!.CellData.EditingText, Is.EqualTo(originalOwner));
                    Assert.That(row.Owner, Is.EqualTo(originalOwner));
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void DemoBinding_WhenRichEditorsInspectionDateChanges_CommitsSelectedDateValue()
        {
            var viewModel = new DemoShellViewModel("Wpf");
            var grid = CreateBoundDemoGrid(viewModel);
            var window = GridSurfaceTestHost.CreateHostWindow(grid, width: 1400, height: 760);

            try
            {
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(grid);

                viewModel.SelectExample("rich-editors");
                GridSurfaceTestHost.FlushDispatcher(grid);

                var surfaceHost = GridSurfaceTestHost.FindSurfaceHost(grid);
                grid.ScrollColumnIntoView("LastInspection", GridScrollAlignment.End);
                GridSurfaceTestHost.FlushDispatcher(grid);
                var inspectionCell = FindEditableCell(surfaceHost, "LastInspection");
                var row = viewModel.GridRecords.First(record => string.Equals(record.ObjectId, inspectionCell.RowKey, StringComparison.OrdinalIgnoreCase));

                GridSurfaceTestHost.DoubleClickPoint(surfaceHost, inspectionCell.Bounds.X + 10d, inspectionCell.Bounds.Y + (inspectionCell.Bounds.Height / 2d));
                GridSurfaceTestHost.FlushDispatcher(grid);

                var presenter = GridSurfaceTestHost.FindVisualChildren<GridCellPresenter>(surfaceHost)
                    .FirstOrDefault(candidate => candidate.CellData?.RowKey == inspectionCell.RowKey && candidate.CellData.ColumnKey == "LastInspection");
                var editor = (presenter?.Content as Border)?.Child as DatePicker;
                Assert.That(editor, Is.Not.Null);

                var expected = new DateTime(2026, 3, 15);
                editor!.SelectedDate = expected;
                GridSurfaceTestHost.SendKey(surfaceHost, "ENTER");
                GridSurfaceTestHost.FlushDispatcher(grid);

                Assert.Multiple(() =>
                {
                    Assert.That(row.LastInspection.Date, Is.EqualTo(expected.Date));
                    Assert.That(grid.PendingEditCount, Is.GreaterThanOrEqualTo(1));
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void DemoBinding_WhenEditingScenarioCancelsPendingEdits_RestoresMultipleRowsAndClearsPendingState()
        {
            var viewModel = new DemoShellViewModel("Wpf");
            var grid = CreateBoundDemoGrid(viewModel);
            var window = GridSurfaceTestHost.CreateHostWindow(grid, width: 1200, height: 700);

            try
            {
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(grid);

                viewModel.SelectExample("editing");
                GridSurfaceTestHost.FlushDispatcher(grid);

                var surfaceHost = GridSurfaceTestHost.FindSurfaceHost(grid);
                var rowModels = grid.RowsView.Cast<GridDataRowModel>().Take(2).ToArray();
                Assert.That(rowModels.Length, Is.EqualTo(2));

                var row1 = (DemoGisRecordViewModel)rowModels[0].SourceRow;
                var row2 = (DemoGisRecordViewModel)rowModels[1].SourceRow;
                var originalName = row1.ObjectName;
                var originalSecondName = row2.ObjectName;

                var objectNameCells = surfaceHost.CurrentSnapshot.Cells
                    .Where(item => string.Equals(item.ColumnKey, "ObjectName", StringComparison.Ordinal))
                    .Take(2)
                    .ToArray();
                Assert.That(objectNameCells.Length, Is.EqualTo(2));

                AppendAndCommitSurfaceCell(surfaceHost, objectNameCells[0], " :: changed");
                AppendAndCommitSurfaceCell(surfaceHost, objectNameCells[1], " :: changed");
                GridSurfaceTestHost.FlushDispatcher(grid);

                Assert.Multiple(() =>
                {
                    Assert.That(grid.HasPendingEdits, Is.True);
                    Assert.That(grid.PendingEditCount, Is.EqualTo(2));
                    Assert.That(row1.ObjectName, Is.Not.EqualTo(originalName));
                    Assert.That(row2.ObjectName, Is.Not.EqualTo(originalSecondName));
                });

                grid.CancelEdits();
                GridSurfaceTestHost.FlushDispatcher(grid);

                Assert.Multiple(() =>
                {
                    Assert.That(row1.ObjectName, Is.EqualTo(originalName));
                    Assert.That(row2.ObjectName, Is.EqualTo(originalSecondName));
                    Assert.That(rowModels[0]["ObjectName"], Is.EqualTo(originalName));
                    Assert.That(rowModels[1]["ObjectName"], Is.EqualTo(originalSecondName));
                    Assert.That(grid.HasPendingEdits, Is.False);
                    Assert.That(grid.PendingEditCount, Is.EqualTo(0));
                    Assert.That(grid.HasValidationIssues, Is.False);
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void DemoBinding_WhenEditingScenarioCommitsInvalidValue_ValidationStateIsRaisedAndCancelRestoresOriginalData()
        {
            var viewModel = new DemoShellViewModel("Wpf");
            var grid = CreateBoundDemoGrid(viewModel);
            var window = GridSurfaceTestHost.CreateHostWindow(grid, width: 1200, height: 700);

            try
            {
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(grid);

                viewModel.SelectExample("editing");
                GridSurfaceTestHost.FlushDispatcher(grid);

                var surfaceHost = GridSurfaceTestHost.FindSurfaceHost(grid);
                var rowModel = grid.RowsView.Cast<GridDataRowModel>().First();
                var sourceRow = (DemoGisRecordViewModel)rowModel.SourceRow;
                var originalValue = sourceRow.ObjectName;
                var objectNameCell = FindEditableCell(surfaceHost, "ObjectName");

                GridSurfaceTestHost.DoubleClickPoint(surfaceHost, objectNameCell.Bounds.X + 10d, objectNameCell.Bounds.Y + (objectNameCell.Bounds.Height / 2d));
                for (var index = 0; index < originalValue.Length; index++)
                {
                    GridSurfaceTestHost.SendKey(surfaceHost, "DELETE");
                }
                GridSurfaceTestHost.SendKey(surfaceHost, "ENTER");
                GridSurfaceTestHost.FlushDispatcher(grid);

                Assert.Multiple(() =>
                {
                    Assert.That(sourceRow.ObjectName, Is.EqualTo(originalValue));
                    Assert.That(grid.HasPendingEdits, Is.False);
                    Assert.That(grid.PendingEditCount, Is.EqualTo(0));
                    Assert.That(grid.HasValidationIssues, Is.True);
                    Assert.That(grid.PendingEditBannerText, Does.Contain("validation").IgnoreCase);
                });

                grid.CancelEdits();
                GridSurfaceTestHost.FlushDispatcher(grid);

                Assert.Multiple(() =>
                {
                    Assert.That(sourceRow.ObjectName, Is.EqualTo(originalValue));
                    Assert.That(rowModel["ObjectName"], Is.EqualTo(originalValue));
                    Assert.That(grid.HasPendingEdits, Is.False);
                    Assert.That(grid.PendingEditCount, Is.EqualTo(0));
                    Assert.That(grid.HasValidationIssues, Is.False);
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void MaskedTextBoxBehavior_WhenInvalidTextIsEntered_RevertsToPreviousValidText()
        {
            var textBox = new TextBox
            {
                Text = "123",
            };

            MaskedTextBoxBehavior.SetMaskPattern(textBox, "^[0-9]{0,6}$");
            textBox.Text = "12a";

            Assert.That(textBox.Text, Is.EqualTo("123"));
        }

        [Test]
        public void DemoBinding_WhenSummariesScenarioAddsAndResetsSummary_LiveGridSummaryItemsFollow()
        {
            var viewModel = new DemoShellViewModel("Wpf");
            var grid = CreateBoundDemoGrid(viewModel);
            var window = GridSurfaceTestHost.CreateHostWindow(grid, width: 1200, height: 700);

            try
            {
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(grid);

                viewModel.SelectExample("summaries");
                GridSurfaceTestHost.FlushDispatcher(grid);

                var initialCount = grid.SummaryItems.Count;

                viewModel.SelectedSummaryColumn = viewModel.AvailableSummaryColumns.Single(option => option.ColumnId == "Priority");
                viewModel.SelectedSummaryType = viewModel.AvailableSummaryTypes.Single(option => option.Type == PhialeGrid.Core.Summaries.GridSummaryType.Min);
                viewModel.AddSelectedSummary();
                GridSurfaceTestHost.FlushDispatcher(grid);

                var summaryHost = (FrameworkElement)grid.FindName("SummaryBottomRegionHost");
                var summaryContent = (FrameworkElement)grid.FindName("SummaryBottomRegionContentScrollViewer");
                var displayedTexts = GridSurfaceTestHost.FindVisualChildren<TextBlock>(summaryHost)
                    .Select(text => text.Text)
                    .Where(text => !string.IsNullOrWhiteSpace(text))
                    .ToArray();

                Assert.That(grid.SummaryItems.Count, Is.EqualTo(initialCount + 1));
                Assert.That(grid.SummaryItems.Any(item => item.Label.IndexOf("Priority", StringComparison.OrdinalIgnoreCase) >= 0), Is.True);
                var prioritySummary = grid.SummaryItems.Single(item => string.Equals(item.ColumnLabel, "Priority", StringComparison.OrdinalIgnoreCase));
                Assert.Multiple(() =>
                {
                    Assert.That(prioritySummary.TypeLabel, Is.EqualTo("Min"));
                    Assert.That(prioritySummary.ValueText, Is.Not.Empty);
                });
                Assert.That(summaryContent.Visibility, Is.EqualTo(Visibility.Visible));
                Assert.That(summaryHost.ActualHeight, Is.GreaterThan(24d));
                Assert.That(displayedTexts, Does.Not.Contain("Summaries"));
                var summaryPlayground = summaryContent as PhialeTech.PhialeGrid.Wpf.Controls.PhialeWorkspacePlayground;
                Assert.That(
                    displayedTexts.Any(text => text.IndexOf("Priority", StringComparison.OrdinalIgnoreCase) >= 0),
                    Is.True,
                    "Rendered summary texts: " + string.Join(" | ", displayedTexts)
                    + "; content type: " + summaryContent.GetType().FullName
                    + "; playground content set: " + (summaryPlayground?.PlaygroundContent != null));

                viewModel.ResetSummaries();
                GridSurfaceTestHost.FlushDispatcher(grid);

                Assert.That(grid.SummaryItems.Count, Is.EqualTo(3));
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void DemoBinding_WhenLanguageChanges_SummaryResultChipsUseLocalizedAggregationLabels()
        {
            var viewModel = new DemoShellViewModel("Wpf");
            var grid = CreateBoundDemoGrid(viewModel);
            var window = GridSurfaceTestHost.CreateHostWindow(grid, width: 1200, height: 700);

            try
            {
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(grid);

                viewModel.SelectExample("summaries");
                GridSurfaceTestHost.FlushDispatcher(grid);

                var englishCountSummary = grid.SummaryItems.Single(item =>
                    string.Equals(item.TypeLabel, "Count", StringComparison.Ordinal));

                Assert.Multiple(() =>
                {
                    Assert.That(englishCountSummary.ValueText, Is.Not.Empty);
                    Assert.That(englishCountSummary.Label, Is.EqualTo(englishCountSummary.ColumnLabel + " Count"));
                });

                grid.LanguageCode = "pl";
                GridSurfaceTestHost.FlushDispatcher(grid);

                var localizedCountSummary = grid.SummaryItems.Single(item =>
                    string.Equals(item.ColumnLabel, englishCountSummary.ColumnLabel, StringComparison.Ordinal) &&
                    string.Equals(item.TypeLabel, "Ilość", StringComparison.Ordinal));
                var summaryHost = (FrameworkElement)grid.FindName("SummaryBottomRegionHost");
                var renderedTexts = GridSurfaceTestHost.FindVisualChildren<TextBlock>(summaryHost)
                    .Select(text => text.Text)
                    .Where(text => !string.IsNullOrWhiteSpace(text))
                    .ToArray();

                Assert.Multiple(() =>
                {
                    Assert.That(localizedCountSummary.ValueText, Is.EqualTo(englishCountSummary.ValueText));
                    Assert.That(localizedCountSummary.Label, Is.EqualTo(englishCountSummary.ColumnLabel + " Ilość"));
                    Assert.That(renderedTexts, Does.Contain("Ilość"));
                    Assert.That(renderedTexts, Does.Contain(":"));
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void DemoBinding_WhenScenarioClearsSummaries_SummaryBandAndDesignerChipsClearTogether()
        {
            var viewModel = new DemoShellViewModel("Wpf");
            var grid = CreateBoundDemoGrid(viewModel);
            var window = GridSurfaceTestHost.CreateHostWindow(grid, width: 1200, height: 700);

            try
            {
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(grid);

                viewModel.SelectExample("summaries");
                GridSurfaceTestHost.FlushDispatcher(grid);

                Assert.Multiple(() =>
                {
                    Assert.That(viewModel.GridSummaries.Count, Is.GreaterThan(0));
                    Assert.That(viewModel.ConfiguredSummaries.Count, Is.EqualTo(viewModel.GridSummaries.Count));
                    Assert.That(grid.SummaryItems.Count, Is.EqualTo(viewModel.GridSummaries.Count));
                });

                viewModel.SelectExample("editing");
                GridSurfaceTestHost.FlushDispatcher(grid);

                var summaryHost = (FrameworkElement)grid.FindName("SummaryBottomRegionHost");
                var displayedTexts = GridSurfaceTestHost.FindVisualChildren<TextBlock>(summaryHost)
                    .Select(text => text.Text)
                    .Where(text => !string.IsNullOrWhiteSpace(text))
                    .ToArray();

                Assert.Multiple(() =>
                {
                    Assert.That(viewModel.GridSummaries.Count, Is.EqualTo(0));
                    Assert.That(viewModel.ConfiguredSummaries.Count, Is.EqualTo(0));
                    Assert.That(grid.SummaryItems.Count, Is.EqualTo(0));
                    Assert.That(displayedTexts.Any(text => text.IndexOf("Count", StringComparison.OrdinalIgnoreCase) >= 0), Is.False);
                    Assert.That(displayedTexts.Any(text => text.IndexOf("Sum", StringComparison.OrdinalIgnoreCase) >= 0), Is.False);
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void DemoBinding_WhenViewStateRestoresSummaries_DesignerChipsUseTheSameDefinitionsAsSummaryBand()
        {
            var viewModel = new DemoShellViewModel("Wpf");
            var grid = CreateBoundDemoGrid(viewModel);
            var window = GridSurfaceTestHost.CreateHostWindow(grid, width: 1200, height: 700);

            try
            {
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(grid);

                viewModel.SelectExample("summaries");
                GridSurfaceTestHost.FlushDispatcher(grid);
                var savedSummaryState = grid.ExportViewState();

                viewModel.SelectExample("editing");
                GridSurfaceTestHost.FlushDispatcher(grid);
                Assert.That(viewModel.ConfiguredSummaries.Count, Is.EqualTo(0));

                grid.ApplyViewState(savedSummaryState);
                GridSurfaceTestHost.FlushDispatcher(grid);

                Assert.Multiple(() =>
                {
                    Assert.That(grid.SummaryItems.Count, Is.GreaterThan(0));
                    Assert.That(viewModel.GridSummaries.Count, Is.EqualTo(grid.SummaryItems.Count));
                    Assert.That(viewModel.ConfiguredSummaries.Count, Is.EqualTo(grid.SummaryItems.Count));
                    Assert.That(viewModel.ConfiguredSummaries.Select(summary => summary.ColumnId), Is.EqualTo(viewModel.GridSummaries.Select(summary => summary.ColumnId)));
                });
            }
            finally
            {
                window.Close();
            }
        }

        private static WpfGrid CreateBoundDemoGrid(DemoShellViewModel viewModel)
        {
            var grid = new WpfGrid
            {
                LanguageDirectory = GetLanguageDirectory(),
                Width = 1200,
                Height = 700,
                DataContext = viewModel,
            };
            BindingOperations.SetBinding(grid, WpfGrid.ItemsSourceProperty, new Binding(nameof(DemoShellViewModel.GridRecords)));
            BindingOperations.SetBinding(grid, WpfGrid.ColumnsProperty, new Binding(nameof(DemoShellViewModel.GridColumns)));
            BindingOperations.SetBinding(grid, WpfGrid.EditSessionContextProperty, new Binding(nameof(DemoShellViewModel.GridEditSessionContext)));
            BindingOperations.SetBinding(grid, WpfGrid.GroupsProperty, new Binding(nameof(DemoShellViewModel.GridGroups)) { Mode = BindingMode.TwoWay });
            BindingOperations.SetBinding(grid, WpfGrid.SortsProperty, new Binding(nameof(DemoShellViewModel.GridSorts)) { Mode = BindingMode.TwoWay });
            BindingOperations.SetBinding(grid, WpfGrid.SummariesProperty, new Binding(nameof(DemoShellViewModel.GridSummaries)) { Mode = BindingMode.TwoWay });
            BindingOperations.SetBinding(grid, WpfGrid.IsGridReadOnlyProperty, new Binding(nameof(DemoShellViewModel.IsGridReadOnly)));
            return grid;
        }

        private static WpfGrid CreateInlineGroupingGrid()
        {
            return new WpfGrid
            {
                LanguageDirectory = GetLanguageDirectory(),
                Width = 900,
                Height = 600,
                Columns = new[]
                {
                    new GridColumnDefinition("Category", "Category", displayIndex: 0, valueType: typeof(string), isEditable: false),
                    new GridColumnDefinition("ObjectName", "Object name", displayIndex: 1, valueType: typeof(string), isEditable: true),
                },
                ItemsSource = new object[]
                {
                    CreateRow("GIS-1", "Valve", "Valve A", "Operations"),
                    CreateRow("GIS-2", "Valve", "Valve B", "Operations"),
                    CreateRow("GIS-3", "Pipe", "Pipe A", "Infrastructure"),
                },
            };
        }

        private static DemoGisRecordViewModel CreateRow(string id, string objectName, string owner)
        {
            return CreateRow(id, "Valve", objectName, owner);
        }

        private static int CountVisibleDataRows(WpfGrid grid)
        {
            return grid.RowsView.Cast<object>().OfType<GridDataRowModel>().Count();
        }

        private static DemoGisRecordViewModel CreateRow(string id, string category, string objectName, string owner)
        {
            return new DemoGisRecordViewModel(
                id,
                category,
                objectName,
                "Point",
                "EPSG:2180",
                "Wroclaw",
                "Srodmiescie",
                "Active",
                0m,
                0m,
                new DateTime(2025, 1, 2),
                "Survey",
                "Medium",
                true,
                true,
                owner,
                500,
                "network");
        }

        private static string GetLanguageDirectory()
        {
            return global::PhialeGrid.Wpf.Tests.GridTestRepositoryPaths.GridLanguagesDirectory;
        }

        private static PhialeGrid.Core.Surface.GridCellSurfaceItem FindEditableCell(PhialeTech.PhialeGrid.Wpf.Surface.GridSurfaceHost surfaceHost, string columnId)
        {
            return surfaceHost.CurrentSnapshot.Cells.First(cell =>
                string.Equals(cell.ColumnKey, columnId, StringComparison.Ordinal) &&
                !string.IsNullOrWhiteSpace(cell.RowKey) &&
                !cell.RowKey.StartsWith("group:", StringComparison.OrdinalIgnoreCase));
        }

        private static void AppendAndCommitSurfaceCell(
            PhialeTech.PhialeGrid.Wpf.Surface.GridSurfaceHost surfaceHost,
            PhialeGrid.Core.Surface.GridCellSurfaceItem cell,
            string appendedText)
        {
            GridSurfaceTestHost.DoubleClickPoint(surfaceHost, cell.Bounds.X + 10d, cell.Bounds.Y + (cell.Bounds.Height / 2d));
            AppendText(surfaceHost, appendedText);
            GridSurfaceTestHost.SendKey(surfaceHost, "ENTER");
        }

        private static void AppendText(PhialeTech.PhialeGrid.Wpf.Surface.GridSurfaceHost surfaceHost, string text)
        {
            foreach (var character in text)
            {
                GridSurfaceTestHost.SendText(surfaceHost, character.ToString());
            }
        }

        private static void ClickElement(UIElement element)
        {
            Assert.That(element, Is.Not.Null);
            element.Dispatcher.Invoke(() =>
            {
                var previewMouseDown = new MouseButtonEventArgs(Mouse.PrimaryDevice, Environment.TickCount, MouseButton.Left)
                {
                    RoutedEvent = UIElement.PreviewMouseLeftButtonDownEvent,
                    Source = element,
                };
                element.RaiseEvent(previewMouseDown);

                var mouseDown = new MouseButtonEventArgs(Mouse.PrimaryDevice, Environment.TickCount, MouseButton.Left)
                {
                    RoutedEvent = UIElement.MouseLeftButtonDownEvent,
                    Source = element,
                };
                element.RaiseEvent(mouseDown);

                var previewMouseUp = new MouseButtonEventArgs(Mouse.PrimaryDevice, Environment.TickCount, MouseButton.Left)
                {
                    RoutedEvent = UIElement.PreviewMouseLeftButtonUpEvent,
                    Source = element,
                };
                element.RaiseEvent(previewMouseUp);

                var mouseUp = new MouseButtonEventArgs(Mouse.PrimaryDevice, Environment.TickCount, MouseButton.Left)
                {
                    RoutedEvent = UIElement.MouseLeftButtonUpEvent,
                    Source = element,
                };
                element.RaiseEvent(mouseUp);
            });
        }

        private static UIElement ResolveHitTestUiElement(FrameworkElement root, Point point)
        {
            if (root == null)
            {
                return null;
            }

            var current = root.InputHitTest(point) as DependencyObject;
            while (current != null && !(current is UIElement))
            {
                current = GetVisualOrLogicalParent(current);
            }

            return current as UIElement;
        }

        private static bool IsDescendantOf(DependencyObject element, DependencyObject ancestor)
        {
            var current = element;
            while (current != null)
            {
                if (ReferenceEquals(current, ancestor))
                {
                    return true;
                }

                current = GetVisualOrLogicalParent(current);
            }

            return false;
        }

        private static DependencyObject GetVisualOrLogicalParent(DependencyObject element)
        {
            if (element == null)
            {
                return null;
            }

            if (element is Visual || element is Visual3D)
            {
                var visualParent = VisualTreeHelper.GetParent(element);
                if (visualParent != null)
                {
                    return visualParent;
                }
            }

            return LogicalTreeHelper.GetParent(element);
        }

        private static void FlushDispatcher(Window window)
        {
            window.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Background);
            window.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.ApplicationIdle);
        }

        private static string ReadDisplayedText(DependencyObject root)
        {
            if (root == null)
            {
                return string.Empty;
            }

            if (root is TextBlock textBlock)
            {
                return textBlock.Text ?? string.Empty;
            }

            if (root is ContentPresenter presenter)
            {
                return presenter.Content?.ToString() ?? string.Empty;
            }

            return GridSurfaceTestHost.FindVisualChildren<TextBlock>(root)
                .Select(text => text.Text)
                .FirstOrDefault(text => !string.IsNullOrWhiteSpace(text))
                ?? string.Empty;
        }
    }
}

