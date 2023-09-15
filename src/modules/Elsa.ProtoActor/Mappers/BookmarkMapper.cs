using Elsa.ProtoActor.Extensions;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Models;
using ProtoBookmark = Elsa.ProtoActor.ProtoBuf.Bookmark;

namespace Elsa.ProtoActor.Mappers;

/// <summary>
/// Maps between <see cref="Bookmark"/> and <see cref="ProtoBookmark"/>.
/// </summary>
internal class BookmarkMapper
{
    private readonly IBookmarkPayloadSerializer _bookmarkPayloadSerializer;

    /// <summary>
    /// Initializes a new instance of <see cref="BookmarkMapper"/>.
    /// </summary>
    public BookmarkMapper(IBookmarkPayloadSerializer bookmarkPayloadSerializer)
    {
        _bookmarkPayloadSerializer = bookmarkPayloadSerializer;
    }


    public IEnumerable<ProtoBookmark> Map(IEnumerable<Bookmark> source) =>
        source.Select(bookmark =>
            new ProtoBookmark
            {
                Id = bookmark.Id,
                Name = bookmark.Name,
                Hash = bookmark.Hash,
                Payload = bookmark.Payload != null ? _bookmarkPayloadSerializer.Serialize(bookmark.Payload) : string.Empty,
                ActivityId = bookmark.ActivityId,
                ActivityNodeId = bookmark.ActivityNodeId,
                ActivityInstanceId = bookmark.ActivityInstanceId,
                AutoBurn = bookmark.AutoBurn,
                CallbackMethodName = bookmark.CallbackMethodName.EmptyIfNull(),
                CreatedAt = bookmark.CreatedAt.ToString("O"),
                Metadata = { bookmark.Metadata ?? new Dictionary<string, string>() }
            });

    public IEnumerable<Bookmark> Map(IEnumerable<ProtoBookmark> source) =>
        source.Select(bookmark =>
            new Bookmark(
                bookmark.Id,
                bookmark.Name,
                bookmark.Hash,
                bookmark.Payload.NullIfEmpty(),
                bookmark.ActivityId,
                bookmark.ActivityNodeId,
                bookmark.ActivityInstanceId,
                DateTimeOffset.Parse(bookmark.CreatedAt),
                bookmark.AutoBurn,
                bookmark.CallbackMethodName.NullIfEmpty(),
                bookmark.Metadata.ToDictionary(x => x.Key, x => x.Value)));
}