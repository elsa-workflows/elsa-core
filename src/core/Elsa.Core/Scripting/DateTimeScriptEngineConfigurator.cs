using System;
using System.Globalization;
using Elsa.Services.Models;
using Jint;
using NodaTime;

namespace Elsa.Scripting
{
    public class DateTimeScriptEngineConfigurator : IScriptEngineConfigurator
    {
        private readonly IClock clock;

        public DateTimeScriptEngineConfigurator(IClock clock)
        {
            this.clock = clock;
        }

        public void Configure(Engine engine, WorkflowExecutionContext workflowExecutionContext)
        {
            engine.SetValue(
                "currentInstant",
                (Func<Instant>) (() => clock.GetCurrentInstant())
            );

            engine.SetValue(
                "currentYear",
                (Func<int>) (() => clock.GetCurrentInstant().InUtc().Year)
            );

            engine.SetValue(
                "startOfMonth",
                (Func<Instant?, LocalDate>) (instant =>
                    (instant ?? clock.GetCurrentInstant()).InUtc().LocalDateTime.With(DateAdjusters.StartOfMonth).Date)
            );

            engine.SetValue(
                "endOfMonth",
                (Func<Instant?, LocalDate>) (instant =>
                    (instant ?? clock.GetCurrentInstant()).InUtc().LocalDateTime.With(DateAdjusters.EndOfMonth).Date)
            );

            engine.SetValue(
                "startOfPreviousMonth",
                (Func<Instant?, LocalDate>) (instant =>
                    GetStartOfMonth(GetStartOfMonth(instant ?? clock.GetCurrentInstant()).Minus(Period.FromDays(14)))
                        .Date)
            );

            engine.SetValue(
                "plus",
                (Func<Instant, Duration, Instant>) ((instant, duration) => instant.Plus(duration))
            );

            engine.SetValue(
                "minus",
                (Func<Instant, Duration, Instant>) ((instant, duration) => instant.Minus(duration))
            );

            engine.SetValue(
                "durationFromDays",
                (Func<int, Duration>) (Duration.FromDays)
            );

            engine.SetValue(
                "formatInstant",
                (Func<Instant, string, string>) ((instant, format) =>
                    instant.ToString(format, CultureInfo.InvariantCulture))
            );

            engine.SetValue(
                "localDateFromInstant",
                (Func<Instant, LocalDate>) (instant => instant.InUtc().Date)
            );

            engine.SetValue(
                "instantFromLocalDate",
                (Func<LocalDate, Instant>) (value => value.AtStartOfDayInZone(DateTimeZone.Utc).ToInstant())
            );
        }

        private LocalDateTime GetStartOfMonth(Instant instant)
        {
            return GetStartOfMonth(instant.InUtc().LocalDateTime);
        }

        private LocalDateTime GetStartOfMonth(LocalDateTime localDateTime)
        {
            return localDateTime.With(DateAdjusters.StartOfMonth);
        }
    }
}