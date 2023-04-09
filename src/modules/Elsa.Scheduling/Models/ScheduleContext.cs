using Elsa.Scheduling.Contracts;

namespace Elsa.Scheduling.Models;

public record ScheduleContext(IServiceProvider ServiceProvider, ITask Task);