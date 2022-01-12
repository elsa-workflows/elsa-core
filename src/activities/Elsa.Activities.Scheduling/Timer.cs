using System;
using Elsa.Attributes;
using Elsa.Models;

namespace Elsa.Activities.Scheduling;

public class Timer : Trigger
{
    [Input] public Input<TimeSpan> Interval { get; set; } = default!;
}