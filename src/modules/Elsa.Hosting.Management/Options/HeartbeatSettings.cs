namespace Elsa.Hosting.Management.Options;

public class HeartbeatSettings
{
    public TimeSpan InstanceHeartbeatRhythm { get; set; } = TimeSpan.FromMinutes(1);
    public TimeSpan InstanceDeactivatedPeriod { get; set; } = TimeSpan.FromHours(1);
}