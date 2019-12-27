using Newtonsoft.Json.Linq;
using NodaTime;
using NodaTime.Text;

namespace Elsa.Serialization.Handlers
{
    public sealed class LocalTimeHandler : PrimitiveValueHandler<LocalTime>
    {
        protected override object ParseValue(JToken value) => LocalTimePattern.LongExtendedIso.Parse(value.ToString()).Value;
    }
}