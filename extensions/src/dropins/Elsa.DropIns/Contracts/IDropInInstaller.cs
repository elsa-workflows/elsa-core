using Elsa.Features.Services;

namespace Elsa.DropIns.Contracts;

public interface IDropInInstaller
{
    void Install(IModule module);
}