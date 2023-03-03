namespace Elsa.Workflows.Core.Contracts;

public interface IBookmarkHasher
{
    string Hash(string activityTypeName, object? payload);
    string Hash(string activityTypeName, string? serializedPayload);
}