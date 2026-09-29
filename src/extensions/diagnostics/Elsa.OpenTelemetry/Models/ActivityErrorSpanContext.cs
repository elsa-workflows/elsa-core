using System.Diagnostics;

namespace Elsa.OpenTelemetry.Models;

public class ActivityErrorSpanContext(Activity span, Exception? exception)
{
    public Activity Span => span;
    public Exception? Exception => exception;
}