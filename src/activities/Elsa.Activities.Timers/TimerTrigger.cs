using System;
using Elsa.Attributes;
using Elsa.Models;

namespace Elsa.Activities.Timers;

public class TimerTrigger : Trigger
{
    [Input] public Input<TimeSpan> Interval { get; set; } = default!;
}