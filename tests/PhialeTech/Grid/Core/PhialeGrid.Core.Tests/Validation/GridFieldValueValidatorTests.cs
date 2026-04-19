using System;
using NUnit.Framework;
using PhialeGrid.Core.Columns;
using PhialeGrid.Core.Validation;

namespace PhialeGrid.Core.Tests.Validation
{
    [TestFixture]
    public sealed class GridFieldValueValidatorTests
    {
        private readonly GridFieldValueValidator _sut = new GridFieldValueValidator();

        [Test]
        public void TextRequired_WhenEmpty_Fails()
        {
            var result = _sut.Validate(CreateContext("Name", typeof(string), new TextValidationConstraints(required: true), string.Empty, string.Empty));

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors[0].ErrorCode, Is.EqualTo("required"));
        }

        [Test]
        public void TextLengthAndPattern_WhenOutOfBounds_Fails()
        {
            var constraints = new TextValidationConstraints(minLength: 3, maxLength: 5, pattern: "^[A-Z]+$");

            var tooShort = _sut.Validate(CreateContext("Code", typeof(string), constraints, "AB", "AB"));
            var invalidPattern = _sut.Validate(CreateContext("Code", typeof(string), constraints, "AB12", "AB12"));
            var tooLong = _sut.Validate(CreateContext("Code", typeof(string), constraints, "ABCDEFG", "ABCDEFG"));

            Assert.Multiple(() =>
            {
                Assert.That(tooShort.IsValid, Is.False);
                Assert.That(tooShort.Errors[0].ErrorCode, Is.EqualTo("min-length"));
                Assert.That(invalidPattern.IsValid, Is.False);
                Assert.That(invalidPattern.Errors[0].ErrorCode, Is.EqualTo("pattern"));
                Assert.That(tooLong.IsValid, Is.False);
                Assert.That(tooLong.Errors[0].ErrorCode, Is.EqualTo("max-length"));
            });
        }

        [Test]
        public void Text_WhenValid_Passes()
        {
            var result = _sut.Validate(CreateContext("Name", typeof(string), new TextValidationConstraints(required: true, minLength: 3), "Alpha", "Alpha"));

            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void Integer_WhenParseFails_Fails()
        {
            var result = _sut.Validate(CreateContext("Scale", typeof(int), new IntegerValidationConstraints(required: true), "abc", "abc"));

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors[0].ErrorCode, Is.EqualTo("parse-integer"));
        }

        [Test]
        public void Integer_WhenOutsideRange_Fails()
        {
            var constraints = new IntegerValidationConstraints(minValue: 100, maxValue: 500);

            var belowMin = _sut.Validate(CreateContext("Scale", typeof(int), constraints, 50, "50"));
            var aboveMax = _sut.Validate(CreateContext("Scale", typeof(int), constraints, 900, "900"));

            Assert.Multiple(() =>
            {
                Assert.That(belowMin.IsValid, Is.False);
                Assert.That(belowMin.Errors[0].ErrorCode, Is.EqualTo("min-value"));
                Assert.That(aboveMax.IsValid, Is.False);
                Assert.That(aboveMax.Errors[0].ErrorCode, Is.EqualTo("max-value"));
            });
        }

        [Test]
        public void Integer_WhenValid_Passes()
        {
            var result = _sut.Validate(CreateContext("Scale", typeof(int), new IntegerValidationConstraints(required: true, minValue: 100, maxValue: 500), 250, "250"));

            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void Decimal_WhenParseFails_Fails()
        {
            var result = _sut.Validate(CreateContext("Budget", typeof(decimal), new DecimalValidationConstraints(required: true), "oops", "oops"));

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors[0].ErrorCode, Is.EqualTo("parse-decimal"));
        }

        [Test]
        public void Decimal_WhenRangeOrScaleInvalid_Fails()
        {
            var constraints = new DecimalValidationConstraints(minValue: 1.5m, maxValue: 10.5m, scale: 2, precision: 5);

            var belowMin = _sut.Validate(CreateContext("Budget", typeof(decimal), constraints, 1.2m, "1.2"));
            var aboveMax = _sut.Validate(CreateContext("Budget", typeof(decimal), constraints, 11.0m, "11.0"));
            var tooManyFractionalDigits = _sut.Validate(CreateContext("Budget", typeof(decimal), constraints, 2.345m, "2.345"));

            Assert.Multiple(() =>
            {
                Assert.That(belowMin.IsValid, Is.False);
                Assert.That(belowMin.Errors[0].ErrorCode, Is.EqualTo("min-value"));
                Assert.That(aboveMax.IsValid, Is.False);
                Assert.That(aboveMax.Errors[0].ErrorCode, Is.EqualTo("max-value"));
                Assert.That(tooManyFractionalDigits.IsValid, Is.False);
                Assert.That(tooManyFractionalDigits.Errors[0].ErrorCode, Is.EqualTo("scale"));
            });
        }

