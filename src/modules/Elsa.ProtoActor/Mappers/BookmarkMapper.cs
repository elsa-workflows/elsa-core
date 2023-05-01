using Elsa.ProtoActor.Extensions;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Models;
using ProtoBookmark = Elsa.ProtoActor.Protos.Bookmark;

namespace Elsa.ProtoActor.Mappers;

/// <summary>
/// Maps between <see cref="Bookmark"/> and <see cref="ProtoBookmark"/>.
/// </summary>
public class BookmarkMapper
{
    private readonly IBookmarkPayloadSerializer _bookmarkPayloadSerializer;

    /// <summary>
    /// Initializes a new instance of <see cref="BookmarkMapper"/>.
    /// </summary>
    public BookmarkMapper(IBookmarkPayloadSerializer bookmarkPayloadSerializer)
    {
        _bookmarkPayloadSerializer = bookmarkPayloadSerializer;
    }
    
    public Bookmark Map(ProtoBookmark bookmark) =>
        new(bookmark.Id, bookmark.Name, bookmark.Hash, bookmark.Payload, bookmark.ActivityNodeId, bookmark.ActivityInstanceId, bookmark.AutoBurn, bookmark.CallbackMethodName);

    
    public IEnumerable<ProtoBookmark> Map(IEnumerable<Bookmark> source) =>   
        source.Select(x =>
            new ProtoBookmark
            {
                Id = x.Id,
                Name = x.Name,
                Hash = x.Hash,
                Payload = x.Payload != null ? _bookmarkPayloadSerializer.Serialize(x.Payload) : string.Empty,
                ActivityNodeId = x.ActivityNodeId,
                ActivityInstanceId = x.ActivityInstanceId,
                AutoBurn = x.AutoBurn,
                CallbackMethodName = x.CallbackMethodName.EmptyIfNull()
            });
    
    public IEnumerable<Bookmark> Map(IEnumerable<ProtoBookmark> source) =>
        source.Select(x =>
            new Bookmark(
                x.Id,
                x.Name,
                x.Hash,
                x.Payload.NullIfEmpty(),
                x.ActivityNodeId,
                x.ActivityInstanceId,
                x.AutoBurn,
                x.CallbackMethodName.NullIfEmpty()));
}