using System;
using Elsa.Activities.Scheduling.Contracts;

namespace Elsa.Activities.Scheduling.Schedules;

public class SpecificInstantSchedule : ISchedule
{
    public DateTime DateTime { get; set; }
}