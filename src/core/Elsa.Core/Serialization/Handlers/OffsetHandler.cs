using Newtonsoft.Json.Linq;
using NodaTime;
using NodaTime.Text;

namespace Elsa.Serialization.Handlers
{
    public sealed class OffsetHandler : PrimitiveValueHandler<Offset>
    {
        protected override object ParseValue(JToken value) => OffsetPattern.GeneralInvariant.Parse(value.ToString()).Value;
    }
}