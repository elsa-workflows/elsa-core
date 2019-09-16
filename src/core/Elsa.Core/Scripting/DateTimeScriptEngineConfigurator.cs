using System;
using Elsa.Services.Models;
using Jint;
using NodaTime;

namespace Elsa.Scripting
{
    public class DateTimeScriptEngineConfigurator : IScriptEngineConfigurator
    {
        public void Configure(Engine engine, WorkflowExecutionContext workflowExecutionContext)
        {
            engine.SetValue(
                "localDateToInstant",
                (Func<LocalDate, Instant>) (value => value.AtStartOfDayInZone(DateTimeZone.Utc).ToInstant())
            );
        }

        private Instant GetInstant(LocalDate localDate)
        {
            var timezone = DateTimeZoneProviders.Tzdb.GetSystemDefault();
            return localDate.AtStartOfDayInZone(timezone).ToInstant();
        }
    }
}