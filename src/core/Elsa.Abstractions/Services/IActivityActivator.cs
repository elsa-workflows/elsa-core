using System;
using System.Threading.Tasks;
using Elsa.Services.Models;

namespace Elsa.Services
{
    public interface IActivityActivator
    {
        Task<IActivity> ActivateActivityAsync(ActivityExecutionContext context, Type type);
    }
}