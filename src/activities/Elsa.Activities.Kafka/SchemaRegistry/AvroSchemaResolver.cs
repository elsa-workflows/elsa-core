using Confluent.Kafka;
using System.Text;
using System.Threading.Tasks;

using Confluent.SchemaRegistry;
using Avro.Generic;
using Avro.Specific;
using System.IO;
using System.Net;
using System;
using System.Buffers.Binary;
using Avro;
using System.Diagnostics.CodeAnalysis;

namespace Elsa.Activities.Kafka.SchemaRegistry
{
    public class AvroSchemaResolver : ISchemaResolver
    {
        private readonly ISchemaRegistryClient _schemaRegistryClient;
        private readonly Confluent.SchemaRegistry.Serdes.AvroDeserializer<SpecificFixed> _deserializer;

        public AvroSchemaResolver(ISchemaRegistryClient schemaRegistryClient)
        {
            _schemaRegistryClient = schemaRegistryClient;
            _deserializer = new Confluent.SchemaRegistry.Serdes.AvroDeserializer<SpecificFixed>(_schemaRegistryClient);
        }

        public async ValueTask<string> ResolveSchemaForMessage(Confluent.Kafka.Message<Ignore, byte[]> message)
        {
            if (_schemaRegistryClient is not null)
            {
                var schemaId = BinaryPrimitives.ReadInt32BigEndian(
                ((byte[])message.Value).AsSpan().Slice(1, 4));
                var schema = await _schemaRegistryClient.GetSchemaAsync(schemaId);
                var parsed = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(schema.SchemaString);

                return parsed is not null ? parsed["name"] : "";
            }
            return "";
        }
    }
}
