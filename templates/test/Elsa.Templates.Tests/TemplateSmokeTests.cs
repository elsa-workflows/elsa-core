using System.Diagnostics;
using System.IO.Compression;
using System.Text.Json;
using System.Xml.Linq;
using Xunit;

namespace Elsa.Templates.Tests;

public class TemplateSmokeTests : IClassFixture<TemplatePackageFixture>
{
    private readonly TemplatePackageFixture _fixture;

    public TemplateSmokeTests(TemplatePackageFixture fixture)
    {
        _fixture = fixture;
    }

    [Theory]
    [InlineData("static")]
    [InlineData("shell")]
    public async Task ElsaServerTemplateBuilds(string featureModel)
    {
        var projectName = $"Sample.{featureModel}.Server";
        await GenerateAndBuildAsync("elsa-server", projectName, "--feature-model", featureModel);
    }

    [Theory]
    [InlineData("sqlite")]
    [InlineData("sqlserver")]
    [InlineData("postgresql")]
    [InlineData("oracle")]
    public async Task ElsaServerTemplateBuildsWithStaticPersistence(string persistence)
    {
        var projectName = $"Sample.{ToPascalCase(persistence)}.Server";
        await GenerateAndBuildAsync("elsa-server", projectName, "--feature-model", "static", "--persistence", persistence);
    }

    [Fact]
    public async Task ElsaServerTemplateBuildsWithShellPersistence()
    {
        await GenerateAndBuildAsync("elsa-server", "Sample.Shell.PostgreSql.Server", "--feature-model", "shell", "--persistence", "postgresql");
    }

    [Theory]
    [InlineData("server")]
    [InlineData("wasm")]
    [InlineData("hybrid")]
    public async Task ElsaStudioTemplateBuilds(string hosting)
    {
        var solutionName = $"Sample.{hosting}.Studio";
        await GenerateAndBuildAsync("elsa-studio", solutionName, "--hosting", hosting);
    }

    [Theory]
    [InlineData("server", "open-id-connect")]
    [InlineData("wasm", "elsa-login")]
    [InlineData("hybrid", "open-id-connect")]
    public async Task ElsaStudioTemplateBuildsWithAuthAndLabelsModule(string hosting, string authProvider)
    {
        var solutionName = $"Sample.{ToPascalCase(hosting)}.{ToPascalCase(authProvider)}.Studio";
        var options = new[] { "--hosting", hosting, "--auth-provider", authProvider, "--with-labels" };

        await GenerateAndBuildAsync("elsa-studio", solutionName, options);
    }

    [Theory]
    [InlineData("static", "server")]
    [InlineData("static", "wasm")]
    [InlineData("static", "hybrid")]
    [InlineData("shell", "server")]
    [InlineData("shell", "wasm")]
    [InlineData("shell", "hybrid")]
    public async Task ElsaCombinedTemplateBuilds(string featureModel, string studioHosting)
    {
        var solutionName = $"Sample.{ToPascalCase(featureModel)}.{ToPascalCase(studioHosting)}.Combined";
        await GenerateAndBuildAsync("elsa-combined", solutionName, "--feature-model", featureModel, "--studio-hosting", studioHosting);
    }

    [Fact]
    public async Task ElsaCombinedTemplateBuildsWithPersistenceAuthAndLabelsModule()
    {
        var solutionName = "Sample.Static.Hybrid.PostgreSql.OpenIdConnect.Combined";
        var options = new[]
        {
            "--feature-model", "static",
            "--studio-hosting", "hybrid",
            "--persistence", "postgresql",
            "--auth-provider", "open-id-connect",
            "--with-labels"
        };

        await GenerateAndBuildAsync("elsa-combined", solutionName, options);
    }

    private static string ToPascalCase(string value)
    {
        var parts = value.Split('-', StringSplitOptions.RemoveEmptyEntries);
        return string.Concat(parts.Select(part => string.Concat(part[..1].ToUpperInvariant(), part[1..])));
    }

