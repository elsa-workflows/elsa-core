using System.Collections.Generic;
using Elsa.Services;

namespace Elsa.Activities.RabbitMq
{
    public interface IRabbitMqActivity : IActivity
    {
        string ConnectionString { get; set; }
        string ExchangeName { get; set; }
        string RoutingKey { get; set; }
        Dictionary<string, string> Headers { get; set; }
    }
}
