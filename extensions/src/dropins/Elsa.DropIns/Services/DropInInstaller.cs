using Elsa.DropIns.Catalogs;
using Elsa.DropIns.Contracts;
using Elsa.DropIns.Core;
using Elsa.DropIns.Options;
using Elsa.Features.Services;
using Microsoft.Extensions.Options;

namespace Elsa.DropIns.Services;

/// <inheritdoc />
public class DropInInstaller : IDropInInstaller
{
    private readonly IOptions<DropInOptions> _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="DropInInstaller"/> class.
    /// </summary>
    public DropInInstaller(IOptions<DropInOptions> options)
    {
        _options = options;
    }

    /// <inheritdoc />
    public void Install(IModule module)
    {
        var dropInRootDirectory = _options.Value.DropInRootDirectory;
        var directoryCatalog = new DirectoryDropInCatalog(dropInRootDirectory);
        var dropInDescriptors = directoryCatalog.List();
        
        foreach (var dropInDescriptor in dropInDescriptors)
        {
            var dropIn = (IDropIn)Activator.CreateInstance(dropInDescriptor.Type)!;
            dropIn.Install(module);
        }
    }
}