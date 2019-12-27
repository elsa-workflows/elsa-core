using Newtonsoft.Json.Linq;
using NodaTime;
using NodaTime.Text;

namespace Elsa.Serialization.Handlers
{
    public sealed class LocalDateHandler : PrimitiveValueHandler<LocalDate>
    {
        protected override object ParseValue(JToken value) => LocalDatePattern.Iso.Parse(value.ToString()).Value;
    }
}