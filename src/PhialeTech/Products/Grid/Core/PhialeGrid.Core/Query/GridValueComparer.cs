using System;
using System.Collections.Generic;
using System.Globalization;

namespace PhialeGrid.Core.Query
{
    public sealed class GridValueComparer : IComparer<object>, IEqualityComparer<object>
    {
        public static readonly GridValueComparer Instance = new GridValueComparer();

        public int Compare(object x, object y)
        {
            if (ReferenceEquals(x, y))
            {
                return 0;
            }

            if (x == null)
            {
                return -1;
            }

            if (y == null)
            {
                return 1;
            }

            if (TryConvertToDecimal(x, out var dx) && TryConvertToDecimal(y, out var dy))
            {
                return dx.CompareTo(dy);
            }

            if (x.GetType() == y.GetType() && x is IComparable sameTypeComparable)
            {
                return sameTypeComparable.CompareTo(y);
            }

            if (x is IComparable comparable)
            {
                try
                {
                    return comparable.CompareTo(y);
                }
                catch
                {
                    // Fall back to string comparison for mixed runtime types.
                }
            }

            if (y is IComparable reverseComparable)
            {
                try
                {
                    return -reverseComparable.CompareTo(x);
                }
                catch
                {
                    // Fall back to string comparison for mixed runtime types.
                }
            }

            return string.CompareOrdinal(
                Convert.ToString(x, CultureInfo.InvariantCulture),
                Convert.ToString(y, CultureInfo.InvariantCulture));
        }

        public new bool Equals(object x, object y)
        {
            if (ReferenceEquals(x, y))
            {
                return true;
            }

            if (x == null || y == null)
            {
                return false;
            }

            if (TryConvertToDecimal(x, out var dx) && TryConvertToDecimal(y, out var dy))
            {
                return dx == dy;
            }

            return object.Equals(x, y);
        }

        public int GetHashCode(object obj)
        {
            if (obj == null)
            {
                return 0;
            }

            if (TryConvertToDecimal(obj, out var value))
            {
                return value.GetHashCode();
            }

            return obj.GetHashCode();
        }

        public static bool TryConvertToDecimal(object value, out decimal result)
        {
            result = 0m;
            if (value == null)
            {
                return false;
            }

            switch (Type.GetTypeCode(value.GetType()))
            {
                case TypeCode.Byte:
                case TypeCode.SByte:
                case TypeCode.Int16:
                case TypeCode.UInt16:
                case TypeCode.Int32:
                case TypeCode.UInt32:
                case TypeCode.Int64:
                case TypeCode.UInt64:
                case TypeCode.Single:
                case TypeCode.Double:
                case TypeCode.Decimal:
                    result = Convert.ToDecimal(value, CultureInfo.InvariantCulture);
                    return true;
                case TypeCode.String:
                    return decimal.TryParse(
                        (string)value,
                        NumberStyles.Number,
                        CultureInfo.InvariantCulture,
                        out result);
                default:
                    return false;
            }
        }

        public static bool TryConvertToBoolean(object value, out bool result)
        {
            result = false;
            if (value == null)
            {
                return false;
            }

            if (value is bool direct)
            {
                result = direct;
                return true;
            }

            if (TryConvertToDecimal(value, out var numeric))
            {
                if (numeric == 0m)
                {
                    result = false;
                    return true;
                }

                if (numeric == 1m)
                {
                    result = true;
                    return true;
                }
            }

            if (value is string text)
            {
                return bool.TryParse(text, out result);
            }

            return false;
        }
    }
}
