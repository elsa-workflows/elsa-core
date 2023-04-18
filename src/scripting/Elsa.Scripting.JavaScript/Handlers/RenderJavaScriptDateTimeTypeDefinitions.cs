using System.Threading;
using System.Threading.Tasks;
using Elsa.Scripting.JavaScript.Events;
using Elsa.Services;
using MediatR;

namespace Elsa.Scripting.JavaScript.Handlers
{
    public class RenderJavaScriptDateTimeTypeDefinitions : INotificationHandler<RenderingTypeScriptDefinitions>
    {
        private readonly IActivityTypeService _activityTypeService;

        public RenderJavaScriptDateTimeTypeDefinitions(IActivityTypeService activityTypeService)
        {
            _activityTypeService = activityTypeService;
        }

        public Task Handle(RenderingTypeScriptDefinitions notification, CancellationToken cancellationToken)
        {
            var output = notification.Output;

            output.AppendLine("declare function instantFromDateTimeUtc(dateTime: DateTime): Instant");
            output.AppendLine("declare function instantFromUtc(year: number, month: number, day: number, hour?: number, minute?: number, second?: number): Instant");
            output.AppendLine("declare function currentInstant(): Instant;");
            output.AppendLine("declare function currentYear(): number;");
            output.AppendLine("declare function startOfMonth(instant?: Instant): LocalDate;");
            output.AppendLine("declare function endOfMonth(instant?: Instant): LocalDate;");
            output.AppendLine("declare function startOfPreviousMonth(instant?: Instant): LocalDate;");
            output.AppendLine("declare function addDurationTo(instant: Instant, duration: Duration): Instant;");
            output.AppendLine("declare function subtractDurationFrom(instant: Instant, duration: Duration): Instant;");
            output.AppendLine("declare function durationFromSeconds(seconds: number): Duration;");
            output.AppendLine("declare function durationFromMinutes(minutes: number): Duration;");
            output.AppendLine("declare function durationFromHours(hours: number): Duration;");
            output.AppendLine("declare function durationFromDays(days: number): Duration;");
            output.AppendLine("declare function formatInstant(instant: Instant, format: string, culture?: CultureInfo): string;");
            output.AppendLine("declare function localDateFromInstant(instant: Instant): LocalDate;");
            output.AppendLine("declare function instantFromLocalDate(localDate: LocalDate): Instant;");
            output.AppendLine("declare function durationBetween(a: Instant, b: Instant): Duration;");
            output.AppendLine("declare function periodFromNow(pastInstant: Instant): Period;");
            output.AppendLine("declare function jsonEncode(value: any): string;");
            output.AppendLine("declare function jsonDecode(value: string): any;");
            output.AppendLine("declare function base64Encode(value: string): string;");
            output.AppendLine("declare function base64Decode(value: string): string;");
            output.AppendLine("declare function addJournal(name: string, value?: any): void;");
            return Task.CompletedTask;
        }
    }
}
