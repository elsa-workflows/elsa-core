using System;

namespace Elsa.Services;

public interface IBookmarkSerializer
{
    string Serialize(IBookmark bookmark);
    IBookmark Deserialize(string json, Type bookmarkType);
}

public static class BookmarkSerializerExtensions
{
    public static T Deserialize<T>(this IBookmarkSerializer serializer, string json) where T : IBookmark => (T)serializer.Deserialize(json, typeof(T));
}