using System.Security.Cryptography;
using System.Text;
using Elsa.Serialization;

namespace Elsa.Triggers
{
    public class WorkflowTriggerHasher : IWorkflowTriggerHasher
    {
        private readonly IContentSerializer _contentSerializer;

        public WorkflowTriggerHasher(IContentSerializer contentSerializer)
        {
            _contentSerializer = contentSerializer;
        }
        
        public string Hash(ITrigger trigger)
        {
            var json = _contentSerializer.Serialize(trigger);
            return Hash(json);
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