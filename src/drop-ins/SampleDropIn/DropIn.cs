using Elsa.DropIns.Core;
using Elsa.Extensions;
using Elsa.Features.Services;

namespace SampleDropIn;

public class DropIn : IDropIn
{
    public void ConfigureModule(IModule module)
    {
        module.AddActivitiesFrom<DropIn>();
    }
}