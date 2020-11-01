using Elsa.Models;
using Elsa.Services.Models;

namespace Elsa.Services
{
    public interface IActivityFactory
    {
        ActivityInstance Instantiate(IActivityBlueprint activityBlueprint);
    }
}