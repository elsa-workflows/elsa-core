using Elsa.Diagnostics.StructuredLogs.Models;

namespace Elsa.Diagnostics.StructuredLogs.Contracts;

public interface IStructuredLogRedactor
{
    StructuredLogEvent Redact(StructuredLogEvent logEvent);
}
