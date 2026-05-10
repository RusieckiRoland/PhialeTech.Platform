using System;
using System.Globalization;
using NUnit.Framework;
using PhialeGrid.Core.Presentation;

namespace PhialeGis.Library.Tests.Grid
{
    public sealed class GridCurrentCellDescriptionBuilderTests
    {
        [Test]
        public void BuildDescription_ForCaptionRow_ShouldReturnCaption()
        {
            var builder = new GridCurrentCellDescriptionBuilder();

            var description = builder.BuildDescription(new GridCurrentCellDescriptorRequest
            {
                Kind = GridCurrentCellDescriptorKind.Caption,
                Caption = "Category: Roads (42)",
            });

            Assert.That(description, Is.EqualTo("Category: Roads (42)"));
        }

        [Test]
        public void BuildDescription_ForDataCell_ShouldFormatValue()
        {
            var builder = new GridCurrentCellDescriptionBuilder();

            var description = builder.BuildDescription(new GridCurrentCellDescriptorRequest
            {
                Kind = GridCurrentCellDescriptorKind.Data,
                Header = "Last inspection",
                Value = new DateTime(2026, 3, 9),
            }, CultureInfo.InvariantCulture);

            Assert.That(description, Is.EqualTo("Last inspection: 2026-03-09"));
        }

        [Test]
        public void ValueFormatter_ForDecimal_ShouldUseNumericFormatting()
        {
            var formatted = GridValueFormatter.FormatDisplayValue(157.41m, CultureInfo.InvariantCulture);

            Assert.That(formatted, Is.EqualTo("157.41"));
        }
    }
}

