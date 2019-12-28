using System;
using Newtonsoft.Json.Linq;

namespace Elsa.Serialization.Handlers
{
    public sealed class DateTimeHandler : PrimitiveValueHandler<DateTime>
    {
        protected override object ParseValue(JToken value) => value.Value<DateTime>();
    }
}