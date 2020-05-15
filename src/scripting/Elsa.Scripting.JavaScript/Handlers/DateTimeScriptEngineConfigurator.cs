using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Scripting.JavaScript.Messages;
using MediatR;
using NodaTime;

namespace Elsa.Scripting.JavaScript.Handlers
{
    public class DateTimeScriptEngineConfigurator : INotificationHandler<EvaluatingJavaScriptExpression>
    {
        private readonly IClock clock;

        public DateTimeScriptEngineConfigurator(IClock clock)
        {
            this.clock = clock;
        }

        public Task Handle(EvaluatingJavaScriptExpression notification, CancellationToken cancellationToken)
        {
            var engine = notification.Engine;

            engine.SetValue(
                "instantFromDateTimeUtc",
                (Func<DateTime, Instant>) Instant.FromDateTimeUtc);

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
                "durationFromSeconds",
                (Func<long, Duration>) (Duration.FromSeconds)
            );

            engine.SetValue(
                "durationFromMinutes",
                (Func<long, Duration>) (Duration.FromMinutes)
            );

            engine.SetValue(
                "durationFromHours",
                (Func<int, Duration>) (Duration.FromHours)
            );

            engine.SetValue(
                "durationFromDays",
                (Func<int, Duration>) (Duration.FromDays)
            );

            engine.SetValue(
                "formatInstant",
                (Func<Instant, string, CultureInfo, string>) ((instant, format, cultureInfo) =>
                    instant.ToString(format, cultureInfo ?? CultureInfo.InvariantCulture))
            );

            engine.SetValue(
                "localDateFromInstant",
                (Func<Instant, LocalDate>) (instant => instant.InUtc().Date)
            );

            engine.SetValue(
                "instantFromLocalDate",
                (Func<LocalDate, Instant>) (value => value.AtStartOfDayInZone(DateTimeZone.Utc).ToInstant())
            );

            return Task.CompletedTask;
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