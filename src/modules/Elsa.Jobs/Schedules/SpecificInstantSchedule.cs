using Elsa.Jobs.Contracts;

namespace Elsa.Jobs.Schedules;

public record SpecificInstantSchedule(DateTimeOffset DateTime) : ISchedule;