using Newtonsoft.Json.Linq;
using NodaTime;
using NodaTime.Text;

namespace Elsa.Serialization.Handlers
{
    public sealed class ZonedDateTimeHandler : PrimitiveValueHandler<ZonedDateTime>
    {
        protected override object ParseValue(JToken value) => ZonedDateTimePattern.ExtendedFormatOnlyIso.Parse(value.ToString()).Value;
    }
}