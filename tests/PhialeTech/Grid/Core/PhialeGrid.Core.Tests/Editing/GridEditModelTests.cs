using System.Collections.Generic;
using NUnit.Framework;
using PhialeGrid.Core.Editing;

namespace PhialeGrid.Core.Tests.Editing
{
    [TestFixture]
    public class GridEditModelTests
    {
        [Test]
        public void StartSession_LoadsCurrentValueAndClearsPreviousValidationErrors()
        {
            var accessor = new TestCellAccessor()
                .Add("row-1", "col-1", "Alpha");
            var sut = new GridEditModel(accessor, new RequiredValueValidator());

            sut.StartSession("row-1", "col-1", GridEditStartMode.DoubleClick);
            sut.Clear();
            Assert.That(sut.Commit(), Is.False);

            var started = sut.StartSession("row-1", "col-1", GridEditStartMode.Programmatic);

            Assert.Multiple(() =>
            {
                Assert.That(started, Is.True);
                Assert.That(sut.State.IsInEditMode, Is.True);
                Assert.That(sut.State.CurrentSession, Is.Not.Null);
                Assert.That(sut.State.CurrentSession.OriginalValue, Is.EqualTo("Alpha"));
                Assert.That(sut.State.CurrentSession.EditingText, Is.EqualTo("Alpha"));
                Assert.That(sut.State.ValidationError, Is.Null);
                Assert.That(sut.ValidationErrors.ContainsKey("row-1_col-1"), Is.False);
            });
        }

        [Test]
        public void Commit_WhenValidationSucceeds_WritesValueAndClosesSession()
        {
            var accessor = new TestCellAccessor()
                .Add("row-1", "col-1", "Alpha");
            var sut = new GridEditModel(accessor, new RequiredValueValidator());

            sut.StartSession("row-1", "col-1", GridEditStartMode.DoubleClick);
            sut.Clear();
            sut.AppendText("Beta");

            var committed = sut.Commit();

            Assert.Multiple(() =>
            {
                Assert.That(committed, Is.True);
                Assert.That(accessor.GetValue("row-1", "col-1"), Is.EqualTo("Beta"));
                Assert.That(sut.State.IsInEditMode, Is.False);
                Assert.That(sut.State.CurrentSession, Is.Null);
                Assert.That(sut.State.ValidationError, Is.Null);
            });
        }

        [Test]
        public void Commit_WhenValidationFails_PreservesSessionAndMarksCellAsInvalid()
        {
            var accessor = new TestCellAccessor()
                .Add("row-1", "col-1", "Alpha");
            var sut = new GridEditModel(accessor, new RequiredValueValidator());

            sut.StartSession("row-1", "col-1", GridEditStartMode.DoubleClick);
            sut.Clear();

            var committed = sut.Commit();

            Assert.Multiple(() =>
            {
                Assert.That(committed, Is.False);
                Assert.That(accessor.GetValue("row-1", "col-1"), Is.EqualTo("Alpha"));
                Assert.That(sut.State.IsInEditMode, Is.True);
                Assert.That(sut.State.CurrentSession, Is.Not.Null);
                Assert.That(sut.State.ValidationError, Is.EqualTo("Value is required."));
                Assert.That(sut.ValidationErrors.ContainsKey("row-1_col-1"), Is.True);
            });
        }

        [Test]
        public void Cancel_DropsTemporaryTextAndLeavesSourceValueUntouched()
        {
            var accessor = new TestCellAccessor()
                .Add("row-1", "col-1", "Alpha");
            var sut = new GridEditModel(accessor, new RequiredValueValidator());

            sut.StartSession("row-1", "col-1", GridEditStartMode.DoubleClick);
            sut.Clear();
            sut.AppendText("Gamma");

            var canceled = sut.Cancel();

            Assert.Multiple(() =>
            {
                Assert.That(canceled, Is.True);
                Assert.That(accessor.GetValue("row-1", "col-1"), Is.EqualTo("Alpha"));
                Assert.That(sut.State.IsInEditMode, Is.False);
                Assert.That(sut.State.CurrentSession, Is.Null);
                Assert.That(sut.State.ValidationError, Is.Null);
            });
        }

