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

        /// <summary>
        /// Mechanism to be used for SASL authentication
        /// </summary>
        public Confluent.Kafka.SaslMechanism SaslMechanism { get; set; }

        /// <summary>
        /// Security protocol to be used for communication with brokers
        /// </summary>
        public Confluent.Kafka.SecurityProtocol SecurityProtocol { get; set; }
    }
}
