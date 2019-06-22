using Elsa.Services.Models;

namespace Elsa.Services
{
    public interface IActivityResolver
    {
        IActivity ResolveActivity(string activityTypeName);
    }
}