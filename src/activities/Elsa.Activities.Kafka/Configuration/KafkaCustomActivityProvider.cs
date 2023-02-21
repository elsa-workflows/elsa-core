using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Activities.Kafka.Configuration
{
    public class KafkaCustomActivityProvider : IKafkaCustomActivityProvider
    {
        public List<string>? KafkaOverrideTriggers { get; set; }

    }
}
