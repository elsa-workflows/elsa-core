using Elsa.ProtoActor.ProtoBuf;
using Elsa.Workflows.Models;

namespace Elsa.ProtoActor.Mappers;

/// <summary>
/// Maps between <see cref="Bookmark"/> and <see cref="ProtoBookmark"/>.
/// </summary>
public class BookmarkInfoMapper
{
    public IEnumerable<ProtoBookmarkInfo> Map(IEnumerable<BookmarkInfo> source)
    {
        return source.Select(Map);
    }

    public IEnumerable<BookmarkInfo> Map(IEnumerable<ProtoBookmarkInfo> source)
    {
        return source.Select(Map);
    }

    public ProtoBookmarkInfo Map(BookmarkInfo source)
    {
        return new ProtoBookmarkInfo
        {
            Id = source.Id,
            Name = source.Name,
            Hash = source.Hash
        };
    }
    
    public BookmarkInfo Map(ProtoBookmarkInfo source)
    {
        return new BookmarkInfo
        {
            Id = source.Id,
            Name = source.Name,
            Hash = source.Hash
        };
    }
}