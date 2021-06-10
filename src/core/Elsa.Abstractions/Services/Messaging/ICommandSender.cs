using System.Collections.Generic;
using System.Threading.Tasks;
using NodaTime;

namespace Elsa.Services
{
    /// <summary>
    /// Sends a command via AMQP (Rebus).
    /// </summary>
    public interface ICommandSender
    {
        Task SendAsync(object message, IDictionary<string, string>? headers = default);
        Task DeferAsync(object message, Duration delay, IDictionary<string, string>? headers = default);
    }
}