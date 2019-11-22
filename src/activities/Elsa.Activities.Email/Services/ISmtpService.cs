using System.Threading;
using System.Threading.Tasks;
using MimeKit;

namespace Elsa.Activities.Email.Services
{
    public interface ISmtpService
    {
        Task SendAsync(MimeMessage message, CancellationToken cancellationToken);
    }
}