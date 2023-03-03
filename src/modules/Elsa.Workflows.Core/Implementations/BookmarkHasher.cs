using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Services;

namespace Elsa.Workflows.Core.Implementations;

public class BookmarkHasher : IBookmarkHasher
{
    private readonly IBookmarkPayloadSerializer _bookmarkPayloadSerializer;
    private readonly IHasher _hasher;

    public BookmarkHasher(IBookmarkPayloadSerializer bookmarkPayloadSerializer, IHasher hasher)
    {
        _bookmarkPayloadSerializer = bookmarkPayloadSerializer;
        _hasher = hasher;
    }

    public string Hash(string activityTypeName, object? payload)
    {
        var json = payload != null ? _bookmarkPayloadSerializer.Serialize(payload) : null;
        return Hash(activityTypeName, json);
    }

    public string Hash(string activityTypeName, string? serializedPayload)
    {
        var input = $"{activityTypeName}{(!string.IsNullOrWhiteSpace(serializedPayload) ? ":" + serializedPayload : "")}";
        var hash = _hasher.Hash(input);

        return hash;
    }
}