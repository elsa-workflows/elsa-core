using System.Threading;
using System.Threading.Tasks;
using Elsa.Services.Models;
using MimeKit;

namespace Elsa.Activities.Email.Services
{
    public interface ISmtpService
    {
        Task SendAsync(ActivityExecutionContext context, MimeMessage message, CancellationToken cancellationToken);
    }
}