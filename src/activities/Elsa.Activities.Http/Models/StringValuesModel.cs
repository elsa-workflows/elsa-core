using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Primitives;

namespace Elsa.Activities.Http.Models
{
    /// <summary>
    /// Represents a collection of zero or more string values.  It is used for values such as
    /// HTTP header/query string values.  These are often thought-of as key/value pairs but might
    /// have more than one value.
    /// </summary>
    public class StringValuesModel : IConvertible
    {
        /// <summary>
        /// Gets either the first value of <see cref="Values"/> or a <see langword="null"/> reference
        /// if the values are null or empty.
        /// </summary>
        /// <returns>The first value</returns>
        public string? Value => Values?.FirstOrDefault();

        /// <summary>
        /// Gets a collection of the string values for the current instance.
        /// </summary>
        /// <value>The values</value>
        public string[] Values { get; set; }

        /// <summary>
        /// Initializes an instance of <see cref="StringValuesModel"/> with an empty collection of values.
        /// </summary>
        public StringValuesModel() => Values = new string[0];

        /// <summary>
        /// Initializes an instance of <see cref="StringValuesModel"/> from a <see cref="StringValues"/>.
        /// </summary>
        /// <param name="value">A model of zero or more string values.</param>
        public StringValuesModel(StringValues value) => Values = value.ToArray();

        /// <summary>
        /// Gets a string representation of the current instance.  Either null (for no, or an empty-collection of <see cref="Values"/>),
        /// a single string (for a collection of one <see cref="Values"/>) or a comma-separated string of many values.
        /// </summary>
        /// <returns>The string representation of the <see cref="Values"/>.</returns>
        public override string? ToString() => (Values?.Any() == true) ? string.Join(",", Values) : null;

        #region IConvertible implementation

        TypeCode IConvertible.GetTypeCode() => TypeCode.Object;
        bool IConvertible.ToBoolean(IFormatProvider? provider) => Convert.ToBoolean(ToString(), provider);
        byte IConvertible.ToByte(IFormatProvider? provider) => Convert.ToByte(ToString(), provider);
        char IConvertible.ToChar(IFormatProvider? provider) => Convert.ToChar(ToString()!, provider);
        DateTime IConvertible.ToDateTime(IFormatProvider? provider) => Convert.ToDateTime(ToString(), provider);
        decimal IConvertible.ToDecimal(IFormatProvider? provider) => Convert.ToDecimal(ToString(), provider);
        double IConvertible.ToDouble(IFormatProvider? provider) => Convert.ToDouble(ToString(), provider);
        short IConvertible.ToInt16(IFormatProvider? provider) => Convert.ToInt16(ToString(), provider);
        int IConvertible.ToInt32(IFormatProvider? provider) => Convert.ToInt32(ToString(), provider);
        long IConvertible.ToInt64(IFormatProvider? provider) => Convert.ToInt64(ToString(), provider);
        sbyte IConvertible.ToSByte(IFormatProvider? provider) => Convert.ToSByte(ToString()!, provider);
        float IConvertible.ToSingle(IFormatProvider? provider) => Convert.ToSingle(ToString(), provider);
        string IConvertible.ToString(IFormatProvider? provider) => ToString()!;
        object IConvertible.ToType(Type conversionType, IFormatProvider? provider) => Convert.ChangeType(ToString()!, conversionType, provider);
        ushort IConvertible.ToUInt16(IFormatProvider? provider) => Convert.ToUInt16(ToString(), provider);
        uint IConvertible.ToUInt32(IFormatProvider? provider) => Convert.ToUInt32(ToString(), provider);
        ulong IConvertible.ToUInt64(IFormatProvider? provider) => Convert.ToUInt64(ToString(), provider);

        #endregion
    }
}