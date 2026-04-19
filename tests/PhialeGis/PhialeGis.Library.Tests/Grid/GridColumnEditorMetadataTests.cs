using NUnit.Framework;
using PhialeGrid.Core.Columns;
using PhialeGrid.Core.Validation;

namespace PhialeGis.Library.Tests.Grid
{
    [TestFixture]
    public sealed class GridColumnEditorMetadataTests
    {
        [Test]
        public void GridLayoutState_ShouldPreserveEditorMetadata()
        {
            var sourceColumn = new GridColumnDefinition(
                "Status",
                "Status",
                width: 150d,
                displayIndex: 0,
                valueType: typeof(string),
                isEditable: true,
                editorKind: GridColumnEditorKind.Combo,
                editorItems: new[] { "Active", "Retired" },
                editMask: string.Empty,
                validationConstraints: new LookupValidationConstraints(new object[] { "Active", "Retired" }, required: true),
                editorItemsMode: GridEditorItemsMode.RestrictToItems);

            var layoutState = new GridLayoutState(new[] { sourceColumn });

            var clonedColumn = layoutState.Columns[0];

            Assert.Multiple(() =>
            {
                Assert.That(clonedColumn.EditorKind, Is.EqualTo(GridColumnEditorKind.Combo));
                Assert.That(clonedColumn.EditorItems, Is.EqualTo(new[] { "Active", "Retired" }));
                Assert.That(clonedColumn.EditorItemsMode, Is.EqualTo(GridEditorItemsMode.RestrictToItems));
                Assert.That(clonedColumn.EditMask, Is.EqualTo(string.Empty));
                Assert.That(clonedColumn.ValidationConstraints, Is.TypeOf<LookupValidationConstraints>());
            });
        }
    }
}