    private async Task GenerateAndBuildAsync(string templateName, string solutionName, params string[] templateOptions)
    {
        await using var workspace = TempWorkspace.Create();
        var outputPath = Path.Combine(workspace.Path, "output");
        var hivePath = Path.Combine(workspace.Path, "hive");

        Directory.CreateDirectory(outputPath);
        Directory.CreateDirectory(hivePath);

        await DotNet.RunAsync("new", "install", _fixture.PackagePath, "--debug:custom-hive", hivePath);

        var newArguments = new List<string>
        {
            "new",
            templateName,
            "-n",
            solutionName,
            "-o",
            outputPath
        };
        newArguments.AddRange(templateOptions);
        newArguments.AddRange(["--debug:custom-hive", hivePath]);

        await DotNet.RunAsync(newArguments.ToArray());
        AssertGeneratedAppsettingsAreValid(outputPath);
        AssertHealthChecksUseDedicatedEndpoint(outputPath);
        AssertPackageVersions(outputPath, TemplatePackageFixture.ElsaVersion, TemplatePackageFixture.CShellsVersion);
        if (templateName is "elsa-studio" or "elsa-combined")
        {
            AssertStudioAuthenticationComposition(outputPath, !templateOptions.Contains("elsa-login", StringComparer.OrdinalIgnoreCase));
            AssertHostPageRoutes(outputPath);
        }
        if (templateName is "elsa-server" or "elsa-combined")
        {
            AssertDashboardComposition(outputPath, templateOptions.Contains("shell", StringComparer.OrdinalIgnoreCase));
            AssertDeploymentOwnedIdentityConfiguration(outputPath);
        }
        await DotNet.RunAsync("build", Path.Combine(outputPath, $"{solutionName}.slnx"));
    }

    private static void AssertStudioAuthenticationComposition(string outputPath, bool expectsSharedAuthenticationUi)
    {
        var programText = string.Join(
            Environment.NewLine,
            Directory.GetFiles(outputPath, "Program.cs", SearchOption.AllDirectories).Select(File.ReadAllText));

        Assert.Contains("AddStudioAuthenticationMode", programText, StringComparison.Ordinal);
        Assert.Contains("AddDashboardModule(backendApiConfig)", programText, StringComparison.Ordinal);
        if (expectsSharedAuthenticationUi)
            Assert.Contains("AddAuthenticationUI", programText, StringComparison.Ordinal);
    }

    private static void AssertGeneratedAppsettingsAreValid(string outputPath)
    {
        foreach (var appsettingsPath in Directory.GetFiles(outputPath, "appsettings*.json", SearchOption.AllDirectories))
        {
            try
            {
                using var document = JsonDocument.Parse(File.ReadAllText(appsettingsPath));
            }
            catch (JsonException exception)
            {
                throw new Xunit.Sdk.XunitException($"Generated configuration is not valid JSON: {appsettingsPath}{Environment.NewLine}{exception.Message}");
            }
        }
    }

    private static void AssertHostPageRoutes(string outputPath)
    {
        var hostPages = Directory.GetFiles(outputPath, "_*Host.cshtml", SearchOption.AllDirectories);
        if (hostPages.Length == 0)
            return;

        foreach (var hostPage in hostPages)
        {
            var pageName = Path.GetFileNameWithoutExtension(hostPage);
            var pageText = File.ReadAllText(hostPage);

            Assert.Contains($"@page \"/{pageName}\"", pageText, StringComparison.Ordinal);
            Assert.DoesNotContain("@page \"/\"", pageText, StringComparison.Ordinal);
        }
    }

