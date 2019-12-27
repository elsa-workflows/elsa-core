using Newtonsoft.Json.Linq;
using NodaTime;
using NodaTime.Text;

namespace Elsa.Serialization.Handlers
{
    public sealed class LocalDateTimeHandler : PrimitiveValueHandler<LocalDateTime>
    {
        protected override object ParseValue(JToken value) => LocalDateTimePattern.ExtendedIso.Parse(value.ToString()).Value;
    }
}