using Elsa.Models;
using Microsoft.Extensions.Localization;

namespace Elsa.Services.Models
{
    public interface IWorkflowFault
    {
        string? FaultedActivityId { get; }
        LocalizedString? Message { get; }
    }
}