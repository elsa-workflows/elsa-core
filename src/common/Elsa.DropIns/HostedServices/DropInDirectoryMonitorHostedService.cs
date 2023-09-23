using Elsa.DropIns.Contracts;
using Elsa.DropIns.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using ThrottleDebounce;

namespace Elsa.DropIns.HostedServices;

public class DropInDirectoryMonitorHostedService : BackgroundService
{
    private readonly IDropInDirectoryLoader _directoryLoader;
    private readonly IDropInStarter _dropInStarter;
    private readonly IOptions<DropInOptions> _options;
    private readonly RateLimitedFunc<string, Task> _debouncedLoader;

    public DropInDirectoryMonitorHostedService(IDropInDirectoryLoader directoryLoader, IDropInStarter dropInStarter, IOptions<DropInOptions> options)
    {
        _directoryLoader = directoryLoader;
        _dropInStarter = dropInStarter;
        _options = options;

        _debouncedLoader = Debouncer.Debounce<string, Task>(LoadDropInAssemblyAsync, TimeSpan.FromSeconds(2));
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var rootDirectoryPath = _options.Value.DropInRootDirectory;

        // Monitor any changes in this directory:
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
        var dropInAssemblyFilename = $"{Path.GetFileNameWithoutExtension(directory)}.dll";
        var assemblyPath = Path.Combine(directory, dropInAssemblyFilename);
        var assembly = _directoryLoader.LoadDropInAssembly(assemblyPath);

        if (assembly != null)
            await _dropInStarter.StartAsync(assembly);
    }
}