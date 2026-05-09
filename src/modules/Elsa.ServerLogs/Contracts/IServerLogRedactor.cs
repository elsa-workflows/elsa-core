using Elsa.ServerLogs.Models;

namespace Elsa.ServerLogs.Contracts;

public interface IServerLogRedactor
{
    ServerLogEvent Redact(ServerLogEvent logEvent);
}
