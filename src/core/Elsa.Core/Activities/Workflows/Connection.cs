using Elsa.Contracts;

namespace Elsa.Activities.Workflows;

public record Connection(IActivity Source, IActivity Target, string? SourcePort, string TargetPort);