    private static void AssertDashboardComposition(string outputPath, bool expectsShellFeatures)
    {
        var programText = string.Join(
            Environment.NewLine,
            Directory.GetFiles(outputPath, "Program.cs", SearchOption.AllDirectories).Select(File.ReadAllText));

        if (expectsShellFeatures)
        {
            Assert.Contains("DashboardApiFeature", programText, StringComparison.Ordinal);
            Assert.Contains("WorkflowRuntimeDashboardFeature", programText, StringComparison.Ordinal);
        }
        else
        {
            Assert.Contains("UseDashboardApi", programText, StringComparison.Ordinal);
            Assert.Contains("UseWorkflowRuntimeDashboard", programText, StringComparison.Ordinal);
        }
    }

    private static void AssertDeploymentOwnedIdentityConfiguration(string outputPath)
    {
        var appsettingsPath = Directory
            .GetFiles(outputPath, "appsettings.json", SearchOption.AllDirectories)
            .Single(file => !file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal) &&
                            !file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal) &&
                            File.ReadAllText(file).Contains("\"CShells\"", StringComparison.Ordinal));
        var appsettings = File.ReadAllText(appsettingsPath);
        var developmentSettings = File.ReadAllText(Path.Combine(Path.GetDirectoryName(appsettingsPath)!, "appsettings.Development.json"));

        Assert.DoesNotContain("HashedPassword", appsettings, StringComparison.Ordinal);
        Assert.DoesNotContain("HashedApiKey", appsettings, StringComparison.Ordinal);
        Assert.Contains("development-only-secret-signing-key-change-before-production", developmentSettings, StringComparison.Ordinal);
        Assert.Contains("DefaultAdminUser", developmentSettings, StringComparison.Ordinal);
    }

    private static void AssertPackageVersions(string outputPath, string expectedElsaVersion, string expectedCShellsVersion)
    {
        var projectFiles = Directory.GetFiles(outputPath, "*.csproj", SearchOption.AllDirectories);
        Assert.NotEmpty(projectFiles);

        foreach (var projectFile in projectFiles)
        {
            var document = XDocument.Load(projectFile);
            var packageReferences = document
                .Descendants("PackageReference")
                .Select(reference => new
                {
                    Include = (string?)reference.Attribute("Include"),
                    Version = (string?)reference.Attribute("Version")
                });

            foreach (var packageReference in packageReferences.Where(reference => reference.Include?.StartsWith("Elsa", StringComparison.Ordinal) == true))
                Assert.Equal(expectedElsaVersion, packageReference.Version);

            foreach (var packageReference in packageReferences.Where(reference => reference.Include?.StartsWith("CShells", StringComparison.Ordinal) == true))
                Assert.Equal(expectedCShellsVersion, packageReference.Version);
        }
    }

    private static void AssertHealthChecksUseDedicatedEndpoint(string outputPath)
    {
        var programFiles = Directory.GetFiles(outputPath, "Program.cs", SearchOption.AllDirectories);
        var rootHealthCheckMappings = programFiles
            .Where(file => File.ReadAllText(file).Contains("MapHealthChecks(\"/\")", StringComparison.Ordinal))
            .Select(file => Path.GetRelativePath(outputPath, file))
            .ToArray();

        Assert.Empty(rootHealthCheckMappings);
    }
}

public sealed class TemplatePackageFixture : IAsyncLifetime
{
    public const string ElsaVersion = "3.9.0";
    public const string CShellsVersion = "0.0.28";

    public string PackagePath { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        var repositoryRoot = RepositoryPaths.Root;
        var projectPath = Path.Combine(repositoryRoot, "src", "Elsa.Templates", "Elsa.Templates.csproj");

        var expectedCommit = RepositoryPaths.Commit;
        await DotNet.RunAsync(
            "pack",
            projectPath,
            "-c",
            "Release",
            "/p:Version=" + ElsaVersion,
            "/p:PackageVersion=" + ElsaVersion,
            "/p:RepositoryCommit=" + expectedCommit,
            "/p:ContinuousIntegrationBuild=true");

        var packageDirectory = Path.Combine(repositoryRoot, "artifacts", "package", "release");
        PackagePath = Directory
            .GetFiles(packageDirectory, "Elsa.Templates.*.nupkg")
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .First();

        Assert.Equal($"Elsa.Templates.{ElsaVersion}.nupkg", Path.GetFileName(PackagePath));
        AssertRepositoryCommit(PackagePath, expectedCommit);
    }

