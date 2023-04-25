using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Activities.Kafka.Configuration
{
    public class KafkaOptions
    {
        /// <summary>
        /// Username for BasicAuth Connection
        /// </summary>
        public string? SaslUsername { get; set; }

        /// <summary>
        /// Password for BasicAuth Connection
        /// </summary>
        public string? SaslPassword { get; set; }

        public Confluent.Kafka.SaslMechanism SaslMechanism { get; set; }

        public Confluent.Kafka.SecurityProtocol SecurityProtocol { get; set; }
    }
}
