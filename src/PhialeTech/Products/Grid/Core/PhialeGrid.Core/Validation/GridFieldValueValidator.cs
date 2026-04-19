using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using PhialeGrid.Core.Columns;

namespace PhialeGrid.Core.Validation
{
    public sealed class GridFieldValueValidator
    {
        public GridFieldValidationResult Validate(GridFieldValidationContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            GridFieldValidationConfigurationValidator.EnsureCompatible(
                context.FieldId,
                context.ValueType,
                context.EditorKind,
                context.Constraints);

            if (context.Constraints == null)
            {
                return new GridFieldValidationResult(Array.Empty<GridFieldValidationFailure>());
            }

            var failures = new List<GridFieldValidationFailure>();
            if (context.Constraints is TextValidationConstraints textConstraints)
            {
                ValidateText(context, textConstraints, failures);
            }
            else if (context.Constraints is IntegerValidationConstraints integerConstraints)
            {
                ValidateInteger(context, integerConstraints, failures);
            }
            else if (context.Constraints is DecimalValidationConstraints decimalConstraints)
            {
                ValidateDecimal(context, decimalConstraints, failures);
            }
            else if (context.Constraints is BooleanValidationConstraints booleanConstraints)
            {
                ValidateBoolean(context, booleanConstraints, failures);
            }
            else if (context.Constraints is DateValidationConstraints dateConstraints)
            {
                ValidateDate(context, dateConstraints, failures);
            }
            else if (context.Constraints is DateTimeValidationConstraints dateTimeConstraints)
            {
                ValidateDateTime(context, dateTimeConstraints, failures);
            }
            else if (context.Constraints is TimeValidationConstraints timeConstraints)
            {
                ValidateTime(context, timeConstraints, failures);
            }
            else if (context.Constraints is LookupValidationConstraints lookupConstraints)
            {
                ValidateLookup(context, lookupConstraints, failures);
            }

            ValidateEditorItemsMode(context, failures);

            return new GridFieldValidationResult(failures);
        }

        public GridFieldValidationResult Validate(
            GridColumnDefinition column,
            object value,
            string editingText = null)
        {
            if (column == null)
            {
                throw new ArgumentNullException(nameof(column));
            }

            return Validate(new GridFieldValidationContext(
                column.Id,
                column.Header,
                column.ValueType,
                column.ValidationConstraints,
                value,
                editingText,
                column.EditorKind,
                column.EditorItems,
                column.EditorItemsMode));
        }

        private static void ValidateText(GridFieldValidationContext context, TextValidationConstraints constraints, ICollection<GridFieldValidationFailure> failures)
        {
            var text = context.Value as string ?? context.EditingText ?? string.Empty;
            if (constraints.TrimBeforeValidation)
            {
                text = text.Trim();
            }

            if (string.IsNullOrWhiteSpace(text))
            {
                if (constraints.Required)
                {
                    failures.Add(CreateFailure(context, GridFieldValidationErrorCodes.Required, "{0} is required."));
                }

                return;
            }

            if (!constraints.AllowMultiline && (text.IndexOf('\r') >= 0 || text.IndexOf('\n') >= 0))
            {
                failures.Add(CreateFailure(context, GridFieldValidationErrorCodes.MultilineNotAllowed, "{0} cannot contain multiple lines."));
            }

            if (constraints.MinLength.HasValue && text.Length < constraints.MinLength.Value)
            {
                failures.Add(CreateFailure(context, GridFieldValidationErrorCodes.MinLength, "{0} must be at least {1} characters.", constraints.MinLength.Value));
            }

            if (constraints.MaxLength.HasValue && text.Length > constraints.MaxLength.Value)
            {
                failures.Add(CreateFailure(context, GridFieldValidationErrorCodes.MaxLength, "{0} cannot be longer than {1} characters.", constraints.MaxLength.Value));
            }

            if (!string.IsNullOrWhiteSpace(constraints.Pattern) && !Regex.IsMatch(text, constraints.Pattern))
            {
                failures.Add(CreateFailure(context, GridFieldValidationErrorCodes.Pattern, "{0} has an invalid format."));
            }

            if (constraints.AllowedValues.Count > 0 && !constraints.AllowedValues.Contains(text, StringComparer.Ordinal))
            {
                failures.Add(CreateFailure(context, GridFieldValidationErrorCodes.AllowedValues, "{0} must be one of the allowed values."));
            }
        }

