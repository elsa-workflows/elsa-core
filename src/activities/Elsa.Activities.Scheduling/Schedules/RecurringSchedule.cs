using System;
using Elsa.Activities.Scheduling.Contracts;

namespace Elsa.Activities.Scheduling.Schedules;

public class RecurringSchedule : ISchedule
{
    public DateTime StartAt { get; set; }
    public TimeSpan Interval { get; set; }
}