using Elsa.Jobs.Services;

namespace Elsa.Jobs.Schedules;

public record SpecificInstantSchedule(DateTimeOffset DateTime) : ISchedule;