using Elsa.ProtoActor.Extensions;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Models;
using ProtoBookmark = Elsa.ProtoActor.Protos.Bookmark;

namespace Elsa.ProtoActor.Mappers;

public class BookmarkMapper
{
    private readonly IBookmarkPayloadSerializer _bookmarkPayloadSerializer;

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
                Payload = x.Payload != null ? _bookmarkPayloadSerializer.Serialize(x.Payload) : default,
                ActivityNodeId = x.ActivityNodeId,
                ActivityInstanceId = x.ActivityInstanceId,
                AutoBurn = x.AutoBurn,
                CallbackMethodName = x.CallbackMethodName.NullIfEmpty()
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