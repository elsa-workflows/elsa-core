using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Elsa.Attributes;
using Elsa.Serialization.ContractResolvers;
using Newtonsoft.Json;
using NodaTime;
using NodaTime.Serialization.JsonNet;

namespace Elsa.Services.Bookmarks
{
    public class BookmarkHasher : IBookmarkHasher
    {
        public string Hash(IBookmark bookmark)
        {
            var type = bookmark.GetType();
            var whiteListedProperties = FilterProperties(type.GetProperties()).ToList();
            var contractResolver = new WhiteListedPropertiesContractResolver(whiteListedProperties);

            var serializerSettings = new JsonSerializerSettings
            {
                ContractResolver = contractResolver
            }.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);

            var json = JsonConvert.SerializeObject(bookmark, serializerSettings);
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

        private static IEnumerable<PropertyInfo> FilterProperties(IEnumerable<PropertyInfo> properties) => properties.Where(property => property.GetCustomAttribute<ExcludeFromHashAttribute>() == null);
    }
}