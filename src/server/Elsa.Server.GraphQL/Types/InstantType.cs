using System;
using System.Globalization;
using Elsa.Server.GraphQL.Extensions;
using HotChocolate.Language;
using HotChocolate.Types;
using NodaTime;
using NodaTime.Text;

namespace Elsa.Server.GraphQL.Types
{
    public class InstantType : ScalarType
    {
        public InstantType() : base("Instant")
        {
            Description = "Represents an instant on the global timeline.";
        }

        public override Type ClrType => typeof(Instant);

        public override bool IsInstanceOfType(IValueNode literal)
        {
            if (literal == null)
            {
                throw new ArgumentNullException(nameof(literal));
            }

            if (literal is NullValueNode)
            {
                return true;
            }

            return literal is StringValueNode stringLiteral && TryParseLiteral(stringLiteral, out _);
        }

        private bool TryParseLiteral(StringValueNode literal, out object obj) =>
            TryDeserializeFromString(literal.Value, out obj);

        protected bool TryDeserializeFromString(
            string serialized,
            out object obj)
        {
            obj = null;
            return false;
        }

        public override object ParseLiteral(IValueNode literal)
        {
            if (literal is StringValueNode stringValue)
            {
                return FromString(stringValue.Value);
            }

            return null;
        }

        public override IValueNode ParseValue(object value)
        {
            return null;
        }

        public override object Serialize(object value)
        {
            if (value is string)
                return value;
            if (value is DateTime dateTime)
                return dateTime.ToString("o", CultureInfo.InvariantCulture);
            if (value is Instant instant)
                return InstantPattern.ExtendedIso
                    .WithCulture(CultureInfo.InvariantCulture)
                    .Format(instant);
            return null;
        }

        public override bool TryDeserialize(object serialized, out object value)
        {
            throw new NotImplementedException();
        }

        private static object FromString(string stringValue)
        {
            return ParserComposer.FirstNonThrowing(new Func<string, Instant>[]
            {
                str => OffsetDateTimePattern.ExtendedIso
                    .WithCulture(CultureInfo.InvariantCulture)
                    .Parse(str).GetValueOrThrow().ToInstant(),
                str => OffsetDateTimePattern
                    .CreateWithInvariantCulture("yyyy'-'MM'-'dd'T'HH':'mm':'sso<+HHmm>")
                    .Parse(str).GetValueOrThrow().ToInstant(),
                str => InstantPattern.ExtendedIso
                    .WithCulture(CultureInfo.InvariantCulture)
                    .Parse(str).GetValueOrThrow(),
            }, stringValue);
        }

        private static object FromDateTimeUtc(DateTime dateTime)
        {
            try
            {
                return Instant.FromDateTimeUtc(dateTime);
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}