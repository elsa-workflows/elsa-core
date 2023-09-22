using Elsa.Features.Services;

namespace Elsa.DropIns.Core;

public interface IDropInStartup
{
    void ConfigureModule(IModule elsa);
}