        [Test]
        public void Cancel_AfterValidationFailure_ClearsStoredValidationErrors()
        {
            var accessor = new TestCellAccessor()
                .Add("row-1", "col-1", "Alpha");
            var sut = new GridEditModel(accessor, new RequiredValueValidator());

            sut.StartSession("row-1", "col-1", GridEditStartMode.DoubleClick);
            sut.Clear();
            Assert.That(sut.Commit(), Is.False);

            var canceled = sut.Cancel();

            Assert.Multiple(() =>
            {
                Assert.That(canceled, Is.True);
                Assert.That(sut.State.IsInEditMode, Is.False);
                Assert.That(sut.State.CurrentSession, Is.Null);
                Assert.That(sut.State.ValidationError, Is.Null);
                Assert.That(sut.ValidationErrors.ContainsKey("row-1_col-1"), Is.False);
            });
        }

        [Test]
        public void EditingTextChanges_RevalidateCurrentSessionAndClearErrorWhenValueBecomesValid()
        {
            var accessor = new TestCellAccessor()
                .Add("row-1", "col-1", "Alpha");
            var sut = new GridEditModel(accessor, new MinLengthValidator(3));

            sut.StartSession("row-1", "col-1", GridEditStartMode.DoubleClick);

            sut.Clear();

            Assert.Multiple(() =>
            {
                Assert.That(sut.State.ValidationError, Is.EqualTo("Value must be at least 3 characters."));
                Assert.That(sut.ValidationErrors.ContainsKey("row-1_col-1"), Is.True);
            });

            sut.AppendText("Ga");

            Assert.Multiple(() =>
            {
                Assert.That(sut.State.ValidationError, Is.EqualTo("Value must be at least 3 characters."));
                Assert.That(sut.ValidationErrors.ContainsKey("row-1_col-1"), Is.True);
            });

            sut.AppendText("m");

            Assert.Multiple(() =>
            {
                Assert.That(sut.State.ValidationError, Is.Null);
                Assert.That(sut.ValidationErrors.ContainsKey("row-1_col-1"), Is.False);
                Assert.That(sut.State.CurrentSession?.EditingText, Is.EqualTo("Gam"));
            });
        }

        private sealed class TestCellAccessor : IGridEditCellAccessor
        {
            private readonly Dictionary<string, object> _values = new Dictionary<string, object>();

            public TestCellAccessor Add(string rowKey, string columnKey, object value)
            {
                _values[rowKey + "_" + columnKey] = value;
                return this;
            }

            public bool TryGetValue(string rowKey, string columnKey, out object value)
            {
                return _values.TryGetValue(rowKey + "_" + columnKey, out value);
            }

            public void SetValue(string rowKey, string columnKey, object value)
            {
                _values[rowKey + "_" + columnKey] = value;
            }

            public object GetValue(string rowKey, string columnKey)
            {
                return _values[rowKey + "_" + columnKey];
            }
        }

        private sealed class RequiredValueValidator : IGridEditValidator
        {
            public IReadOnlyList<GridValidationError> Validate(string rowKey, string columnKey, object parsedValue, string editingText)
            {
                if (string.IsNullOrWhiteSpace(editingText))
                {
                    return new[] { new GridValidationError(columnKey, "Value is required.") };
                }

                return System.Array.Empty<GridValidationError>();
            }
        }

        private sealed class MinLengthValidator : IGridEditValidator
        {
            private readonly int _minLength;

            public MinLengthValidator(int minLength)
            {
                _minLength = minLength;
            }

            public IReadOnlyList<GridValidationError> Validate(string rowKey, string columnKey, object parsedValue, string editingText)
            {
                if ((editingText ?? string.Empty).Length < _minLength)
                {
                    return new[] { new GridValidationError(columnKey, "Value must be at least " + _minLength + " characters.") };
                }

                return System.Array.Empty<GridValidationError>();
            }
        }
    }
}