        private static void ValidateInteger(GridFieldValidationContext context, IntegerValidationConstraints constraints, ICollection<GridFieldValidationFailure> failures)
        {
            if (IsEmpty(context))
            {
                if (constraints.Required)
                {
                    failures.Add(CreateFailure(context, GridFieldValidationErrorCodes.Required, "{0} is required."));
                }

                return;
            }

            if (!TryReadInteger(context.Value, out var value))
            {
                failures.Add(CreateFailure(context, GridFieldValidationErrorCodes.ParseInteger, "{0} must be a whole number."));
                return;
            }

            if (constraints.AllowNegative.HasValue && !constraints.AllowNegative.Value && value < 0)
            {
                failures.Add(CreateFailure(context, GridFieldValidationErrorCodes.NegativeNotAllowed, "{0} cannot be negative."));
            }

            if (constraints.AllowZero.HasValue && !constraints.AllowZero.Value && value == 0)
            {
                failures.Add(CreateFailure(context, GridFieldValidationErrorCodes.ZeroNotAllowed, "{0} cannot be zero."));
            }

            if (constraints.MinValue.HasValue && value < constraints.MinValue.Value)
            {
                failures.Add(CreateFailure(context, GridFieldValidationErrorCodes.MinValue, "{0} must be at least {1}.", constraints.MinValue.Value));
            }

            if (constraints.MaxValue.HasValue && value > constraints.MaxValue.Value)
            {
                failures.Add(CreateFailure(context, GridFieldValidationErrorCodes.MaxValue, "{0} cannot be greater than {1}.", constraints.MaxValue.Value));
            }

            if (constraints.Step.HasValue && value % constraints.Step.Value != 0)
            {
                failures.Add(CreateFailure(context, GridFieldValidationErrorCodes.Step, "{0} must follow step {1}.", constraints.Step.Value));
            }
        }

        private static void ValidateDecimal(GridFieldValidationContext context, DecimalValidationConstraints constraints, ICollection<GridFieldValidationFailure> failures)
        {
            if (IsEmpty(context))
            {
                if (constraints.Required)
                {
                    failures.Add(CreateFailure(context, GridFieldValidationErrorCodes.Required, "{0} is required."));
                }

                return;
            }

            if (!TryReadDecimal(context.Value, out var value))
            {
                failures.Add(CreateFailure(context, GridFieldValidationErrorCodes.ParseDecimal, "{0} must be a decimal number."));
                return;
            }

            if (constraints.AllowNegative.HasValue && !constraints.AllowNegative.Value && value < 0m)
            {
                failures.Add(CreateFailure(context, GridFieldValidationErrorCodes.NegativeNotAllowed, "{0} cannot be negative."));
            }

            if (constraints.AllowZero.HasValue && !constraints.AllowZero.Value && value == 0m)
            {
                failures.Add(CreateFailure(context, GridFieldValidationErrorCodes.ZeroNotAllowed, "{0} cannot be zero."));
            }

            if (constraints.MinValue.HasValue && value < constraints.MinValue.Value)
            {
                failures.Add(CreateFailure(context, GridFieldValidationErrorCodes.MinValue, "{0} must be at least {1}.", constraints.MinValue.Value));
            }

            if (constraints.MaxValue.HasValue && value > constraints.MaxValue.Value)
            {
                failures.Add(CreateFailure(context, GridFieldValidationErrorCodes.MaxValue, "{0} cannot be greater than {1}.", constraints.MaxValue.Value));
            }

            var precision = ResolvePrecision(context, value);
            var scale = ResolveScale(context, value);
            if (constraints.Scale.HasValue && scale > constraints.Scale.Value)
            {
                failures.Add(CreateFailure(context, GridFieldValidationErrorCodes.Scale, "{0} can have at most {1} fractional digits.", constraints.Scale.Value));
            }

            if (constraints.Precision.HasValue && precision > constraints.Precision.Value)
            {
                failures.Add(CreateFailure(context, GridFieldValidationErrorCodes.Precision, "{0} can have at most {1} digits.", constraints.Precision.Value));
            }

            if (constraints.Step.HasValue && decimal.Remainder(value, constraints.Step.Value) != 0m)
            {
                failures.Add(CreateFailure(context, GridFieldValidationErrorCodes.Step, "{0} must follow step {1}.", constraints.Step.Value));
            }
        }

