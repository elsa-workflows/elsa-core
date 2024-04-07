using Elsa.Extensions;
using Elsa.ProtoActor.ProtoBuf;
using Elsa.Workflows.Helpers;
using Elsa.Workflows.Models;

namespace Elsa.ProtoActor.Mappers;

/// <summary>
/// Maps between <see cref="Bookmark"/> and <see cref="ProtoBookmark"/>.
/// </summary>
public class BookmarkDiffMapper(BookmarkInfoMapper bookmarkInfoMapper)
{
    
    public ProtoBookmarkDiff Map(Diff<BookmarkInfo> source)
    {
        var destination = new ProtoBookmarkDiff();
        
        destination.Added.AddRange(bookmarkInfoMapper.Map(source.Added));
        destination.Removed.AddRange(bookmarkInfoMapper.Map(source.Removed));
        
        return destination;
    }

    public Diff<BookmarkInfo> Map(ProtoBookmarkDiff source)
    {
        var destination = Diff.Empty<BookmarkInfo>();
        
        destination.Added.AddRange(bookmarkInfoMapper.Map(source.Added));
        destination.Removed.AddRange(bookmarkInfoMapper.Map(source.Removed));
        
        return destination;
    }
}