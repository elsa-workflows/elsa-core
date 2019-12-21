using Newtonsoft.Json.Linq;
using NodaTime;
using NodaTime.Text;

namespace Elsa.Serialization.Handlers
{
    public sealed class OffsetTimeHandler : PrimitiveValueHandler<OffsetTime>
    {
        protected override object ParseValue(JToken value) => OffsetTimePattern.GeneralIso.Parse(value.ToString()).Value;
    }
}