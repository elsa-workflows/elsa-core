using Elsa.ModularPersistence.Diagnostics;

namespace Elsa.ModularPersistence.Contracts;

public interface IModularPersistenceDiagnosticsService
{
    ModularPersistenceDiagnostics GetDiagnostics();
}
