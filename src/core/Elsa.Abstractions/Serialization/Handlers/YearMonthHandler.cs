using Newtonsoft.Json.Linq;
using NodaTime;
using NodaTime.Text;

namespace Elsa.Serialization.Handlers
{
    public sealed class YearMonthHandler : PrimitiveValueHandler<YearMonth>
    {
        protected override object ParseValue(JToken value) => YearMonthPattern.Iso.Parse(value.ToString()).Value;
    }
}