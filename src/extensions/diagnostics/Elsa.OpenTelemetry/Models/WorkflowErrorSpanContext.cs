using System.Diagnostics;
using Elsa.Workflows.Models;

namespace Elsa.OpenTelemetry.Models;

public class WorkflowErrorSpanContext(Activity span, ActivityIncident incident, Exception? exception)
{
    public Activity Span => span;
    public ActivityIncident Incident => incident;
    public Exception? Exception { get; } = exception;
}