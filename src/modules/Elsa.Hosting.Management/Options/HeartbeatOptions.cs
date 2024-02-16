namespace Elsa.Hosting.Management.Options;

public class HeartbeatOptions
{
    public TimeSpan InstanceHeartbeatRhythm { get; set; } = TimeSpan.FromMinutes(1);
    public TimeSpan HeartbeatTimeoutPeriod { get; set; } = TimeSpan.FromHours(1);
}