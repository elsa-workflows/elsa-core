using Elsa.Services;
using System.Collections.Generic;
using MQTTnet.Protocol;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Mqtt.Activities.MqttMessageReceived;
using Elsa.Attributes;

namespace Elsa.Activities.Mqtt.Bookmarks
{
    public class MessageReceivedBookmark : IBookmark
    {
        public MessageReceivedBookmark()
        {
        }

        public MessageReceivedBookmark(string topic, string host, int port, string username, string password, MqttQualityOfServiceLevel qos)
        {
            Topic = topic;
            Host = host;
            Port = port;
            Username = username;
            Password = password;
            Qos = qos;
        }

        public string Topic { get; set; } = default!;
        public string Host { get; set; } = default!;
        public int Port { get; set; }
        public string Username { get; set; } = default!;
        public string Password { get; set; } = default!;
        [ExcludeFromHash] public MqttQualityOfServiceLevel Qos { get; set; }
    }

    public class MessageReceivedBookmarkProvider : BookmarkProvider<MessageReceivedBookmark, MqttMessageReceived>
    {
        public override async ValueTask<IEnumerable<BookmarkResult>> GetBookmarksAsync(BookmarkProviderContext<MqttMessageReceived> context, CancellationToken cancellationToken) =>
            new[]
            {
                Result(new MessageReceivedBookmark
                {
                    Topic = (await context.ReadActivityPropertyAsync(x => x.Topic, cancellationToken))!,
                    Host = (await context.ReadActivityPropertyAsync(x => x.Host, cancellationToken))!,
                    Port = (await context.ReadActivityPropertyAsync(x => x.Port, cancellationToken))!,
                    Username = (await context.ReadActivityPropertyAsync(x => x.Username, cancellationToken))!,
                    Password = (await context.ReadActivityPropertyAsync(x => x.Password, cancellationToken))!,
                    Qos = (await context.ReadActivityPropertyAsync(x => x.QualityOfService, cancellationToken))!
                })
            };
    }
}