using System.Diagnostics;

namespace Elsa.OpenTelemetry.Helpers;

public class ElsaOpenTelemetry
{
    public static readonly ActivitySource ActivitySource = new("Elsa.Workflows");
}