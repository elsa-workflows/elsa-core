namespace Elsa.Resilience.Options;

public class SimulateResponseOptions
{
    public int SessionCapacity { get; set; } = 1_000;
    public TimeSpan SessionSlidingExpiration { get; set; } = TimeSpan.FromMinutes(15);
    public int MaxSessionIdLength { get; set; } = 128;
    public int MaxCodesQueryLength { get; set; } = 1_024;
    public int MaxCodes { get; set; } = 32;
}
