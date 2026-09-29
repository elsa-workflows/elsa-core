namespace Elsa.Studio.Diagnostics.OpenTelemetry;

/// <summary>Backend permission resources guarding the OpenTelemetry APIs.</summary>
public static class OpenTelemetryPermissions
{
    /// <summary>OpenTelemetry traces, logs, metrics and resources.</summary>
    public const string OpenTelemetry = "diagnostics/opentelemetry";
}