        private static void ValidateBoolean(GridFieldValidationContext context, BooleanValidationConstraints constraints, ICollection<GridFieldValidationFailure> failures)
        {
            if (IsEmpty(context))
            {
                if (constraints.Required)
                {
                    failures.Add(CreateFailure(context, GridFieldValidationErrorCodes.Required, "{0} is required."));
                }

                return;
            }

            if (!(context.Value is bool))
            {
                failures.Add(CreateFailure(context, GridFieldValidationErrorCodes.ParseBoolean, "{0} must be true or false."));
            }
        }

        private static void ValidateDate(GridFieldValidationContext context, DateValidationConstraints constraints, ICollection<GridFieldValidationFailure> failures)
        {
            if (IsEmpty(context))
            {
                if (constraints.Required)
                {
                    failures.Add(CreateFailure(context, GridFieldValidationErrorCodes.Required, "{0} is required."));
                }

                return;
            }

            if (!TryReadDateTime(context.Value, out var value))
            {
                failures.Add(CreateFailure(context, GridFieldValidationErrorCodes.ParseDate, "{0} must be a valid date."));
                return;
            }

            var date = value.Date;
            if (constraints.MinDate.HasValue && date < constraints.MinDate.Value.Date)
            {
                failures.Add(CreateFailure(context, GridFieldValidationErrorCodes.MinDate, "{0} cannot be earlier than {1:d}.", constraints.MinDate.Value.Date));
            }

            if (constraints.MaxDate.HasValue && date > constraints.MaxDate.Value.Date)
            {
                failures.Add(CreateFailure(context, GridFieldValidationErrorCodes.MaxDate, "{0} cannot be later than {1:d}.", constraints.MaxDate.Value.Date));
            }

            if (constraints.AllowPast.HasValue && !constraints.AllowPast.Value && date < DateTime.Today)
            {
                failures.Add(CreateFailure(context, GridFieldValidationErrorCodes.PastNotAllowed, "{0} cannot be in the past."));
            }

            if (constraints.AllowFuture.HasValue && !constraints.AllowFuture.Value && date > DateTime.Today)
            {
                failures.Add(CreateFailure(context, GridFieldValidationErrorCodes.FutureNotAllowed, "{0} cannot be in the future."));
            }

            if (constraints.AllowedDaysOfWeek.Count > 0 && !constraints.AllowedDaysOfWeek.Contains(date.DayOfWeek))
            {
                failures.Add(CreateFailure(context, GridFieldValidationErrorCodes.AllowedDaysOfWeek, "{0} must fall on an allowed day of week."));
            }
        }

        private static void ValidateDateTime(GridFieldValidationContext context, DateTimeValidationConstraints constraints, ICollection<GridFieldValidationFailure> failures)
        {
            if (IsEmpty(context))
            {
                if (constraints.Required)
                {
                    failures.Add(CreateFailure(context, GridFieldValidationErrorCodes.Required, "{0} is required."));
                }

                return;
            }

            if (!TryReadDateTime(context.Value, out var value))
            {
                failures.Add(CreateFailure(context, GridFieldValidationErrorCodes.ParseDateTime, "{0} must be a valid date and time."));
                return;
            }

            if (constraints.MinDateTime.HasValue && value < constraints.MinDateTime.Value)
            {
                failures.Add(CreateFailure(context, GridFieldValidationErrorCodes.MinDateTime, "{0} cannot be earlier than {1:g}.", constraints.MinDateTime.Value));
            }

            if (constraints.MaxDateTime.HasValue && value > constraints.MaxDateTime.Value)
            {
                failures.Add(CreateFailure(context, GridFieldValidationErrorCodes.MaxDateTime, "{0} cannot be later than {1:g}.", constraints.MaxDateTime.Value));
            }
        }

