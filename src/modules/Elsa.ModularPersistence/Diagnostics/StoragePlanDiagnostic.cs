namespace Elsa.ModularPersistence.Diagnostics;

/// <summary>
/// Describes a validation or planning diagnostic emitted by persistence providers and planners.
/// </summary>
public sealed record StoragePlanDiagnostic(StoragePlanDiagnosticSeverity Severity, string Code, string Message, string Path);
