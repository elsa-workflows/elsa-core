namespace Elsa.ProtoActor.Options;

public class ClusterConfigurationSettings
{
    public TimeSpan HeartBeatExpiration { get; set; } = TimeSpan.FromDays(1);
    public TimeSpan ActorRequestTimeout { get; set; } = TimeSpan.FromHours(1);
    public TimeSpan ActorActivationTimeout { get; set; } = TimeSpan.FromHours(1);
    public TimeSpan ActorSpawnTimeout { get; set; } = TimeSpan.FromHours(1);
}