using NodaTime;

namespace Elsa.Options
{
    public class CooldownLoopHandlerOptions
    {
        public Duration CooldownPeriod { get; set; } = Duration.FromMinutes(1);
        public int MaxCooldownEvents { get; set; } = 10;
    }
}