        private static void ValidateTime(GridFieldValidationContext context, TimeValidationConstraints constraints, ICollection<GridFieldValidationFailure> failures)
        {
            if (IsEmpty(context))
            {
                if (constraints.Required)
                {
                    failures.Add(CreateFailure(context, GridFieldValidationErrorCodes.Required, "{0} is required."));
                }

                return;
            }

            if (!TryReadTime(context.Value, out var value))
            {
                failures.Add(CreateFailure(context, GridFieldValidationErrorCodes.ParseTime, "{0} must be a valid time."));
                return;
            }

            if (constraints.MinTime.HasValue && value < constraints.MinTime.Value)
            {
                failures.Add(CreateFailure(context, GridFieldValidationErrorCodes.MinTime, "{0} cannot be earlier than {1:c}.", constraints.MinTime.Value));
            }

            if (constraints.MaxTime.HasValue && value > constraints.MaxTime.Value)
            {
                failures.Add(CreateFailure(context, GridFieldValidationErrorCodes.MaxTime, "{0} cannot be later than {1:c}.", constraints.MaxTime.Value));
            }

            if (constraints.MinuteStep.HasValue)
            {
                var totalMinutes = (long)value.TotalMinutes;
                if (totalMinutes % constraints.MinuteStep.Value != 0)
                {
                    failures.Add(CreateFailure(context, GridFieldValidationErrorCodes.MinuteStep, "{0} must follow minute step {1}.", constraints.MinuteStep.Value));
                }
            }
        }

        private static void ValidateLookup(GridFieldValidationContext context, LookupValidationConstraints constraints, ICollection<GridFieldValidationFailure> failures)
        {
            if (IsEmpty(context))
            {
                if (constraints.Required || !constraints.AllowEmptySelection)
                {
                    failures.Add(CreateFailure(context, GridFieldValidationErrorCodes.Required, "{0} is required."));
                }

                return;
            }

            if (!MatchesAllowedValue(context.Value, context.EditingText, constraints))
            {
                failures.Add(CreateFailure(context, GridFieldValidationErrorCodes.AllowedValues, "{0} must be one of the allowed values."));
            }
        }

        private static void ValidateEditorItemsMode(GridFieldValidationContext context, ICollection<GridFieldValidationFailure> failures)
        {
            if (context.EditorItemsMode != GridEditorItemsMode.RestrictToItems ||
                context.EditorItems.Count == 0 ||
                HasExplicitAllowedValuesConstraint(context.Constraints) ||
                IsEmpty(context))
            {
                return;
            }

            var editingText = context.EditingText ?? Convert.ToString(context.Value, CultureInfo.CurrentCulture) ?? string.Empty;
            if (context.EditorItems.Contains(editingText, StringComparer.Ordinal))
            {
                return;
            }

            failures.Add(CreateFailure(context, GridFieldValidationErrorCodes.AllowedValues, "{0} must be one of the allowed values."));
        }

        private static bool HasExplicitAllowedValuesConstraint(GridFieldValidationConstraints constraints)
        {
            if (constraints is LookupValidationConstraints lookupConstraints)
            {
                return lookupConstraints.AllowedValues.Count > 0;
            }

            if (constraints is TextValidationConstraints textConstraints)
            {
                return textConstraints.AllowedValues.Count > 0;
            }

            return false;
        }

