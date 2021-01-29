using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using NodaTime;
using NodaTime.Serialization.JsonNet;

namespace Elsa.Bookmarks
{
    public class BookmarkHasher : IBookmarkHasher
    {
        private readonly JsonSerializerSettings _serializerSettings;

        public BookmarkHasher()
        {
            _serializerSettings = new JsonSerializerSettings().ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
        }
        
        public string Hash(IBookmark bookmark)
        {
            var json = JsonConvert.SerializeObject(bookmark, _serializerSettings);
            var hash = Hash(json); 
            return hash;
        }

        private static string Hash(string input)
        {
            using var sha = new SHA256Managed();
            return Hash(sha, input);
        }

        private static string Hash(HashAlgorithm hashAlgorithm, string input)
        {
            var data = hashAlgorithm.ComputeHash(Encoding.UTF8.GetBytes(input));
            var builder = new StringBuilder();

            foreach (var t in data)
                builder.Append(t.ToString("x2"));

            return builder.ToString();
        }
    }
}