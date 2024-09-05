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
    private readonly RateLimitedFunc<string, Task> _debouncedUnloader;
    private readonly FileSystemWatcher _watcher;

    private readonly Dictionary<string, List<IDropIn>> _installedDropIns;


    /// <inheritdoc />
    public DropInDirectoryMonitorHostedService(IOptions<DropInOptions> options, IServiceProvider serviceProvider)
    {
        _options = options;
        _serviceProvider = serviceProvider;
        _debouncedLoader = Debouncer.Debounce<string, Task>(LoadDropInAssemblyAsync, TimeSpan.FromSeconds(2));
        _debouncedUnloader = Debouncer.Debounce<string, Task>(UnloadDropInAssemblyAsync, TimeSpan.FromSeconds(2));

        _installedDropIns = [];

        var rootDirectoryPath = _options.Value.DropInRootDirectory;

        if (!Directory.Exists(rootDirectoryPath))
            Directory.CreateDirectory(rootDirectoryPath);

        _watcher = new FileSystemWatcher(rootDirectoryPath)
        {
            EnableRaisingEvents = true,
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName
        };
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await LoadDropInAssemblyAsync(_options.Value.DropInRootDirectory);

        _watcher.Changed += OnChanged;
        _watcher.Deleted += OnDeleted;
    }

    private async void OnChanged(object sender, FileSystemEventArgs e)
    {
        var task = _debouncedLoader.Invoke(e.FullPath);

        if (task == null)
            return;

        await task;
    }

    private async void OnDeleted(object sender, FileSystemEventArgs e)
    {
        var task = _debouncedUnloader.Invoke(e.FullPath);

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
            if(_installedDropIns.TryGetValue(fullPath, out var installedDropIns))
            {
                installedDropIns.Add(dropIn);
            }
            else
            {
                _installedDropIns[fullPath] = [dropIn];
            }
            await dropIn.ConfigureAsync(_serviceProvider, CancellationToken.None);
        }
    }

    private Task UnloadDropInAssemblyAsync(string fullPath)
    {
        if (_installedDropIns.TryGetValue(fullPath, out var installedDropIn))
        {
            installedDropIn.ForEach(dropIn => dropIn.Unconfigure(_serviceProvider));
            _installedDropIns.Remove(fullPath);
        }
        return Task.CompletedTask;
    }
}