using Elsa.DropIns.Core;
using Elsa.Extensions;
using Elsa.Features.Services;

namespace SampleDropIn;

public class DropInStartup : IDropInStartup
{
    public void ConfigureModule(IModule elsa)
    {
        elsa.AddActivitiesFrom<DropInStartup>();
    }
}