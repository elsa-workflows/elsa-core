using Newtonsoft.Json.Linq;
using NodaTime;
using NodaTime.Text;

namespace Elsa.Serialization.Handlers
{
    public sealed class OffsetDateHandler : PrimitiveValueHandler<OffsetDate>
    {
        protected override object ParseValue(JToken value) => OffsetDatePattern.GeneralIso.Parse(value.ToString()).Value;
    }
}