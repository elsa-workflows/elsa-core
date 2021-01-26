using System.Security.Cryptography;
using System.Text;
using Elsa.Serialization;
using Newtonsoft.Json;
using NodaTime;
using NodaTime.Serialization.JsonNet;

namespace Elsa.Triggers
{
    public class WorkflowTriggerHasher : IWorkflowTriggerHasher
    {
        private readonly JsonSerializerSettings _serializerSettings;

        public WorkflowTriggerHasher()
        {
            _serializerSettings = new JsonSerializerSettings().ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
        }
        
        public string Hash(ITrigger trigger)
        {
            var json = JsonConvert.SerializeObject(trigger, _serializerSettings);
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