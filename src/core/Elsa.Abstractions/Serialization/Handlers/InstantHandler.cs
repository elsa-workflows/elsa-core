using Newtonsoft.Json.Linq;
using NodaTime;
using NodaTime.Text;

namespace Elsa.Serialization.Handlers
{
    public sealed class InstantHandler : PrimitiveValueHandler<Instant>
    {
        protected override object ParseValue(JToken value) => InstantPattern.ExtendedIso.Parse(value.ToString()).Value;
    }
}