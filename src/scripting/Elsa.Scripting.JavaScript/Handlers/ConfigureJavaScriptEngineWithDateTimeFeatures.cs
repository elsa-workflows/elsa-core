using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Scripting.JavaScript.Messages;
using MediatR;
using NodaTime;

namespace Elsa.Scripting.JavaScript.Handlers
{
    public class ConfigureJavaScriptEngineWithDateTimeFeatures : INotificationHandler<EvaluatingJavaScriptExpression>
    {
        private readonly IClock _clock;

        public ConfigureJavaScriptEngineWithDateTimeFeatures(IClock clock)
        {
            _clock = clock;
        }

        public Task Handle(EvaluatingJavaScriptExpression notification, CancellationToken cancellationToken)
        {
            var engine = notification.Engine;

            engine.SetValue(
                "instantFromDateTimeUtc",
                (Func<DateTime, Instant>) Instant.FromDateTimeUtc);
            
            engine.SetValue(
                "instantFromUtc",
                (Func<int, int, int, int, int, int, Instant>) Instant.FromUtc);

            engine.SetValue(
                "currentInstant",
                (Func<Instant>) (() => _clock.GetCurrentInstant())
            );

            engine.SetValue(
                "currentYear",
                (Func<int>) (() => _clock.GetCurrentInstant().InUtc().Year)
            );

            engine.SetValue(
                "startOfMonth",
                (Func<Instant?, LocalDate>) (instant =>
                    (instant ?? _clock.GetCurrentInstant()).InUtc().LocalDateTime.With(DateAdjusters.StartOfMonth).Date)
            );

            engine.SetValue(
                "endOfMonth",
                (Func<Instant?, LocalDate>) (instant =>
                    (instant ?? _clock.GetCurrentInstant()).InUtc().LocalDateTime.With(DateAdjusters.EndOfMonth).Date)
            );

            engine.SetValue(
                "startOfPreviousMonth",
                (Func<Instant?, LocalDate>) (instant =>
                    GetStartOfMonth(GetStartOfMonth(instant ?? _clock.GetCurrentInstant()).Minus(Period.FromDays(14)))
                        .Date)
            );

            engine.SetValue(
                "addDurationTo",
                (Func<Instant, Duration, Instant>) ((instant, duration) => instant.Plus(duration))
            );

            engine.SetValue(
                "subtractDurationFrom",
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
                (Func<Instant, string, CultureInfo?, string>) ((instant, format, cultureInfo) =>
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
            
            engine.SetValue(
                "durationBetween",
                (Func<Instant, Instant, Duration>) ((a, b) => a - b)
            );
            
            engine.SetValue(
                "periodFromNow",
                (Func<Instant, Period>) GetPeriodFromNow
            );

            return Task.CompletedTask;
        }

        private LocalDateTime GetStartOfMonth(Instant instant) => GetStartOfMonth(instant.InUtc().LocalDateTime);
        private LocalDateTime GetStartOfMonth(LocalDateTime localDateTime) => localDateTime.With(DateAdjusters.StartOfMonth);
        
        private Period GetPeriodFromNow(Instant pastInstant)
        {
            var now = _clock.GetCurrentInstant();
            var today = now.InUtc().Date;
            var pastDate = pastInstant.InUtc().Date;
            var period = Period.Between(pastDate, today);
            return period;
        }
    }
}