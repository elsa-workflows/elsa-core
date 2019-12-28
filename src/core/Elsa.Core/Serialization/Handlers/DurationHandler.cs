using Newtonsoft.Json.Linq;
using NodaTime;
using NodaTime.Text;

namespace Elsa.Serialization.Handlers
{
    public sealed class DurationHandler : PrimitiveValueHandler<Duration>
    {
        protected override object ParseValue(JToken value) => DurationPattern.Roundtrip.Parse(value.ToString()).Value;
    }
}