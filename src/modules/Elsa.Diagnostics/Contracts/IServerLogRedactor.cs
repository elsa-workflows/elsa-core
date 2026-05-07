using Elsa.Diagnostics.Models;

namespace Elsa.Diagnostics.Contracts;

public interface IServerLogRedactor
{
    ServerLogEvent Redact(ServerLogEvent logEvent);
}
