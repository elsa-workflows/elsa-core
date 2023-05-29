using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Services.Models;
using MimeKit;

namespace Elsa.Activities.Email.Services
{
    public interface ISmtpService
    {
        [Obsolete("Use the overload without the ActivityExecutionContext parameter instead.")]
        Task SendAsync(ActivityExecutionContext context, MimeMessage message, CancellationToken cancellationToken);
        
        Task SendAsync(MimeMessage message, CancellationToken cancellationToken);
    }
}