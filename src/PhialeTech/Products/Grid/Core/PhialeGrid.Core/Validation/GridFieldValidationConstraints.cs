using System;
using System.Collections.Generic;
using System.Linq;
using PhialeGrid.Core.Columns;

namespace PhialeGrid.Core.Validation
{
    public abstract class GridFieldValidationConstraints
    {
        protected GridFieldValidationConstraints(bool required = false)
        {
            Required = required;
        }

        public bool Required { get; }
    }

    public sealed class TextValidationConstraints : GridFieldValidationConstraints
    {
        public TextValidationConstraints(
            bool required = false,
            int? minLength = null,
            int? maxLength = null,
            string pattern = null,
            bool trimBeforeValidation = false,
            IReadOnlyList<string> allowedValues = null,
            bool allowMultiline = true)
            : base(required)
        {
            if (minLength.HasValue && minLength.Value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(minLength));
            }

            if (maxLength.HasValue && maxLength.Value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxLength));
            }

            if (minLength.HasValue && maxLength.HasValue && minLength.Value > maxLength.Value)
            {
                throw new ArgumentException("MinLength cannot be greater than MaxLength.");
            }

            MinLength = minLength;
            MaxLength = maxLength;
            Pattern = pattern ?? string.Empty;
            TrimBeforeValidation = trimBeforeValidation;
            AllowedValues = (allowedValues ?? Array.Empty<string>())
                .Where(value => value != null)
                .ToArray();
            AllowMultiline = allowMultiline;
        }

        public int? MinLength { get; }

        public int? MaxLength { get; }

        public string Pattern { get; }

        public bool TrimBeforeValidation { get; }

        public IReadOnlyList<string> AllowedValues { get; }

        public bool AllowMultiline { get; }
    }

    public sealed class IntegerValidationConstraints : GridFieldValidationConstraints
    {
        public IntegerValidationConstraints(
            bool required = false,
            long? minValue = null,
            long? maxValue = null,
            bool? allowNegative = null,
            bool? allowZero = null,
            long? step = null)
            : base(required)
        {
            if (minValue.HasValue && maxValue.HasValue && minValue.Value > maxValue.Value)
            {
                throw new ArgumentException("MinValue cannot be greater than MaxValue.");
            }

            if (step.HasValue && step.Value <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(step));
            }

            MinValue = minValue;
            MaxValue = maxValue;
            AllowNegative = allowNegative;
            AllowZero = allowZero;
            Step = step;
        }

        public long? MinValue { get; }

        public long? MaxValue { get; }

        public bool? AllowNegative { get; }

        public bool? AllowZero { get; }

        public long? Step { get; }
    }

    public sealed class DecimalValidationConstraints : GridFieldValidationConstraints
    {
        public DecimalValidationConstraints(
            bool required = false,
            decimal? minValue = null,
            decimal? maxValue = null,
            int? scale = null,
            int? precision = null,
            bool? allowNegative = null,
            bool? allowZero = null,
            decimal? step = null)
            : base(required)
        {
            if (minValue.HasValue && maxValue.HasValue && minValue.Value > maxValue.Value)
            {
                throw new ArgumentException("MinValue cannot be greater than MaxValue.");
            }

            if (scale.HasValue && scale.Value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(scale));
            }

            if (precision.HasValue && precision.Value <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(precision));
            }

            if (precision.HasValue && scale.HasValue && scale.Value > precision.Value)
            {
                throw new ArgumentException("Scale cannot be greater than Precision.");
            }

            if (step.HasValue && step.Value <= 0m)
            {
                throw new ArgumentOutOfRangeException(nameof(step));
            }

            MinValue = minValue;
            MaxValue = maxValue;
            Scale = scale;
            Precision = precision;
            AllowNegative = allowNegative;
            AllowZero = allowZero;
            Step = step;
        }

        public decimal? MinValue { get; }

        public decimal? MaxValue { get; }

        public int? Scale { get; }

        public int? Precision { get; }

        public bool? AllowNegative { get; }

        public bool? AllowZero { get; }

        public decimal? Step { get; }
    }

    public sealed class BooleanValidationConstraints : GridFieldValidationConstraints
    {
        public BooleanValidationConstraints(bool required = false, bool? defaultValue = null)
            : base(required)
        {
            DefaultValue = defaultValue;
        }

        public bool? DefaultValue { get; }
    }

    public sealed class DateValidationConstraints : GridFieldValidationConstraints
    {
        public DateValidationConstraints(
            bool required = false,
            DateTime? minDate = null,
            DateTime? maxDate = null,
            bool? allowPast = null,
            bool? allowFuture = null,
            IReadOnlyList<DayOfWeek> allowedDaysOfWeek = null)
            : base(required)
        {
            if (minDate.HasValue && maxDate.HasValue && minDate.Value.Date > maxDate.Value.Date)
            {
                throw new ArgumentException("MinDate cannot be greater than MaxDate.");
            }

            MinDate = minDate?.Date;
            MaxDate = maxDate?.Date;
            AllowPast = allowPast;
            AllowFuture = allowFuture;
            AllowedDaysOfWeek = (allowedDaysOfWeek ?? Array.Empty<DayOfWeek>()).ToArray();
        }

        public DateTime? MinDate { get; }

        public DateTime? MaxDate { get; }

        public bool? AllowPast { get; }

        public bool? AllowFuture { get; }

        public IReadOnlyList<DayOfWeek> AllowedDaysOfWeek { get; }
    }

    public sealed class DateTimeValidationConstraints : GridFieldValidationConstraints
    {
        public DateTimeValidationConstraints(
            bool required = false,
            DateTime? minDateTime = null,
            DateTime? maxDateTime = null)
            : base(required)
        {
            if (minDateTime.HasValue && maxDateTime.HasValue && minDateTime.Value > maxDateTime.Value)
            {
                throw new ArgumentException("MinDateTime cannot be greater than MaxDateTime.");
            }

            MinDateTime = minDateTime;
            MaxDateTime = maxDateTime;
        }

        public DateTime? MinDateTime { get; }

        public DateTime? MaxDateTime { get; }
    }

    public sealed class TimeValidationConstraints : GridFieldValidationConstraints
    {
        public TimeValidationConstraints(
            bool required = false,
            TimeSpan? minTime = null,
            TimeSpan? maxTime = null,
            int? minuteStep = null)
            : base(required)
        {
            if (minTime.HasValue && maxTime.HasValue && minTime.Value > maxTime.Value)
            {
                throw new ArgumentException("MinTime cannot be greater than MaxTime.");
            }

            if (minuteStep.HasValue && minuteStep.Value <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(minuteStep));
            }

            MinTime = minTime;
            MaxTime = maxTime;
            MinuteStep = minuteStep;
        }

        public TimeSpan? MinTime { get; }

        public TimeSpan? MaxTime { get; }

        public int? MinuteStep { get; }
    }

    public sealed class LookupValidationConstraints : GridFieldValidationConstraints
    {
        public LookupValidationConstraints(
            IReadOnlyList<object> allowedValues,
            bool required = false,
            bool allowEmptySelection = true,
            bool strictMatch = true)
            : base(required)
        {
            AllowedValues = (allowedValues ?? Array.Empty<object>())
                .Where(value => value != null)
                .ToArray();
            if (AllowedValues.Count == 0)
            {
                throw new ArgumentException("AllowedValues must contain at least one value.", nameof(allowedValues));
            }

            AllowEmptySelection = allowEmptySelection;
            StrictMatch = strictMatch;
        }

        public IReadOnlyList<object> AllowedValues { get; }

        public bool AllowEmptySelection { get; }

        public bool StrictMatch { get; }
    }

    internal static class GridFieldValidationConfigurationValidator
    {
        public static void EnsureCompatible(
            string fieldId,
            Type valueType,
            GridColumnEditorKind editorKind,
            GridFieldValidationConstraints constraints)
        {
            if (constraints == null)
            {
                return;
            }

            var normalizedType = Nullable.GetUnderlyingType(valueType ?? typeof(object)) ?? valueType ?? typeof(object);
            if (constraints is TextValidationConstraints)
            {
                if (normalizedType != typeof(string))
                {
                    ThrowIncompatible(fieldId, normalizedType, typeof(TextValidationConstraints));
                }

                return;
            }

            if (constraints is IntegerValidationConstraints)
            {
                if (!IsIntegerType(normalizedType))
                {
                    ThrowIncompatible(fieldId, normalizedType, typeof(IntegerValidationConstraints));
                }

                return;
            }

            if (constraints is DecimalValidationConstraints)
            {
                if (!IsDecimalType(normalizedType))
                {
                    ThrowIncompatible(fieldId, normalizedType, typeof(DecimalValidationConstraints));
                }

                return;
            }

            if (constraints is BooleanValidationConstraints)
            {
                if (normalizedType != typeof(bool))
                {
                    ThrowIncompatible(fieldId, normalizedType, typeof(BooleanValidationConstraints));
                }

                return;
            }

            if (constraints is DateValidationConstraints)
            {
                if (normalizedType != typeof(DateTime) && normalizedType != typeof(DateTimeOffset))
                {
                    ThrowIncompatible(fieldId, normalizedType, typeof(DateValidationConstraints));
                }

                return;
            }

            if (constraints is DateTimeValidationConstraints)
            {
                if (normalizedType != typeof(DateTime) && normalizedType != typeof(DateTimeOffset))
                {
                    ThrowIncompatible(fieldId, normalizedType, typeof(DateTimeValidationConstraints));
                }

                return;
            }

            if (constraints is TimeValidationConstraints)
            {
                if (normalizedType != typeof(TimeSpan))
                {
                    ThrowIncompatible(fieldId, normalizedType, typeof(TimeValidationConstraints));
                }

                return;
            }

            if (constraints is LookupValidationConstraints)
            {
                if (normalizedType != typeof(string) &&
                    !normalizedType.IsEnum &&
                    editorKind != GridColumnEditorKind.Combo &&
                    editorKind != GridColumnEditorKind.Autocomplete)
                {
                    ThrowIncompatible(fieldId, normalizedType, typeof(LookupValidationConstraints));
                }

                return;
            }

            throw new InvalidOperationException(
                string.Format(
                    System.Globalization.CultureInfo.InvariantCulture,
                    "Unsupported validation constraint type '{0}' for field '{1}'.",
                    constraints.GetType().FullName,
                    fieldId ?? string.Empty));
        }

        private static void ThrowIncompatible(string fieldId, Type valueType, Type constraintsType)
        {
            throw new InvalidOperationException(
                string.Format(
                    System.Globalization.CultureInfo.InvariantCulture,
                    "Validation constraints '{0}' are not compatible with field '{1}' of type '{2}'.",
                    constraintsType.Name,
                    fieldId ?? string.Empty,
                    valueType == null ? typeof(object).FullName : valueType.FullName));
        }

        private static bool IsIntegerType(Type type)
        {
            return type == typeof(byte) ||
                type == typeof(sbyte) ||
                type == typeof(short) ||
                type == typeof(ushort) ||
                type == typeof(int) ||
                type == typeof(uint) ||
                type == typeof(long) ||
                type == typeof(ulong);
        }

        private static bool IsDecimalType(Type type)
        {
            return type == typeof(decimal) ||
                type == typeof(double) ||
                type == typeof(float);
        }
    }
}
