using Elsa.DropIns.Contracts;
using Elsa.DropIns.Core;
using Elsa.DropIns.Options;
using Elsa.Features.Services;
using Microsoft.Extensions.Options;

namespace Elsa.DropIns.Services;

public class DropInInstaller : IDropInInstaller
{
    private readonly IDropInDirectoryLoader _directoryLoader;
    private readonly ITypeFinder _typeFinder;
    private readonly IOptions<DropInOptions> _options;

    public DropInInstaller(IDropInDirectoryLoader directoryLoader, ITypeFinder typeFinder, IOptions<DropInOptions> options)
    {
        _directoryLoader = directoryLoader;
        _typeFinder = typeFinder;
        _options = options;
    }
    
    public void Install(IModule module)
    {
        var dropInRootDirectory = _options.Value.DropInRootDirectory;
        var dropInAssemblies = _directoryLoader.LoadDropInAssembliesFromRootDirectory(dropInRootDirectory).ToList();
        var dropInTypes = _typeFinder.FindImplementationsOf<IDropIn>(dropInAssemblies).ToList();
        
        foreach (var type in dropInTypes)
        {
            var dropIn = (IDropIn)Activator.CreateInstance(type)!;
            dropIn.ConfigureModule(module);
        }
    }
}