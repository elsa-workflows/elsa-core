using System;
using Elsa.Scheduling.Contracts;

namespace Elsa.Scheduling.Schedules;

public class SpecificInstantSchedule : ISchedule
{
    public DateTime DateTime { get; set; }
}