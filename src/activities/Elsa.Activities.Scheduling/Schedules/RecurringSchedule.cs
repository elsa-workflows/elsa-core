using System;
using Elsa.Activities.Scheduling.Contracts;

namespace Elsa.Activities.Scheduling.Schedules;

public record RecurringSchedule(DateTime StartAt, TimeSpan Interval) : ISchedule;