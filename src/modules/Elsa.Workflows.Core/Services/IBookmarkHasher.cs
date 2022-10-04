namespace Elsa.Workflows.Core.Services;

public interface IBookmarkHasher
{
    string Hash(string activityTypeName, object? payload);
    string Hash(string activityTypeName, string? serializedPayload);
}