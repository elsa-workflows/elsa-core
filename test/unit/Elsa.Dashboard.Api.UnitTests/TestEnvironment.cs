using Elsa.Common;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace Elsa.Dashboard.Api.UnitTests;

internal sealed class TestClock(DateTimeOffset utcNow) : ISystemClock
{
    public DateTimeOffset UtcNow { get; } = utcNow;
}

internal sealed class TestHostEnvironment : IHostEnvironment
{
    public string EnvironmentName { get; set; } = "Integration";

    public string ApplicationName { get; set; } = "Elsa.TestHost";

    public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

    public IFileProvider ContentRootFileProvider { get; set; } = null!;
}
