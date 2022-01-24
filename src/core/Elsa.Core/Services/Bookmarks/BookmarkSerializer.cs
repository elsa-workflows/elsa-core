using System;
using Newtonsoft.Json;
using NodaTime;
using NodaTime.Serialization.JsonNet;

namespace Elsa.Services.Bookmarks;

public class BookmarkSerializer : IBookmarkSerializer
{
    private readonly JsonSerializerSettings _serializerSettings;

    public BookmarkSerializer() => _serializerSettings = new JsonSerializerSettings().ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
    public string Serialize(IBookmark bookmark) => JsonConvert.SerializeObject(bookmark, _serializerSettings);
    public IBookmark Deserialize(string json, Type bookmarkType) => (IBookmark)JsonConvert.DeserializeObject(json, bookmarkType, _serializerSettings)!;
}