    private static void AssertRepositoryCommit(string packagePath, string expectedCommit)
    {
        using var archive = ZipFile.OpenRead(packagePath);
        var nuspecEntry = archive.GetEntry("Elsa.Templates.nuspec");
        Assert.NotNull(nuspecEntry);

        using var stream = nuspecEntry!.Open();
        var document = XDocument.Load(stream);
        var repository = document
            .Descendants()
            .Where(element => element.Name.LocalName == "repository")
            .SingleOrDefault();

        Assert.NotNull(repository);
        Assert.Equal("https://github.com/elsa-workflows/elsa-templates", (string?)repository!.Attribute("url"));
        Assert.Equal(expectedCommit, (string?)repository.Attribute("commit"));
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }
}

public static class RepositoryPaths
{
    public static string Root { get; } = FindRoot();
    public static string Commit { get; } = ResolveCommit();

    private static string FindRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Elsa.Templates.slnx")))
                return directory.FullName;

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not locate repository root.");
    }

    private static string ResolveCommit()
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "git",
            WorkingDirectory = Root,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        startInfo.ArgumentList.Add("rev-parse");
        startInfo.ArgumentList.Add("HEAD");

        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start git process.");
        var output = process.StandardOutput.ReadToEnd().Trim();
        process.WaitForExit();

        if (process.ExitCode != 0 || string.IsNullOrWhiteSpace(output))
            throw new InvalidOperationException("Could not determine the repository commit.");

        return output;
    }
}

public static class DotNet
{
    private static readonly TimeSpan Timeout = TimeSpan.FromMinutes(5);

    public static async Task RunAsync(params string[] arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = ResolveDotNetPath(),
            WorkingDirectory = RepositoryPaths.Root,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        foreach (var argument in arguments)
            startInfo.ArgumentList.Add(argument);

        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start dotnet process.");
        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();
        var waitTask = process.WaitForExitAsync();
        var completedTask = await Task.WhenAny(waitTask, Task.Delay(Timeout));

        if (completedTask != waitTask)
        {
            process.Kill(entireProcessTree: true);
            throw new TimeoutException($"dotnet {string.Join(' ', arguments)} timed out after {Timeout}.");
        }

        var output = await outputTask;
        var error = await errorTask;

        if (process.ExitCode != 0)
            throw new InvalidOperationException($"dotnet {string.Join(' ', arguments)} failed with exit code {process.ExitCode}.{Environment.NewLine}{output}{Environment.NewLine}{error}");
    }

    private static string ResolveDotNetPath()
    {
        var configuredPath = Environment.GetEnvironmentVariable("DOTNET_EXE");
        if (!string.IsNullOrWhiteSpace(configuredPath))
            return configuredPath;

        const string macOsPath = "/usr/local/share/dotnet/dotnet";
        return File.Exists(macOsPath) ? macOsPath : "dotnet";
    }
}

public sealed class TempWorkspace : IAsyncDisposable
{
    private TempWorkspace(string path)
    {
        Path = path;
    }

    public string Path { get; }

    public static TempWorkspace Create()
    {
        var parentDirectory = Directory.GetParent(RepositoryPaths.Root)?.FullName ?? RepositoryPaths.Root;
        var path = System.IO.Path.Combine(parentDirectory, ".elsa-template-tests", $"elsa-template-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return new(path);
    }

    public ValueTask DisposeAsync()
    {
        if (Directory.Exists(Path))
            Directory.Delete(Path, recursive: true);

        return ValueTask.CompletedTask;
    }
}
