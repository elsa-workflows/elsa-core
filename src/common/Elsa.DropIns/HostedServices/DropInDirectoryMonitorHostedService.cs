using Elsa.DropIns.Catalogs;
using Elsa.DropIns.Core;
using Elsa.DropIns.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using ThrottleDebounce;

namespace Elsa.DropIns.HostedServices;

/// <summary>
/// Monitors the drop-in directory for changes and loads any new drop-ins.
/// </summary>
public class DropInDirectoryMonitorHostedService : BackgroundService
{
    private readonly IOptions<DropInOptions> _options;
    private readonly IServiceProvider _serviceProvider;
    private readonly RateLimitedFunc<string, Task> _debouncedLoader;

    /// <inheritdoc />
    public DropInDirectoryMonitorHostedService(IOptions<DropInOptions> options, IServiceProvider serviceProvider)
    {
        _options = options;
        _serviceProvider = serviceProvider;
        _debouncedLoader = Debouncer.Debounce<string, Task>(LoadDropInAssemblyAsync, TimeSpan.FromSeconds(2));
    }

    /// <inheritdoc />
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var rootDirectoryPath = _options.Value.DropInRootDirectory;

        if (!Directory.Exists(rootDirectoryPath)) 
            Directory.CreateDirectory(rootDirectoryPath);

        var watcher = new FileSystemWatcher(rootDirectoryPath)
        {
            EnableRaisingEvents = true,
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName
        };

        watcher.Changed += OnChanged;
        return Task.CompletedTask;
    }

    private async void OnChanged(object sender, FileSystemEventArgs e)
    {
        var task = _debouncedLoader.Invoke(e.FullPath);

        if (task == null)
            return;

        await task;
    }

    private async Task LoadDropInAssemblyAsync(string fullPath)
    {
        var directory = Path.GetDirectoryName(fullPath)!;
        var directoryCatalog = new DirectoryDropInCatalog(directory);
        var dropInDescriptors = directoryCatalog.List();

        foreach (var dropInDescriptor in dropInDescriptors)
        {
            var dropIn = (IDropIn)Activator.CreateInstance(dropInDescriptor.Type)!;
            await dropIn.ConfigureAsync(_serviceProvider, CancellationToken.None);
        }
    }
}