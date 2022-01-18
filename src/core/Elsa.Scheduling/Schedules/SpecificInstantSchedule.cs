using System;
using Elsa.Scheduling.Contracts;

namespace Elsa.Scheduling.Schedules;

public record SpecificInstantSchedule(DateTime DateTime) : ISchedule;