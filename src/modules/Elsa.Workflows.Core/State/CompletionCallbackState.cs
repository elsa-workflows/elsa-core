// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
// Required for JSON serialization configured with reference handling.
namespace Elsa.Workflows.Core.State;

// Can't use records when using System.Text.Json serialization and reference handling. Hence, using a class with default constructor.
//public record CompletionCallbackState(string OwnerId, string ChildId, string MethodName);

public class CompletionCallbackState
{
    // ReSharper disable once UnusedMember.Global
    // Required for JSON serialization configured with reference handling.
    public CompletionCallbackState()
    {
    }

    public CompletionCallbackState(string ownerId, string childId, string methodName)
    {
        OwnerId = ownerId;
        ChildId = childId;
        MethodName = methodName;
    }

    public string OwnerId { get; init; } = default!;
    public string ChildId { get; init; } = default!;
    public string MethodName { get; init; } = default!;
}