        private static bool MatchesAllowedValue(object value, string editingText, LookupValidationConstraints constraints)
        {
            foreach (var allowed in constraints.AllowedValues)
            {
                if (object.Equals(allowed, value))
                {
                    return true;
                }

                var candidate = Convert.ToString(value, CultureInfo.CurrentCulture);
                var allowedText = Convert.ToString(allowed, CultureInfo.CurrentCulture);
                if (string.IsNullOrEmpty(candidate))
                {
                    candidate = editingText ?? string.Empty;
                }

                if (constraints.StrictMatch)
                {
                    if (string.Equals(candidate, allowedText, StringComparison.Ordinal))
                    {
                        return true;
                    }
                }
                else if (string.Equals(candidate, allowedText, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsEmpty(GridFieldValidationContext context)
        {
            if (context.Value == null)
            {
                return string.IsNullOrWhiteSpace(context.EditingText);
            }

            if (context.Value is string text)
            {
                return string.IsNullOrWhiteSpace(text);
            }

            return false;
        }

        private static bool TryReadInteger(object value, out long result)
        {
            switch (value)
            {
                case byte typedByte:
                    result = typedByte;
                    return true;
                case sbyte typedSByte:
                    result = typedSByte;
                    return true;
                case short typedShort:
                    result = typedShort;
                    return true;
                case ushort typedUShort:
                    result = typedUShort;
                    return true;
                case int typedInt:
                    result = typedInt;
                    return true;
                case uint typedUInt:
                    result = typedUInt;
                    return true;
                case long typedLong:
                    result = typedLong;
                    return true;
                default:
                    result = 0L;
                    return false;
            }
        }

        private static bool TryReadDecimal(object value, out decimal result)
        {
            switch (value)
            {
                case decimal typedDecimal:
                    result = typedDecimal;
                    return true;
                case double typedDouble:
                    result = Convert.ToDecimal(typedDouble, CultureInfo.InvariantCulture);
                    return true;
                case float typedFloat:
                    result = Convert.ToDecimal(typedFloat, CultureInfo.InvariantCulture);
                    return true;
                case byte typedByte:
                    result = typedByte;
                    return true;
                case sbyte typedSByte:
                    result = typedSByte;
                    return true;
                case short typedShort:
                    result = typedShort;
                    return true;
                case ushort typedUShort:
                    result = typedUShort;
                    return true;
                case int typedInt:
                    result = typedInt;
                    return true;
                case uint typedUInt:
                    result = typedUInt;
                    return true;
                case long typedLong:
                    result = typedLong;
                    return true;
                default:
                    result = 0m;
                    return false;
            }
        }

        private static bool TryReadDateTime(object value, out DateTime result)
        {
            if (value is DateTime dateTime)
            {
                result = dateTime;
                return true;
            }

            if (value is DateTimeOffset offset)
            {
                result = offset.DateTime;
                return true;
            }

            result = default(DateTime);
            return false;
        }

        private static bool TryReadTime(object value, out TimeSpan result)
        {
            if (value is TimeSpan span)
            {
                result = span;
                return true;
            }

            if (value is DateTime dateTime)
            {
                result = dateTime.TimeOfDay;
                return true;
            }

            result = default(TimeSpan);
            return false;
        }

        private static int ResolvePrecision(GridFieldValidationContext context, decimal parsedValue)
        {
            if (!string.IsNullOrWhiteSpace(context.EditingText))
            {
                return context.EditingText.Count(char.IsDigit);
            }

            return parsedValue.ToString(CultureInfo.InvariantCulture).Count(char.IsDigit);
        }

        private static int ResolveScale(GridFieldValidationContext context, decimal parsedValue)
        {
            var source = !string.IsNullOrWhiteSpace(context.EditingText)
                ? context.EditingText.Trim()
                : parsedValue.ToString(CultureInfo.InvariantCulture);
            var separatorIndex = Math.Max(source.LastIndexOf('.'), source.LastIndexOf(','));
            if (separatorIndex < 0 || separatorIndex >= source.Length - 1)
            {
                return 0;
            }

            return source.Substring(separatorIndex + 1).Count(char.IsDigit);
        }

        private static GridFieldValidationFailure CreateFailure(GridFieldValidationContext context, string errorCode, string messageFormat, params object[] arguments)
        {
            var args = new object[(arguments?.Length ?? 0) + 1];
            args[0] = context.DisplayName;
            if (arguments != null && arguments.Length > 0)
            {
                Array.Copy(arguments, 0, args, 1, arguments.Length);
            }

            return new GridFieldValidationFailure(
                context.FieldId,
                errorCode,
                string.Format(CultureInfo.CurrentCulture, messageFormat, args),
                "grid.validation." + errorCode);
        }

        private static class GridFieldValidationErrorCodes
        {
            public const string Required = "required";
            public const string MinLength = "min-length";
            public const string MaxLength = "max-length";
            public const string Pattern = "pattern";
            public const string AllowedValues = "allowed-values";
            public const string MultilineNotAllowed = "multiline-not-allowed";
            public const string ParseInteger = "parse-integer";
            public const string ParseDecimal = "parse-decimal";
            public const string ParseBoolean = "parse-boolean";
            public const string ParseDate = "parse-date";
            public const string ParseDateTime = "parse-datetime";
            public const string ParseTime = "parse-time";
            public const string MinValue = "min-value";
            public const string MaxValue = "max-value";
            public const string MinDate = "min-date";
            public const string MaxDate = "max-date";
            public const string MinDateTime = "min-datetime";
            public const string MaxDateTime = "max-datetime";
            public const string MinTime = "min-time";
            public const string MaxTime = "max-time";
            public const string Precision = "precision";
            public const string Scale = "scale";
            public const string Step = "step";
            public const string NegativeNotAllowed = "negative-not-allowed";
            public const string ZeroNotAllowed = "zero-not-allowed";
            public const string PastNotAllowed = "past-not-allowed";
            public const string FutureNotAllowed = "future-not-allowed";
            public const string AllowedDaysOfWeek = "allowed-days-of-week";
            public const string MinuteStep = "minute-step";
        }
    }
}
