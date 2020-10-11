using Microsoft.Extensions.Localization;

namespace Elsa.Services.Models
{
    public interface IWorkflowFault
    {
        IActivity? FaultedActivity { get; }
        LocalizedString? Message { get; }
    }
}