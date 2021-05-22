using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Elsa.Services
{
    public class RedisBus
    {
        /// <summary>
        /// TODO: Design multi-tenancy.
        /// </summary>
        private const string? TenantId = default;
        
        private readonly string _hostName;
        private readonly string _channelPrefix;
        private readonly string _messagePrefix;
        private readonly IConnectionMultiplexer _multiplexer;
        private readonly ILogger _logger;

        public RedisBus(IConnectionMultiplexer multiplexer, IContainerNameAccessor containerNameAccessor, ILogger<RedisBus> logger)
        {
            _multiplexer = multiplexer;
            _hostName = Dns.GetHostName() + ':' + Process.GetCurrentProcess().Id;
            _channelPrefix = (TenantId ?? "Default") + ':';
            _messagePrefix = _hostName + '/';
            _logger = logger;
        }

        public async Task SubscribeAsync(string channel, Action<string, string> handler)
        {
            try
            {
                var subscriber = _multiplexer.GetSubscriber();

                await subscriber.SubscribeAsync(_channelPrefix + channel, (redisChannel, redisValue) =>
                {
                    var tokens = redisValue.ToString().Split('/').ToArray();

                    if (tokens.Length != 2 || tokens[0].Length == 0 || tokens[0].Equals(_hostName, StringComparison.OrdinalIgnoreCase))
                        return;

                    handler(channel, tokens[1]);
                });
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Unable to subscribe to channel {ChannelName}", _channelPrefix + channel);
            }
        }

        public async Task PublishAsync(string channel, string message)
        {
            try
            {
                await _multiplexer.GetSubscriber().PublishAsync(_channelPrefix + channel, _messagePrefix + message);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Unable to publish to channel {ChannelName}", _channelPrefix + channel);
            }
        }
    }
}