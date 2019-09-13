using System;
using Elsa.Services.Models;
using Jint;
using Newtonsoft.Json.Linq;
using NodaTime;

namespace Elsa.Scripting
{
    public class DateTimeScriptEngineConfigurator : IScriptEngineConfigurator
    {
        public void Configure(Engine engine, WorkflowExecutionContext workflowExecutionContext)
        {
            engine.SetValue("localDateToInstant", (Func<JToken, Instant>) (value => GetInstant(value.ToObject<LocalDate>())));
        }

        private Instant GetInstant(LocalDate localDate)
        {
            var timezone = DateTimeZoneProviders.Tzdb.GetSystemDefault();
            return localDate.AtStartOfDayInZone(timezone).ToInstant();
        }
    }
}