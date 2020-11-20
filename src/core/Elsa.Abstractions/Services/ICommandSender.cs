using System.Collections.Generic;
using System.Threading.Tasks;

namespace Elsa.Services
{
    public interface ICommandSender
    {
        Task SendAsync(object message, IDictionary<string, string>? headers = default);
    }
}