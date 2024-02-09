namespace Elsa.Hosting.Management.Options;

public class HeartbeatSettings
{
    public TimeSpan InstanceHeartbeatRythm { get; set; } = TimeSpan.FromMinutes(1);
    public TimeSpan InstanceDeactivatedPeriod { get; set; } = TimeSpan.FromHours(1);
}