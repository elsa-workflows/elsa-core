using Newtonsoft.Json.Linq;
using NodaTime;
using NodaTime.Text;

namespace Elsa.Serialization.Handlers
{
    public sealed class AnnualDateHandler : PrimitiveValueHandler<AnnualDate>
    {
        protected override object ParseValue(JToken value) => AnnualDatePattern.Iso.Parse(value.ToString()).Value;
    }
}