        [Test]
        public void Decimal_WhenValid_Passes()
        {
            var result = _sut.Validate(CreateContext("Budget", typeof(decimal), new DecimalValidationConstraints(required: true, minValue: 1m, maxValue: 10m, scale: 2, precision: 5), 3.25m, "3.25"));

            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void Date_WhenOutsideRange_Fails()
        {
            var constraints = new DateValidationConstraints(minDate: new DateTime(2025, 1, 1), maxDate: new DateTime(2025, 12, 31));

            var belowMin = _sut.Validate(CreateContext("LastInspection", typeof(DateTime), constraints, new DateTime(2024, 12, 31), "2024-12-31"));
            var aboveMax = _sut.Validate(CreateContext("LastInspection", typeof(DateTime), constraints, new DateTime(2026, 1, 1), "2026-01-01"));

            Assert.Multiple(() =>
            {
                Assert.That(belowMin.IsValid, Is.False);
                Assert.That(belowMin.Errors[0].ErrorCode, Is.EqualTo("min-date"));
                Assert.That(aboveMax.IsValid, Is.False);
                Assert.That(aboveMax.Errors[0].ErrorCode, Is.EqualTo("max-date"));
            });
        }

        [Test]
        public void Date_WhenValid_Passes()
        {
            var result = _sut.Validate(CreateContext("LastInspection", typeof(DateTime), new DateValidationConstraints(required: true, minDate: new DateTime(2025, 1, 1), maxDate: new DateTime(2025, 12, 31)), new DateTime(2025, 5, 4), "2025-05-04"));

            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void Lookup_WhenOutsideAllowedValues_Fails()
        {
            var result = _sut.Validate(CreateContext("Status", typeof(string), new LookupValidationConstraints(new object[] { "Active", "Retired" }, required: true), "Draft", "Draft", GridColumnEditorKind.Combo));

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors[0].ErrorCode, Is.EqualTo("allowed-values"));
        }

        [Test]
        public void Lookup_WhenAllowedValue_Passes()
        {
            var result = _sut.Validate(CreateContext("Status", typeof(string), new LookupValidationConstraints(new object[] { "Active", "Retired" }, required: true), "Active", "Active", GridColumnEditorKind.Combo));

            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void EditorItemsMode_WhenRestrictedAndTextIsOutsideItems_FailsEvenWithoutExplicitAllowedValuesConstraint()
        {
            var result = _sut.Validate(CreateContext(
                "Owner",
                typeof(string),
                new TextValidationConstraints(required: true, minLength: 3),
                "External vendor",
                "External vendor",
                GridColumnEditorKind.Autocomplete,
                new[] { "Municipality", "Parks Department" },
                GridEditorItemsMode.RestrictToItems));

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors[0].ErrorCode, Is.EqualTo("allowed-values"));
        }

        [Test]
        public void EditorItemsMode_WhenSuggestionsAreEnabled_DoesNotFailForTextOutsideItems()
        {
            var result = _sut.Validate(CreateContext(
                "Owner",
                typeof(string),
                new TextValidationConstraints(required: true, minLength: 3),
                "External vendor",
                "External vendor",
                GridColumnEditorKind.Autocomplete,
                new[] { "Municipality", "Parks Department" },
                GridEditorItemsMode.Suggestions));

            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void GridColumnDefinition_WhenConstraintTypeIsInvalid_FailsFast()
        {
            var exception = Assert.Throws<InvalidOperationException>(() => new GridColumnDefinition(
                "Scale",
                "Scale",
                valueType: typeof(string),
                validationConstraints: new IntegerValidationConstraints(minValue: 1)));

            Assert.That(exception.Message, Does.Contain("not compatible"));
        }

        private static GridFieldValidationContext CreateContext(
            string fieldId,
            Type valueType,
            GridFieldValidationConstraints constraints,
            object value,
            string editingText,
            GridColumnEditorKind editorKind = GridColumnEditorKind.Text,
            System.Collections.Generic.IReadOnlyList<string> editorItems = null,
            GridEditorItemsMode editorItemsMode = GridEditorItemsMode.Suggestions)
        {
            return new GridFieldValidationContext(fieldId, fieldId, valueType, constraints, value, editingText, editorKind, editorItems, editorItemsMode);
        }
    }
}
