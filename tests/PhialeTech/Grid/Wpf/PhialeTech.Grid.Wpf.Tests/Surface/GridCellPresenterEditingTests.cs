using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Threading;
using System.Reflection;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;
using System.Windows.Threading;
using PhialeGrid.Core.Columns;
using NUnit.Framework;
using PhialeGrid.Core.Surface;
using PhialeTech.PhialeGrid.Wpf.Surface.Presenters;
using UniversalInput.Contracts;
using WpfCalendar = System.Windows.Controls.Calendar;

namespace PhialeGrid.Wpf.Tests.Surface
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public sealed class GridCellPresenterEditingTests
    {
        [Test]
        public void CellData_WhenEditing_RendersTextBoxWithEditingText()
        {
            var presenter = new GridCellPresenter
            {
                CellData = new GridCellSurfaceItem("row-1", "col-1")
                {
                    DisplayText = "Alpha",
                    IsEditing = true,
                },
            };

            Assert.That(presenter.Content, Is.TypeOf<Border>());
            var chrome = (Border)presenter.Content;
            var editor = chrome.Child as TextBox;
            Assert.Multiple(() =>
            {
                Assert.That(editor, Is.Not.Null);
                Assert.That(editor.Text, Is.EqualTo("Alpha"));
                Assert.That(editor.FocusVisualStyle, Is.Null);
                Assert.That(editor.BorderThickness, Is.EqualTo(new Thickness(0)));
                Assert.That(chrome.RenderTransform, Is.TypeOf<System.Windows.Media.ScaleTransform>());
                Assert.That(((System.Windows.Media.ScaleTransform)chrome.RenderTransform).ScaleX, Is.EqualTo(1.00d).Within(0.02d));
                Assert.That(((System.Windows.Media.ScaleTransform)chrome.RenderTransform).ScaleY, Is.EqualTo(1.00d).Within(0.02d));
                Assert.That(chrome.RenderTransformOrigin, Is.EqualTo(new Point(0.5d, 0.5d)));
            });
        }

        [Test]
        public void CellData_WhenEditing_WithThemeResources_KeepsTransparentBorderlessTextEditor()
        {
            var presenter = new GridCellPresenter
            {
                CellData = new GridCellSurfaceItem("row-1", "col-1")
                {
                    DisplayText = "Alpha",
                    IsEditing = true,
                },
            };

            var window = CreateStyledHostWindow(presenter);

            try
            {
                window.Show();
                FlushDispatcher(window.Dispatcher);

                var editor = (presenter.Content as Border)?.Child as TextBox;
                Assert.That(editor, Is.Not.Null);
                editor!.ApplyTemplate();
                var contentHost = editor.Template?.FindName("PART_ContentHost", editor) as FrameworkElement;

                Assert.Multiple(() =>
                {
                    Assert.That(editor.Background, Is.EqualTo(Brushes.Transparent));
                    Assert.That(editor.BorderThickness, Is.EqualTo(new Thickness(0d)));
                    Assert.That(contentHost, Is.Not.Null);
                    Assert.That(editor.Clip, Is.InstanceOf<RectangleGeometry>());
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void CellData_WhenNotEditing_RendersPlainDisplayText()
        {
            var presenter = new GridCellPresenter
            {
                CellData = new GridCellSurfaceItem("row-1", "col-1")
                {
                    DisplayText = "Alpha",
                    IsEditing = false,
                    ValueKind = "Text",
                },
            };

            Assert.That(presenter.Content, Is.TypeOf<TextBlock>());
            Assert.Multiple(() =>
            {
                Assert.That(((TextBlock)presenter.Content).Text, Is.EqualTo("Alpha"));
                Assert.That(((TextBlock)presenter.Content).TextAlignment, Is.EqualTo(TextAlignment.Left));
            });
        }

        [Test]
        public void CellData_WhenNumericValueKind_RendersRightAlignedText()
        {
            var presenter = new GridCellPresenter
            {
                CellData = new GridCellSurfaceItem("row-1", "AreaSquareMeters")
                {
                    DisplayText = "157,41",
                    RawValue = 157.41m,
                    IsEditing = false,
                    ValueKind = "Number",
                },
            };

            Assert.That(presenter.Content, Is.TypeOf<TextBlock>());
            Assert.That(((TextBlock)presenter.Content).TextAlignment, Is.EqualTo(TextAlignment.Right));
        }

        [Test]
        public void CellData_WhenBooleanValueKind_RendersCenteredCheckBox()
        {
            var presenter = new GridCellPresenter
            {
                CellData = new GridCellSurfaceItem("row-1", "Visible")
                {
                    RawValue = true,
                    DisplayText = "True",
                    IsEditing = false,
                    ValueKind = "Boolean",
                },
            };

            Assert.That(presenter.Content, Is.TypeOf<CheckBox>());
            Assert.Multiple(() =>
            {
                Assert.That(((CheckBox)presenter.Content).IsChecked, Is.True);
                Assert.That(((CheckBox)presenter.Content).HorizontalAlignment, Is.EqualTo(HorizontalAlignment.Center));
            });
        }

        [Test]
        public void CellData_WhenGroupCaptionCell_RendersInlineChevronAndSeparateCaptionContent()
        {
            var presenter = new GridCellPresenter
            {
                CellData = new GridCellSurfaceItem("group:alpha", "col-1")
                {
                    DisplayText = "City: Alpha (2)",
                    IsGroupCaptionCell = true,
                    ShowInlineChevron = true,
                    IsInlineChevronExpanded = true,
                    ContentIndent = 20d,
                    IsReadOnly = true,
                },
                Bounds = new GridBounds(0, 0, 180, 24),
            };

            var groupRoot = GridSurfaceTestHost.FindElementByAutomationId<FrameworkElement>(presenter, "surface.group-cell.group:alpha.col-1");
            var chevron = GridSurfaceTestHost.FindElementByAutomationId<Path>(presenter, "surface.group-toggle.group:alpha.col-1");
            var caption = GridSurfaceTestHost.FindElementByAutomationId<TextBlock>(presenter, "surface.group-caption.group:alpha.col-1");

            Assert.Multiple(() =>
            {
                Assert.That(presenter.Content, Is.InstanceOf<Grid>());
                Assert.That(groupRoot, Is.Not.Null);
                Assert.That(chevron, Is.Not.Null);
                Assert.That(caption, Is.Not.Null);
                Assert.That(caption.Text, Is.EqualTo("City: Alpha (2)"));
                Assert.That(GridSurfaceTestHost.ReadVisibleText(presenter), Is.EqualTo("City: Alpha (2)"));
            });
        }

        [Test]
        public void EditingTextBox_WhenUserChangesText_RaisesEditingTextChangedForActiveCell()
        {
            var presenter = new GridCellPresenter
            {
                CellData = new GridCellSurfaceItem("row-1", "col-1")
                {
                    DisplayText = "Alpha",
                    EditingText = "Alpha",
                    IsEditing = true,
                },
            };

            GridCellEditingTextChangedEventArgs raisedArgs = null;
            presenter.EditingTextChanged += (sender, args) => raisedArgs = args;

            var editor = (presenter.Content as Border)?.Child as TextBox;
            Assert.That(editor, Is.Not.Null);

            editor.Text = "Alpha Beta";

            Assert.Multiple(() =>
            {
                Assert.That(raisedArgs, Is.Not.Null);
                Assert.That(raisedArgs.RowKey, Is.EqualTo("row-1"));
                Assert.That(raisedArgs.ColumnKey, Is.EqualTo("col-1"));
                Assert.That(raisedArgs.Text, Is.EqualTo("Alpha Beta"));
                Assert.That(raisedArgs.ChangeKind, Is.EqualTo(UniversalEditorValueChangeKind.TextEdited));
            });
        }

        [Test]
        public void EditingTextBox_WhenBringIntoViewIsRequested_HandlesRequestToKeepOuterScrollStable()
        {
            var presenter = new GridCellPresenter
            {
                CellData = new GridCellSurfaceItem("row-1", "col-1")
                {
                    DisplayText = "Oliwa Segment 3",
                    EditingText = "Oliwa Segment 3",
                    IsEditing = true,
                },
            };

            var editor = (presenter.Content as Border)?.Child as TextBox;
            Assert.That(editor, Is.Not.Null);
            if (editor == null)
            {
                return;
            }

            var args = (System.Windows.RequestBringIntoViewEventArgs)Activator.CreateInstance(
                typeof(System.Windows.RequestBringIntoViewEventArgs),
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                binder: null,
                args: new object[] { editor, new Rect(0, 0, 16, 16) },
                culture: null);
            Assert.That(args, Is.Not.Null);
            if (args == null)
            {
                return;
            }

            args.RoutedEvent = FrameworkElement.RequestBringIntoViewEvent;
            args.Source = editor;

            editor.RaiseEvent(args);

            Assert.That(args.Handled, Is.True);
        }

        [Test]
        public void CellData_WhenComboEditorIsRequested_RendersComboBoxAndRaisesSelectionChanges()
        {
            var presenter = new GridCellPresenter
            {
                CellData = new GridCellSurfaceItem("row-1", "Status")
                {
                    DisplayText = "Verified",
                    EditingText = "Verified",
                    IsEditing = true,
                    EditorKind = GridColumnEditorKind.Combo,
                    EditorItems = new[] { "Active", "Verified", "Retired" },
                },
            };

            GridCellEditingTextChangedEventArgs raisedArgs = null;
            presenter.EditingTextChanged += (_, args) => raisedArgs = args;

            Assert.That(presenter.Content, Is.TypeOf<Border>());
            var editor = (presenter.Content as Border)?.Child as ComboBox;
            Assert.That(editor, Is.Not.Null);
            editor.SelectedItem = "Retired";

            Assert.Multiple(() =>
            {
                Assert.That(editor.Items.Count, Is.EqualTo(3));
                Assert.That(raisedArgs, Is.Not.Null);
                Assert.That(raisedArgs.Text, Is.EqualTo("Retired"));
                Assert.That(raisedArgs.ChangeKind, Is.EqualTo(UniversalEditorValueChangeKind.SelectionCommitted));
            });
        }

        [Test]
        public void CellData_WhenAutocompleteEditorSelectionChanges_RaisesSelectedValue()
        {
            var presenter = new GridCellPresenter
            {
                CellData = new GridCellSurfaceItem("row-1", "Owner")
                {
                    DisplayText = "Water Utility",
                    EditingText = "Water Utility",
                    IsEditing = true,
                    EditorKind = GridColumnEditorKind.Autocomplete,
                    EditorItemsMode = GridEditorItemsMode.Suggestions,
                    EditorItems = new[] { "Water Utility", "Municipality", "Parks Department" },
                },
            };

            GridCellEditingTextChangedEventArgs raisedArgs = null;
            presenter.EditingTextChanged += (_, args) => raisedArgs = args;

            Assert.That(presenter.Content, Is.TypeOf<Border>());
            var editor = (presenter.Content as Border)?.Child as ComboBox;
            Assert.That(editor, Is.Not.Null);

            editor!.SelectedItem = "Municipality";

            Assert.Multiple(() =>
            {
                Assert.That(editor.Items.Count, Is.EqualTo(3));
                Assert.That(editor.IsEditable, Is.True);
                Assert.That(raisedArgs, Is.Not.Null);
                Assert.That(raisedArgs.Text, Is.EqualTo("Municipality"));
                Assert.That(raisedArgs.ChangeKind, Is.EqualTo(UniversalEditorValueChangeKind.SelectionCommitted));
            });
        }

        [Test]
        public void CellData_WhenRestrictedAutocompleteDropdownClosesWithNewSelection_RaisesSelectedValue()
        {
            var presenter = new GridCellPresenter
            {
                CellData = new GridCellSurfaceItem("row-1", "Owner")
                {
                    DisplayText = "Water Utility",
                    EditingText = "Water Utility",
                    IsEditing = true,
                    EditorKind = GridColumnEditorKind.Autocomplete,
                    EditorItemsMode = GridEditorItemsMode.RestrictToItems,
                    EditorItems = new[] { "Water Utility", "Municipality", "Parks Department" },
                },
            };

            GridCellEditingTextChangedEventArgs raisedArgs = null;
            presenter.EditingTextChanged += (_, args) => raisedArgs = args;

            var window = new Window
            {
                Width = 400,
                Height = 240,
                Content = presenter,
                ShowInTaskbar = false,
                WindowStyle = WindowStyle.None,
            };

            try
            {
                window.Show();
                FlushDispatcher(window.Dispatcher);

                Assert.That(presenter.Content, Is.TypeOf<Border>());
                var editor = (presenter.Content as Border)?.Child as ComboBox;
                Assert.That(editor, Is.Not.Null);
                Assert.That(editor!.IsEditable, Is.False);

                editor.IsDropDownOpen = true;
                FlushDispatcher(window.Dispatcher);
                editor.SelectedItem = "Municipality";
                editor.IsDropDownOpen = false;
                FlushDispatcher(window.Dispatcher);

                Assert.Multiple(() =>
                {
                    Assert.That(raisedArgs, Is.Not.Null);
                    Assert.That(raisedArgs!.Text, Is.EqualTo("Municipality"));
                    Assert.That(raisedArgs.ChangeKind, Is.EqualTo(UniversalEditorValueChangeKind.SelectionCommitted));
                    Assert.That(editor.SelectedItem, Is.EqualTo("Municipality"));
                    Assert.That(editor.IsDropDownOpen, Is.False);
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void CellData_WhenAutocompleteEditorUsesRestrictedItems_RendersNonEditableSelector()
        {
            var presenter = new GridCellPresenter
            {
                CellData = new GridCellSurfaceItem("row-1", "Owner")
                {
                    DisplayText = "Water Utility",
                    EditingText = "Water Utility",
                    IsEditing = true,
                    EditorKind = GridColumnEditorKind.Autocomplete,
                    EditorItemsMode = GridEditorItemsMode.RestrictToItems,
                    EditorItems = new[] { "Water Utility", "Municipality", "Parks Department" },
                },
            };

            var editor = (presenter.Content as Border)?.Child as ComboBox;
            Assert.That(editor, Is.Not.Null);
            Assert.That(editor!.IsEditable, Is.False);
        }

        [Test]
        public void CellData_WhenAutocompleteEditorUsesRestrictedItems_IgnoresProgrammaticTextChanges()
        {
            var presenter = new GridCellPresenter
            {
                CellData = new GridCellSurfaceItem("row-1", "Owner")
                {
                    DisplayText = "Water Utility",
                    EditingText = "Water Utility",
                    IsEditing = true,
                    EditorKind = GridColumnEditorKind.Autocomplete,
                    EditorItemsMode = GridEditorItemsMode.RestrictToItems,
                    EditorItems = new[] { "Water Utility", "Municipality", "Parks Department" },
                },
            };

            var raisedValues = new System.Collections.Generic.List<string>();
            presenter.EditingTextChanged += (_, args) => raisedValues.Add(args.Text);

            var editor = (presenter.Content as Border)?.Child as ComboBox;
            Assert.That(editor, Is.Not.Null);
            Assert.That(editor!.IsEditable, Is.False);

            editor.Text = "External vendor";

            Assert.That(raisedValues, Is.Empty);
        }

        [Test]
        public void CellData_WhenAutocompleteEditorUsesSuggestions_WithThemeResources_KeepsEditableInputAccessible()
        {
            var presenter = new GridCellPresenter
            {
                CellData = new GridCellSurfaceItem("row-1", "Owner")
                {
                    DisplayText = "Water Utility",
                    EditingText = "Water Utility",
                    IsEditing = true,
                    EditorKind = GridColumnEditorKind.Autocomplete,
                    EditorItemsMode = GridEditorItemsMode.Suggestions,
                    EditorItems = new[] { "Water Utility", "Municipality", "Parks Department" },
                },
            };

            var window = CreateStyledHostWindow(presenter);

            try
            {
                window.Show();
                FlushDispatcher(window.Dispatcher);

                var editor = (presenter.Content as Border)?.Child as ComboBox;
                Assert.That(editor, Is.Not.Null);

                editor!.ApplyTemplate();
                var editableTextBox = editor.Template?.FindName("PART_EditableTextBox", editor) as TextBox;
                var comboRoot = editor.Template?.FindName("PgEditingComboRoot", editor) as FrameworkElement;
                Assert.That(editableTextBox, Is.Not.Null);

                Assert.Multiple(() =>
                {
                    Assert.That(editor.IsEditable, Is.True);
                    Assert.That(comboRoot, Is.Not.Null);
                    Assert.That(editor.Template?.FindName("PgEditingComboChevron", editor), Is.Not.Null);
                    Assert.That(editableTextBox!.Text, Is.EqualTo("Water Utility"));
                    Assert.That(editableTextBox.Background, Is.EqualTo(Brushes.Transparent));
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void CellData_WhenRestrictedAutocompleteUsesDayTheme_RendersVisibleSelectedValueInClosedEditor()
        {
            var presenter = new GridCellPresenter
            {
                CellData = new GridCellSurfaceItem("row-1", "Owner")
                {
                    DisplayText = "Municipality",
                    EditingText = "Municipality",
                    IsEditing = true,
                    EditorKind = GridColumnEditorKind.Autocomplete,
                    EditorItemsMode = GridEditorItemsMode.RestrictToItems,
                    EditorItems = new[] { "Water Utility", "Municipality", "Parks Department" },
                },
            };

            var window = CreateStyledHostWindow(presenter);

            try
            {
                window.Show();
                FlushDispatcher(window.Dispatcher);

                var chrome = presenter.Content as Border;
                var editor = chrome?.Child as ComboBox;
                Assert.That(editor, Is.Not.Null);

                editor!.ApplyTemplate();
                FlushDispatcher(window.Dispatcher);

                var visibleText = GridSurfaceTestHost.ReadVisibleText(editor);
                var chevron = editor.Template?.FindName("PgEditingComboChevron", editor) as Path;
                var chromeScale = chrome?.RenderTransform as ScaleTransform;
                var bitmap = RenderElement(chrome!);
                var centerY = bitmap.PixelHeight / 2;
                var background = GetPixelColor(bitmap, Math.Min(10, bitmap.PixelWidth - 1), centerY);
                var contrastPixels = CountContrastingPixels(
                    bitmap,
                    new Int32Rect(
                        Math.Min(18, bitmap.PixelWidth - 1),
                        Math.Max(0, centerY - 7),
                        Math.Max(1, Math.Min(120, bitmap.PixelWidth - 44)),
                        Math.Max(1, Math.Min(14, bitmap.PixelHeight))),
                    background,
                    36);
                var chevronPixels = CountContrastingPixels(
                    bitmap,
                    new Int32Rect(
                        Math.Max(0, bitmap.PixelWidth - 28),
                        Math.Max(0, centerY - 8),
                        Math.Min(20, bitmap.PixelWidth),
                        Math.Max(1, Math.Min(16, bitmap.PixelHeight))),
                    background,
                    24);

                Assert.Multiple(() =>
                {
                    Assert.That(chevron, Is.Not.Null);
                    Assert.That(chromeScale, Is.Not.Null);
                    Assert.That(visibleText, Does.Contain("Municipality"));
                    Assert.That(editor.ActualHeight, Is.LessThanOrEqualTo(chrome.ActualHeight + 0.5d), "Combo editor should stay inside the editing chrome height.");
                    Assert.That(chromeScale!.ScaleY, Is.EqualTo(1.00d).Within(0.02d), "Closed combo editor should keep the editing chrome inside the cell instead of vertically inflating it.");
                    Assert.That(contrastPixels, Is.GreaterThan(35), "Closed combo editor should render visible glyph pixels for the selected text.");
                    Assert.That(chevronPixels, Is.GreaterThan(8), "Closed combo editor should render a visible drop-down chevron.");
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void CellData_WhenRestrictedAutocompleteUsesNightTheme_RendersClosedEditorWithoutLightInnerField()
        {
            var presenter = new GridCellPresenter
            {
                CellData = new GridCellSurfaceItem("row-1", "Owner")
                {
                    DisplayText = "Water Utility",
                    EditingText = "Water Utility",
                    IsEditing = true,
                    EditorKind = GridColumnEditorKind.Autocomplete,
                    EditorItemsMode = GridEditorItemsMode.RestrictToItems,
                    EditorItems = new[] { "Water Utility", "Municipality", "Parks Department" },
                },
            };

            var window = CreateStyledHostWindow(
                presenter,
                new Uri("pack://application:,,,/PhialeTech.Styles.Wpf;component/Themes/ThemeTokens.Night.xaml", UriKind.Absolute));

            try
            {
                window.Show();
                FlushDispatcher(window.Dispatcher);

                var chrome = presenter.Content as Border;
                var editor = chrome?.Child as ComboBox;
                Assert.That(editor, Is.Not.Null);

                editor!.ApplyTemplate();
                FlushDispatcher(window.Dispatcher);

                var visibleText = GridSurfaceTestHost.ReadVisibleText(editor);
                var bitmap = RenderElement(chrome!);
                var innerRect = new Int32Rect(
                    Math.Min(12, bitmap.PixelWidth - 1),
                    Math.Min(5, bitmap.PixelHeight - 1),
                    Math.Max(1, bitmap.PixelWidth - 36),
                    Math.Max(1, bitmap.PixelHeight - 10));
                var averageLuminance = ComputeAverageLuminance(bitmap, innerRect);

                Assert.Multiple(() =>
                {
                    Assert.That(editor.IsEditable, Is.False);
                    Assert.That(visibleText, Does.Contain("Water Utility"));
                    Assert.That(editor.Background, Is.EqualTo(Brushes.Transparent));
                    Assert.That(editor.BorderThickness, Is.EqualTo(new Thickness(0d)));
                    Assert.That(averageLuminance, Is.LessThan(150d), "Night combo editor should not render a bright inner field.");
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void CellData_WhenAutocompleteToggleIsClicked_OpensSuggestionsPopupReliably()
        {
            var presenter = new GridCellPresenter
            {
                CellData = new GridCellSurfaceItem("row-1", "Owner")
                {
                    DisplayText = "Water Utility",
                    EditingText = "Water Utility",
                    IsEditing = true,
                    EditorKind = GridColumnEditorKind.Autocomplete,
                    EditorItemsMode = GridEditorItemsMode.Suggestions,
                    EditorItems = new[] { "Water Utility", "Municipality", "Parks Department" },
                },
            };

            var window = CreateStyledHostWindow(presenter);

            try
            {
                window.Show();
                FlushDispatcher(window.Dispatcher);

                var editor = (presenter.Content as Border)?.Child as ComboBox;
                Assert.That(editor, Is.Not.Null);

                editor!.ApplyTemplate();
                var toggle = editor.Template?.FindName("DropDownToggle", editor) as ToggleButton;
                var popup = editor.Template?.FindName("PART_Popup", editor) as Popup;

                Assert.That(toggle, Is.Not.Null);
                Assert.That(popup, Is.Not.Null);

                ClickElement(toggle!);
                FlushDispatcher(window.Dispatcher);

                Assert.Multiple(() =>
                {
                    Assert.That(editor.IsDropDownOpen, Is.True, "Autocomplete suggestions should open on the first drop-down toggle click.");
                    Assert.That(popup!.IsOpen, Is.True, "Autocomplete popup should be visible after clicking the toggle.");
                    Assert.That(editor.ItemContainerGenerator.ContainerFromItem("Municipality"), Is.Not.Null, "Suggestions popup should materialize item containers when opened.");
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void CellData_WhenAutocompleteToggleSlotIsClickedAwayFromChevron_StillOpensSuggestionsPopup()
        {
            var presenter = new GridCellPresenter
            {
                CellData = new GridCellSurfaceItem("row-1", "Owner")
                {
                    DisplayText = "Water Utility",
                    EditingText = "Water Utility",
                    IsEditing = true,
                    EditorKind = GridColumnEditorKind.Autocomplete,
                    EditorItemsMode = GridEditorItemsMode.Suggestions,
                    EditorItems = new[] { "Water Utility", "Municipality", "Parks Department" },
                },
            };

            var window = CreateStyledHostWindow(presenter);

            try
            {
                window.Show();
                FlushDispatcher(window.Dispatcher);

                var editor = (presenter.Content as Border)?.Child as ComboBox;
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

                Assert.That(hitElement, Is.Not.Null, "Autocomplete toggle slot should expose a clickable hit target away from the chevron glyph.");
                Assert.That(IsDescendantOf(hitElement, toggle), Is.True, "Autocomplete toggle slot should hit-test to the toggle chrome, not only the chevron path.");

                ClickElement(toggle);
                FlushDispatcher(window.Dispatcher);

                Assert.Multiple(() =>
                {
                    Assert.That(editor.IsDropDownOpen, Is.True, "Autocomplete suggestions should open when clicking inside the toggle slot away from the chevron.");
                    Assert.That(popup!.IsOpen, Is.True, "Autocomplete popup should become visible when the toggle slot is clicked away from the chevron.");
                    Assert.That(editor.ItemContainerGenerator.ContainerFromItem("Municipality"), Is.Not.Null, "Suggestions popup should materialize item containers when opened from the wider toggle hit area.");
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void CellData_WhenAutocompleteSelectionTemporarilyClearsText_DoesNotOverwriteSelectedValue()
        {
            var presenter = new GridCellPresenter
            {
                CellData = new GridCellSurfaceItem("row-1", "Owner")
                {
                    DisplayText = "Water Utility",
                    EditingText = "Water Utility",
                    IsEditing = true,
                    EditorKind = GridColumnEditorKind.Autocomplete,
                    EditorItems = new[] { "Water Utility", "Municipality", "Parks Department" },
                },
            };

            var raisedValues = new System.Collections.Generic.List<string>();
            presenter.EditingTextChanged += (_, args) => raisedValues.Add(args.Text);

            Assert.That(presenter.Content, Is.TypeOf<Border>());
            var editor = (presenter.Content as Border)?.Child as ComboBox;
            Assert.That(editor, Is.Not.Null);

            editor!.SelectedItem = "Municipality";
            editor.Text = string.Empty;

            Assert.That(raisedValues, Does.Not.Contain(string.Empty));
            Assert.That(raisedValues[raisedValues.Count - 1], Is.EqualTo("Municipality"));
        }

        [Test]
        public void CellData_WhenDateEditorIsRequested_RendersDatePickerAndRaisesSelectionChanges()
        {
            var presenter = new GridCellPresenter
            {
                CellData = new GridCellSurfaceItem("row-1", "LastInspection")
                {
                    DisplayText = "2025-01-16",
                    EditingText = "2025-01-16",
                    RawValue = new DateTime(2025, 1, 16),
                    IsEditing = true,
                    EditorKind = GridColumnEditorKind.DatePicker,
                },
            };

            GridCellEditingTextChangedEventArgs raisedArgs = null;
            presenter.EditingTextChanged += (_, args) => raisedArgs = args;
            var window = CreateStyledHostWindow(presenter);

            try
            {
                window.Show();
                FlushDispatcher(window.Dispatcher);

                Assert.That(presenter.Content, Is.TypeOf<Border>());
                var editor = (presenter.Content as Border)?.Child as DatePicker;
                Assert.That(editor, Is.Not.Null);
                editor!.SelectedDate = new DateTime(2025, 2, 3);
                editor.ApplyTemplate();
                var textBox = editor.Template?.FindName("PART_TextBox", editor) as DatePickerTextBox;
                var button = editor.Template?.FindName("PART_Button", editor) as Button;
                textBox?.ApplyTemplate();
                button?.ApplyTemplate();

                Assert.Multiple(() =>
                {
                    Assert.That(editor.SelectedDate, Is.EqualTo(new DateTime(2025, 2, 3)));
                    Assert.That(editor.HorizontalContentAlignment, Is.EqualTo(HorizontalAlignment.Center));
                    Assert.That(editor.VerticalContentAlignment, Is.EqualTo(VerticalAlignment.Center));
                    Assert.That(textBox, Is.Not.Null);
                    Assert.That(button, Is.Not.Null);
                    Assert.That(button!.Content, Is.EqualTo("\uE787"));
                    Assert.That(button.FontFamily?.Source, Does.Contain("Segoe MDL2 Assets"));
                    Assert.That(raisedArgs, Is.Not.Null);
                    Assert.That(DateTime.TryParse(raisedArgs?.Text, out var raisedDate), Is.True);
                    Assert.That(raisedDate, Is.EqualTo(new DateTime(2025, 2, 3)));
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void CellData_WhenDateEditorUsesNightTheme_RendersDarkPopupCalendar()
        {
            var presenter = new GridCellPresenter
            {
                CellData = new GridCellSurfaceItem("row-1", "LastInspection")
                {
                    DisplayText = "2025-12-22",
                    EditingText = "2025-12-22",
                    RawValue = new DateTime(2025, 12, 22),
                    IsEditing = true,
                    EditorKind = GridColumnEditorKind.DatePicker,
                },
            };

            var window = CreateStyledHostWindow(
                presenter,
                new Uri("pack://application:,,,/PhialeTech.Styles.Wpf;component/Themes/ThemeTokens.Night.xaml", UriKind.Absolute));

            try
            {
                window.Show();
                FlushDispatcher(window.Dispatcher);

                var editor = (presenter.Content as Border)?.Child as DatePicker;
                Assert.That(editor, Is.Not.Null);

                editor!.ApplyTemplate();
                editor.IsDropDownOpen = true;
                FlushDispatcher(window.Dispatcher);

                var popup = editor.Template?.FindName("PART_Popup", editor) as Popup;
                var popupChild = popup?.Child as FrameworkElement;
                var popupCalendar = popupChild as WpfCalendar ?? (popupChild == null ? null : GridSurfaceTestHost.FindDescendant<WpfCalendar>(popupChild));
                Assert.That(popupChild, Is.Not.Null);

                popupCalendar?.ApplyTemplate();
                var calendarItem = popupCalendar?.Template.FindName("PART_CalendarItem", popupCalendar) as CalendarItem;
                calendarItem?.ApplyTemplate();
                var sharedCalendarStyle = FindResourceInDictionaryTree<Style>(window.Resources, "Calendar.Shared.Style");
                var sharedDayButtonStyle = FindResourceInDictionaryTree<Style>(window.Resources, "Calendar.Shared.DayButtonStyle");
                var sharedMonthButtonStyle = FindResourceInDictionaryTree<Style>(window.Resources, "Calendar.Shared.MonthButtonStyle");
                var previousButton = calendarItem?.Template.FindName("PART_PreviousButton", calendarItem) as Button;
                var headerButton = calendarItem?.Template.FindName("PART_HeaderButton", calendarItem) as Button;
                var nextButton = calendarItem?.Template.FindName("PART_NextButton", calendarItem) as Button;
                var monthView = calendarItem?.Template.FindName("PART_MonthView", calendarItem) as Grid;
                var monthViewport = calendarItem?.Template.FindName("PART_MonthViewport", calendarItem) as Grid;
                var weekdayHeaders = monthView?.Children.OfType<TextBlock>().Take(7).ToArray() ?? Array.Empty<TextBlock>();
                var controlTitleFontSize = (double)window.TryFindResource("Text.ControlTitle.FontSize");
                var labelFontSize = (double)window.TryFindResource("Text.ControlLabel.FontSize");

                var popupBitmap = RenderElement(popupChild!);
                var averageLuminance = ComputeAverageLuminance(
                    popupBitmap,
                    new Int32Rect(
                        Math.Min(12, popupBitmap.PixelWidth - 1),
                        Math.Min(12, popupBitmap.PixelHeight - 1),
                        Math.Max(1, popupBitmap.PixelWidth - 24),
                        Math.Max(1, popupBitmap.PixelHeight - 24)));

                Assert.Multiple(() =>
                {
                    Assert.That(popupCalendar, Is.Not.Null);
                    Assert.That(calendarItem, Is.Not.Null);
                    Assert.That(sharedCalendarStyle, Is.Not.Null);
                    Assert.That(sharedDayButtonStyle, Is.Not.Null);
                    Assert.That(sharedMonthButtonStyle, Is.Not.Null);
                    Assert.That(popupCalendar!.Style, Is.SameAs(sharedCalendarStyle));
                    Assert.That(popupCalendar.CalendarDayButtonStyle, Is.SameAs(sharedDayButtonStyle));
                    Assert.That(popupCalendar.CalendarButtonStyle, Is.SameAs(sharedMonthButtonStyle));
                    Assert.That(calendarItem!.Template.Resources.Contains(CalendarItem.DayTitleTemplateResourceKey), Is.True);
                    Assert.That(previousButton, Is.Not.Null);
                    Assert.That(headerButton, Is.Not.Null);
                    Assert.That(nextButton, Is.Not.Null);
                    Assert.That(previousButton!.Style, Is.Not.Null, "Calendar previous navigation button should use non-stock chrome.");
                    Assert.That(headerButton!.Style, Is.Not.Null, "Calendar header button should use non-stock chrome.");
                    Assert.That(nextButton!.Style, Is.Not.Null, "Calendar next navigation button should use non-stock chrome.");
                    Assert.That(previousButton.ActualWidth, Is.GreaterThanOrEqualTo(36d), "Calendar previous navigation button should keep the larger shared hit area.");
                    Assert.That(nextButton.ActualWidth, Is.GreaterThanOrEqualTo(36d), "Calendar next navigation button should keep the larger shared hit area.");
                    Assert.That(headerButton.FontSize, Is.EqualTo(controlTitleFontSize).Within(0.01d), "Calendar month/year caption should resolve from the shared control-title token.");
                    Assert.That(headerButton.FontFamily?.Source, Does.Contain("Bahnschrift"));
                    Assert.That(headerButton.Content?.ToString(), Is.Not.Empty, "Calendar header should expose a readable month/year caption.");
                    Assert.That(ComputeBrushLuminance(previousButton.Foreground), Is.GreaterThan(140d), "Calendar previous navigation glyph should remain readable in night theme.");
                    Assert.That(ComputeBrushLuminance(headerButton.Foreground), Is.GreaterThan(180d), "Calendar month/year caption should remain readable in night theme.");
                    Assert.That(ComputeBrushLuminance(nextButton.Foreground), Is.GreaterThan(140d), "Calendar next navigation glyph should remain readable in night theme.");
                    Assert.That(weekdayHeaders.Length, Is.EqualTo(7), "Calendar should render seven weekday labels in month mode.");
                    Assert.That(weekdayHeaders.All(header => ComputeBrushLuminance(header.Foreground) > 150d), Is.True, "Calendar weekday labels should remain readable in night theme.");
                    Assert.That(weekdayHeaders.All(header => Math.Abs(header.FontSize - labelFontSize) < 0.01d), Is.True, "Calendar weekday labels should resolve from the shared label token.");
                    Assert.That(weekdayHeaders.All(header => header.FontFamily?.Source.Contains("Bahnschrift") == true), Is.True);
                    Assert.That(monthViewport, Is.Not.Null);
                    Assert.That(monthViewport!.ClipToBounds, Is.False, "Calendar month viewport should not clip the edge day capsule.");
                    Assert.That(averageLuminance, Is.LessThan(180d), "Night date picker popup should not render as a bright day calendar.");
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void CellData_WhenDateEditorSelectsFirstColumnDay_ShouldKeepSelectionPlateFullyVisible()
        {
            var previousCulture = CultureInfo.CurrentCulture;
            var previousUiCulture = CultureInfo.CurrentUICulture;
            CultureInfo.CurrentCulture = new CultureInfo("pl-PL");
            CultureInfo.CurrentUICulture = new CultureInfo("pl-PL");

            try
            {
                var presenter = new GridCellPresenter
                {
                    CellData = new GridCellSurfaceItem("row-1", "LastInspection")
                    {
                        DisplayText = "2026-06-01",
                        EditingText = "2026-06-01",
                        RawValue = new DateTime(2026, 6, 1),
                        IsEditing = true,
                        EditorKind = GridColumnEditorKind.DatePicker,
                    },
                };

                var window = CreateStyledHostWindow(
                    presenter,
                    new Uri("pack://application:,,,/PhialeTech.Styles.Wpf;component/Themes/ThemeTokens.Night.xaml", UriKind.Absolute));

                try
                {
                    window.Show();
                    FlushDispatcher(window.Dispatcher);

                    var editor = (presenter.Content as Border)?.Child as DatePicker;
                    Assert.That(editor, Is.Not.Null);

                    editor!.ApplyTemplate();
                    editor.IsDropDownOpen = true;
                    FlushDispatcher(window.Dispatcher);

                    var popup = editor.Template?.FindName("PART_Popup", editor) as Popup;
                    var popupChild = popup?.Child as FrameworkElement;
                    var popupCalendar = popupChild as WpfCalendar ?? (popupChild == null ? null : GridSurfaceTestHost.FindDescendant<WpfCalendar>(popupChild));
                    popupCalendar?.ApplyTemplate();
                    var calendarItem = popupCalendar?.Template.FindName("PART_CalendarItem", popupCalendar) as CalendarItem;
                    calendarItem?.ApplyTemplate();
                    var monthView = calendarItem?.Template.FindName("PART_MonthView", calendarItem) as Grid;
                    var selectedDayButton = monthView?.Children
                        .OfType<CalendarDayButton>()
                        .FirstOrDefault(dayButton => dayButton.IsSelected);

                    Assert.That(popupChild, Is.Not.Null);
                    Assert.That(selectedDayButton, Is.Not.Null);

                    selectedDayButton!.UpdateLayout();
                    var selectedBounds = selectedDayButton
                        .TransformToAncestor(popupChild!)
                        .TransformBounds(new Rect(0d, 0d, selectedDayButton.ActualWidth, selectedDayButton.ActualHeight));
                    var popupBitmap = RenderElement(popupChild!);
                    var popupBackground = ((SolidColorBrush)popupCalendar!.Background).Color;
                    var leftContrast = CountContrastingPixels(
                        popupBitmap,
                        new Int32Rect(
                            (int)Math.Floor(selectedBounds.Left),
                            (int)Math.Floor(selectedBounds.Top) + 3,
                            5,
                            Math.Max(1, (int)Math.Ceiling(selectedBounds.Height) - 6)),
                        popupBackground,
                        12);
                    var rightContrast = CountContrastingPixels(
                        popupBitmap,
                        new Int32Rect(
                            (int)Math.Ceiling(selectedBounds.Right) - 5,
                            (int)Math.Floor(selectedBounds.Top) + 3,
                            5,
                            Math.Max(1, (int)Math.Ceiling(selectedBounds.Height) - 6)),
                        popupBackground,
                        12);

                    Assert.Multiple(() =>
                    {
                        Assert.That(selectedBounds.Left, Is.GreaterThan(0d), "Selected first-column day should stay inset from the popup edge.");
                        Assert.That(leftContrast, Is.GreaterThan(8), "Selected day plate should render visible fill/stroke pixels on its left edge.");
                        Assert.That(rightContrast, Is.GreaterThan(8), "Selected day plate should render visible fill/stroke pixels on its right edge.");
                        Assert.That(leftContrast, Is.GreaterThanOrEqualTo((int)Math.Floor(rightContrast * 0.7d)), "Selected day plate should not be visibly clipped on the left edge.");
                    });
                }
                finally
                {
                    window.Close();
                }
            }
            finally
            {
                CultureInfo.CurrentCulture = previousCulture;
                CultureInfo.CurrentUICulture = previousUiCulture;
            }
        }

        [Test]
        public void CellData_WhenDateEditorSelectsLastColumnDay_ShouldKeepSelectionPlateFullyVisible()
        {
            var previousCulture = CultureInfo.CurrentCulture;
            var previousUiCulture = CultureInfo.CurrentUICulture;
            CultureInfo.CurrentCulture = new CultureInfo("pl-PL");
            CultureInfo.CurrentUICulture = new CultureInfo("pl-PL");

            try
            {
                var presenter = new GridCellPresenter
                {
                    CellData = new GridCellSurfaceItem("row-1", "LastInspection")
                    {
                        DisplayText = "2025-01-26",
                        EditingText = "2025-01-26",
                        RawValue = new DateTime(2025, 1, 26),
                        IsEditing = true,
                        EditorKind = GridColumnEditorKind.DatePicker,
                    },
                };

                var window = CreateStyledHostWindow(
                    presenter,
                    new Uri("pack://application:,,,/PhialeTech.Styles.Wpf;component/Themes/ThemeTokens.Night.xaml", UriKind.Absolute));

                try
                {
                    window.Show();
                    FlushDispatcher(window.Dispatcher);

                    var editor = (presenter.Content as Border)?.Child as DatePicker;
                    Assert.That(editor, Is.Not.Null);

                    editor!.ApplyTemplate();
                    editor.IsDropDownOpen = true;
                    FlushDispatcher(window.Dispatcher);

                    var popup = editor.Template?.FindName("PART_Popup", editor) as Popup;
                    var popupChild = popup?.Child as FrameworkElement;
                    var popupCalendar = popupChild as WpfCalendar ?? (popupChild == null ? null : GridSurfaceTestHost.FindDescendant<WpfCalendar>(popupChild));
                    popupCalendar?.ApplyTemplate();
                    var calendarItem = popupCalendar?.Template.FindName("PART_CalendarItem", popupCalendar) as CalendarItem;
                    calendarItem?.ApplyTemplate();
                    var monthView = calendarItem?.Template.FindName("PART_MonthView", calendarItem) as Grid;
                    var selectedDayButton = monthView?.Children
                        .OfType<CalendarDayButton>()
                        .FirstOrDefault(dayButton => dayButton.IsSelected);

                    Assert.That(popupChild, Is.Not.Null);
                    Assert.That(selectedDayButton, Is.Not.Null);

                    selectedDayButton!.UpdateLayout();
                    var selectedBounds = selectedDayButton
                        .TransformToAncestor(popupChild!)
                        .TransformBounds(new Rect(0d, 0d, selectedDayButton.ActualWidth, selectedDayButton.ActualHeight));
                    var popupBitmap = RenderElement(popupChild!);
                    var popupBackground = ((SolidColorBrush)popupCalendar!.Background).Color;
                    var leftContrast = CountContrastingPixels(
                        popupBitmap,
                        new Int32Rect(
                            (int)Math.Floor(selectedBounds.Left),
                            (int)Math.Floor(selectedBounds.Top) + 3,
                            5,
                            Math.Max(1, (int)Math.Ceiling(selectedBounds.Height) - 6)),
                        popupBackground,
                        12);
                    var rightContrast = CountContrastingPixels(
                        popupBitmap,
                        new Int32Rect(
                            (int)Math.Ceiling(selectedBounds.Right) - 5,
                            (int)Math.Floor(selectedBounds.Top) + 3,
                            5,
                            Math.Max(1, (int)Math.Ceiling(selectedBounds.Height) - 6)),
                        popupBackground,
                        12);

                    Assert.Multiple(() =>
                    {
                        Assert.That(selectedBounds.Right, Is.LessThan(popupChild!.ActualWidth), "Selected last-column day should stay inset from the popup edge.");
                        Assert.That(leftContrast, Is.GreaterThan(8), "Selected day plate should render visible fill/stroke pixels on its left edge.");
                        Assert.That(rightContrast, Is.GreaterThan(8), "Selected day plate should render visible fill/stroke pixels on its right edge.");
                        Assert.That(rightContrast, Is.GreaterThanOrEqualTo((int)Math.Floor(leftContrast * 0.7d)), "Selected day plate should not be visibly clipped on the right edge.");
                    });
                }
                finally
                {
                    window.Close();
                }
            }
            finally
            {
                CultureInfo.CurrentCulture = previousCulture;
                CultureInfo.CurrentUICulture = previousUiCulture;
            }
        }

        [Test]
        public void CellData_WhenMaskedEditorIsRequested_AppliesMaskPatternToTextBox()
        {
            var presenter = new GridCellPresenter
            {
                CellData = new GridCellSurfaceItem("row-1", "ScaleHint")
                {
                    DisplayText = "1000",
                    EditingText = "1000",
                    IsEditing = true,
                    EditorKind = GridColumnEditorKind.MaskedText,
                    EditMask = "^[0-9]{0,6}$",
                },
            };

            Assert.That(presenter.Content, Is.TypeOf<Border>());
            var editor = (presenter.Content as Border)?.Child as TextBox;
            Assert.That(editor, Is.Not.Null);
            var maskProperty = typeof(PhialeTech.PhialeGrid.Wpf.Controls.Editing.MaskedTextBoxBehavior)
                .GetMethod("GetMaskPattern", BindingFlags.Public | BindingFlags.Static);

            Assert.That(maskProperty, Is.Not.Null);
            Assert.That(maskProperty.Invoke(null, new object[] { editor }), Is.EqualTo("^[0-9]{0,6}$"));
        }

        [Test]
        public void CellData_WhenCheckBoxEditorIsRequested_RendersCheckBoxAndRaisesToggleChange()
        {
            var presenter = new GridCellPresenter
            {
                CellData = new GridCellSurfaceItem("row-1", "Visible")
                {
                    RawValue = true,
                    DisplayText = "True",
                    EditingText = "True",
                    IsEditing = true,
                    EditorKind = GridColumnEditorKind.CheckBox,
                    ValueKind = "Boolean",
                },
            };

            GridCellEditingTextChangedEventArgs raisedArgs = null;
            presenter.EditingTextChanged += (_, args) => raisedArgs = args;

            Assert.That(presenter.Content, Is.TypeOf<Border>());
            var editor = (presenter.Content as Border)?.Child as CheckBox;
            Assert.That(editor, Is.Not.Null);
            editor.IsChecked = false;

            Assert.Multiple(() =>
            {
                Assert.That(raisedArgs, Is.Not.Null);
                Assert.That(raisedArgs.Text, Is.EqualTo(bool.FalseString));
                Assert.That(raisedArgs.ChangeKind, Is.EqualTo(UniversalEditorValueChangeKind.SelectionCommitted));
            });
        }

        [Test]
        public void CellData_WhenCheckBoxEditingTextDiffersFromRawValue_PrefersEditingText()
        {
            var presenter = new GridCellPresenter
            {
                CellData = new GridCellSurfaceItem("row-1", "Visible")
                {
                    RawValue = true,
                    DisplayText = "True",
                    EditingText = "False",
                    IsEditing = true,
                    EditorKind = GridColumnEditorKind.CheckBox,
                    ValueKind = "Boolean",
                },
            };

            var editor = (presenter.Content as Border)?.Child as CheckBox;
            Assert.That(editor, Is.Not.Null);
            Assert.That(editor!.IsChecked, Is.False);
        }

        [Test]
        public void CellData_WhenCheckBoxEditorIsRequested_UsesCompactEditingChromeScale()
        {
            var presenter = new GridCellPresenter
            {
                CellData = new GridCellSurfaceItem("row-1", "Visible")
                {
                    RawValue = true,
                    DisplayText = "True",
                    EditingText = "True",
                    IsEditing = true,
                    EditorKind = GridColumnEditorKind.CheckBox,
                    ValueKind = "Boolean",
                },
            };

            var chrome = presenter.Content as Border;
            Assert.That(chrome, Is.Not.Null);
            Assert.That(chrome?.RenderTransform, Is.TypeOf<System.Windows.Media.ScaleTransform>());
            Assert.Multiple(() =>
            {
                Assert.That(chrome!.Padding, Is.EqualTo(new Thickness(2d, 0d, 2d, 0d)));
                Assert.That(((System.Windows.Media.ScaleTransform)chrome!.RenderTransform).ScaleX, Is.EqualTo(1.00d).Within(0.02d));
                Assert.That(((System.Windows.Media.ScaleTransform)chrome.RenderTransform).ScaleY, Is.EqualTo(1.20d).Within(0.02d));
            });
        }

        [Test]
        public void CellData_WhenCheckBoxEditorIsRequested_WithThemeResources_UsesLargerEditingGlyph()
        {
            var presenter = new GridCellPresenter
            {
                CellData = new GridCellSurfaceItem("row-1", "Visible")
                {
                    RawValue = true,
                    DisplayText = "True",
                    EditingText = "True",
                    IsEditing = true,
                    EditorKind = GridColumnEditorKind.CheckBox,
                    ValueKind = "Boolean",
                },
            };

            var window = CreateStyledHostWindow(presenter);

            try
            {
                window.Show();
                FlushDispatcher(window.Dispatcher);

                var editor = (presenter.Content as Border)?.Child as CheckBox;
                Assert.That(editor, Is.Not.Null);
                editor!.ApplyTemplate();

                var box = editor.Template?.FindName("PgEditingCheckBoxBox", editor) as Border;
                var check = editor.Template?.FindName("PgEditingCheckBoxCheck", editor) as Path;

                Assert.Multiple(() =>
                {
                    Assert.That(editor.Width, Is.EqualTo(24d));
                    Assert.That(editor.Height, Is.EqualTo(24d));
                    Assert.That(box, Is.Not.Null);
                    Assert.That(box!.Width, Is.EqualTo(20d));
                    Assert.That(box.Height, Is.EqualTo(20d));
                    Assert.That(check, Is.Not.Null);
                    Assert.That(check!.Visibility, Is.EqualTo(Visibility.Visible));
                });
            }
            finally
            {
                window.Close();
            }
        }

        private static void FlushDispatcher(Dispatcher dispatcher)
        {
            dispatcher.Invoke(() => { }, DispatcherPriority.Background);
            dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
        }

        private static RenderTargetBitmap RenderElement(FrameworkElement element)
        {
            Assert.That(element, Is.Not.Null);

            element.UpdateLayout();
            FlushDispatcher(element.Dispatcher);

            var width = Math.Max(1, (int)Math.Ceiling(element.ActualWidth));
            var height = Math.Max(1, (int)Math.Ceiling(element.ActualHeight));
            var bitmap = new RenderTargetBitmap(width, height, 96d, 96d, PixelFormats.Pbgra32);
            bitmap.Render(element);
            return bitmap;
        }

        private static Color GetPixelColor(BitmapSource bitmap, int x, int y)
        {
            var safeX = Math.Max(0, Math.Min(bitmap.PixelWidth - 1, x));
            var safeY = Math.Max(0, Math.Min(bitmap.PixelHeight - 1, y));
            var pixels = new byte[4];
            bitmap.CopyPixels(new Int32Rect(safeX, safeY, 1, 1), pixels, 4, 0);
            return Color.FromArgb(pixels[3], pixels[2], pixels[1], pixels[0]);
        }

        private static int CountContrastingPixels(BitmapSource bitmap, Int32Rect rect, Color reference, byte minimumDelta)
        {
            var safeRect = NormalizeRect(bitmap, rect);
            var pixelBytes = new byte[safeRect.Width * safeRect.Height * 4];
            var stride = safeRect.Width * 4;
            bitmap.CopyPixels(safeRect, pixelBytes, stride, 0);

            var count = 0;
            for (var offset = 0; offset < pixelBytes.Length; offset += 4)
            {
                var blue = pixelBytes[offset];
                var green = pixelBytes[offset + 1];
                var red = pixelBytes[offset + 2];
                var alpha = pixelBytes[offset + 3];
                if (alpha < 32)
                {
                    continue;
                }

                var delta = Math.Abs(red - reference.R) + Math.Abs(green - reference.G) + Math.Abs(blue - reference.B);
                if (delta >= minimumDelta * 3)
                {
                    count++;
                }
            }

            return count;
        }

        private static double ComputeAverageLuminance(BitmapSource bitmap, Int32Rect rect)
        {
            var safeRect = NormalizeRect(bitmap, rect);
            var pixelBytes = new byte[safeRect.Width * safeRect.Height * 4];
            var stride = safeRect.Width * 4;
            bitmap.CopyPixels(safeRect, pixelBytes, stride, 0);

            var total = 0d;
            var count = 0;
            for (var offset = 0; offset < pixelBytes.Length; offset += 4)
            {
                var blue = pixelBytes[offset];
                var green = pixelBytes[offset + 1];
                var red = pixelBytes[offset + 2];
                var alpha = pixelBytes[offset + 3];
                if (alpha < 32)
                {
                    continue;
                }

                total += (0.2126d * red) + (0.7152d * green) + (0.0722d * blue);
                count++;
            }

            return count == 0 ? 0d : total / count;
        }

        private static int CountBrightPixels(BitmapSource bitmap, Int32Rect rect, double luminanceThreshold)
        {
            var safeRect = NormalizeRect(bitmap, rect);
            var pixelBytes = new byte[safeRect.Width * safeRect.Height * 4];
            var stride = safeRect.Width * 4;
            bitmap.CopyPixels(safeRect, pixelBytes, stride, 0);

            var brightPixelCount = 0;
            for (var offset = 0; offset < pixelBytes.Length; offset += 4)
            {
                var blue = pixelBytes[offset];
                var green = pixelBytes[offset + 1];
                var red = pixelBytes[offset + 2];
                var alpha = pixelBytes[offset + 3];
                if (alpha < 32)
                {
                    continue;
                }

                var luminance = (0.2126d * red) + (0.7152d * green) + (0.0722d * blue);
                if (luminance >= luminanceThreshold)
                {
                    brightPixelCount++;
                }
            }

            return brightPixelCount;
        }

        private static double ComputeBrushLuminance(Brush brush)
        {
            if (brush is not SolidColorBrush solidBrush)
            {
                return 0d;
            }

            var color = solidBrush.Color;
            return (0.2126d * color.R) + (0.7152d * color.G) + (0.0722d * color.B);
        }

        private static T FindResourceInDictionaryTree<T>(ResourceDictionary resources, object resourceKey)
        {
            if (resources == null || resourceKey == null)
            {
                return default;
            }

            if (resources.Contains(resourceKey) && resources[resourceKey] is T directResource)
            {
                return directResource;
            }

            foreach (var mergedDictionary in resources.MergedDictionaries)
            {
                var mergedResource = FindResourceInDictionaryTree<T>(mergedDictionary, resourceKey);
                if (mergedResource != null)
                {
                    return mergedResource;
                }
            }

            return default;
        }

        private static void ClickElement(UIElement element)
        {
            Assert.That(element, Is.Not.Null);
            if (element == null)
            {
                return;
            }

            element.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, Environment.TickCount, MouseButton.Left)
            {
                RoutedEvent = UIElement.PreviewMouseLeftButtonDownEvent,
                Source = element,
            });
            element.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, Environment.TickCount, MouseButton.Left)
            {
                RoutedEvent = UIElement.MouseLeftButtonDownEvent,
                Source = element,
            });
            element.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, Environment.TickCount, MouseButton.Left)
            {
                RoutedEvent = UIElement.PreviewMouseLeftButtonUpEvent,
                Source = element,
            });
            element.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, Environment.TickCount, MouseButton.Left)
            {
                RoutedEvent = UIElement.MouseLeftButtonUpEvent,
                Source = element,
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

        private static Int32Rect NormalizeRect(BitmapSource bitmap, Int32Rect rect)
        {
            var x = Math.Max(0, Math.Min(bitmap.PixelWidth - 1, rect.X));
            var y = Math.Max(0, Math.Min(bitmap.PixelHeight - 1, rect.Y));
            var width = Math.Max(1, Math.Min(rect.Width, bitmap.PixelWidth - x));
            var height = Math.Max(1, Math.Min(rect.Height, bitmap.PixelHeight - y));
            return new Int32Rect(x, y, width, height);
        }

        private static Window CreateStyledHostWindow(UIElement content, Uri themeTokensUri = null)
        {
            var resources = new ResourceDictionary();
            resources.MergedDictionaries.Add(new ResourceDictionary
            {
                Source = themeTokensUri ?? new Uri(
                    "pack://application:,,,/PhialeTech.Styles.Wpf;component/Themes/ThemeTokens.Day.xaml",
                    UriKind.Absolute)
            });
            resources.MergedDictionaries.Add(new ResourceDictionary
            {
                Source = new Uri(
                    "pack://application:,,,/PhialeTech.Styles.Wpf;component/Themes/PhialeGrid.Shared.xaml",
                    UriKind.Absolute)
            });
            resources.MergedDictionaries.Add(new ResourceDictionary
            {
                Source = new Uri(
                    "pack://application:,,,/PhialeTech.Styles.Wpf;component/Themes/PhialeGrid.Controls.xaml",
                    UriKind.Absolute)
            });

            return new Window
            {
                Width = 420,
                Height = 240,
                ShowInTaskbar = false,
                WindowStyle = WindowStyle.None,
                Resources = resources,
                Content = content,
            };
        }
    }
}
