namespace Elsa.OpenTelemetry.Options;

public class OpenTelemetryOptions
{
    public bool UseNewRootActivityForRemoteParent { get; set; }
    public bool UseDummyParentActivityAsRootSpan { get; set; }
}