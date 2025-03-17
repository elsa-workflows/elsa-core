using System;
using System.Diagnostics;

namespace Elsa.OpenTelemetry.Models;

public class ErrorSpanContext(Activity span, Exception? exception)
{
    public Activity Span => span;
    public Exception? Exception => exception;
}