using Elsa.Activities.Temporal;
using Elsa.Attributes;
using Microsoft.Extensions.Logging;
using NodaTime;

namespace Elsa.Samples.Interrupts.Activities
{
    [Activity]
    public class Sleep : Timer
    {
        public Sleep(IClock clock, ILogger<Sleep> logger) : base(clock, logger)
        {
        }
    }
}