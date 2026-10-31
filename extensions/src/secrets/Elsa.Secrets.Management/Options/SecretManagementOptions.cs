namespace Elsa.Secrets.Management;

public class SecretManagementOptions
{
    /// <summary>
    /// The interval at which the background sweep should run for expired secrets.
    /// </summary>
    public TimeSpan SweepInterval { get; set; } = TimeSpan.FromHours(12);
}