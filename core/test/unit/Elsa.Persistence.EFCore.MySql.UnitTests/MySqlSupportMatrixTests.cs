using System.Diagnostics;
using System.Reflection;
using System.Runtime.Versioning;
using System.Xml.Linq;
using Elsa.Testing.Shared;

namespace Elsa.Persistence.EFCore.MySql.UnitTests;

/// <summary>
/// Elsa 3.10 MySQL is net8/net9 only. Combined net10 restores still mix Pomelo 9 with EF 10 (NU1608);
/// that graph is not a supported runtime and must not be treated as a proof.
/// </summary>
public sealed class MySqlSupportMatrixTests
{
    private static readonly string[] MySqlProviderProjects =
    [
        Path.Combine("core", "src", "modules", "Elsa.Persistence.EFCore.MySql", "Elsa.Persistence.EFCore.MySql.csproj"),
        Path.Combine("core", "src", "modules", "Elsa.UserTasks.Persistence.EFCore.MySql", "Elsa.UserTasks.Persistence.EFCore.MySql.csproj"),
        Path.Combine("core", "src", "modules", "Elsa.Secrets.Persistence.EFCore.MySql", "Elsa.Secrets.Persistence.EFCore.MySql.csproj"),
        Path.Combine("core", "src", "modules", "Elsa.AI.Persistence.EFCore.MySql", "Elsa.AI.Persistence.EFCore.MySql.csproj"),
        Path.Combine("core", "src", "modules", "Elsa.ExternalAuthentication.Persistence.EFCore.MySql", "Elsa.ExternalAuthentication.Persistence.EFCore.MySql.csproj")
    ];

    private static readonly string[] CombinedNet10MySqlProjects =
    [
        Path.Combine("extensions", "src", "agents", "Elsa.Agents.Persistence.EFCore.MySql", "Elsa.Agents.Persistence.EFCore.MySql.csproj"),
        Path.Combine("extensions", "src", "scheduling", "Elsa.Scheduling.Quartz.EFCore.MySql", "Elsa.Scheduling.Quartz.EFCore.MySql.csproj"),
        Path.Combine("extensions", "samples", "workbench", "Elsa.Server.Web", "Elsa.Server.Web.csproj"),
        Path.Combine("extensions", "samples", "workbench", "Elsa.ServerAndStudio.Web", "Elsa.ServerAndStudio.Web.csproj")
    ];

    [Fact]
    public void EfCoreMySqlProviders_TargetNet8AndNet9Only()
    {
        var repoRoot = FindRepoRoot();
        foreach (var relative in MySqlProviderProjects)
        {
            var path = Path.Combine(repoRoot, relative);
            var tfms = ReadProperty(path, "TargetFrameworks");
            Assert.Equal("net8.0;net9.0", tfms);
            Assert.DoesNotContain("net10", tfms, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void LiveMySqlUnitTests_TargetNet9Only()
    {
        var framework = Assembly.GetExecutingAssembly().GetCustomAttribute<TargetFrameworkAttribute>()?.FrameworkName;
        Assert.Equal(".NETCoreApp,Version=v9.0", framework);
        var tfm = ReadProperty(Path.Combine(FindRepoRoot(), "core", "test", "unit", "Elsa.Persistence.EFCore.MySql.UnitTests", "Elsa.Persistence.EFCore.MySql.UnitTests.csproj"), "TargetFramework");
        Assert.Equal("net9.0", tfm);
        Assert.DoesNotContain("net10", tfm, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CombinedNet10MySqlHosts_DoNotSuppressNu1608()
    {
        var repoRoot = FindRepoRoot();
        foreach (var relative in CombinedNet10MySqlProjects)
        {
            var path = Path.Combine(repoRoot, relative);
            var noWarn = ReadProperty(path, "NoWarn") ?? string.Empty;
            Assert.DoesNotContain("NU1608", noWarn, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void CombinedNet10AgentsMySql_StillReferencesNet8Net9MySqlProvider()
    {
        var path = Path.Combine(FindRepoRoot(), CombinedNet10MySqlProjects[0]);
        var text = File.ReadAllText(path);
        Assert.Contains("Elsa.Persistence.EFCore.MySql.csproj", text, StringComparison.Ordinal);
        Assert.DoesNotContain("NU1608", ReadProperty(path, "NoWarn") ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CentralPins_KeepPomelo9AgainstNet10Ef10()
    {
        var packages = Path.Combine(FindRepoRoot(), "Directory.Packages.props");
        Assert.Equal("9.0.0", ReadPackageVersion(packages, "Pomelo.EntityFrameworkCore.MySql"));
        Assert.Equal("10.0.9", ReadNet10PackageVersion(packages, "Microsoft.EntityFrameworkCore.Relational"));
    }

    [Fact]
    public void Pomelo9AgainstEf10_RestoreEmitsNu1608()
    {
        var packages = Path.Combine(FindRepoRoot(), "Directory.Packages.props");
        var pomeloVersion = ReadPackageVersion(packages, "Pomelo.EntityFrameworkCore.MySql");
        var relationalVersion = ReadNet10PackageVersion(packages, "Microsoft.EntityFrameworkCore.Relational");
        var temp = Directory.CreateTempSubdirectory("elsa-mysql-nu1608");
        try
        {
            var project = Path.Combine(temp.FullName, "Nu1608Probe.csproj");
            File.WriteAllText(project, $"""
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <TargetFramework>net10.0</TargetFramework>
                    <ManagePackageVersionsCentrally>false</ManagePackageVersionsCentrally>
                    <TreatWarningsAsErrors>false</TreatWarningsAsErrors>
                  </PropertyGroup>
                  <ItemGroup>
                    <PackageReference Include="Pomelo.EntityFrameworkCore.MySql" Version="{pomeloVersion}" />
                    <PackageReference Include="Microsoft.EntityFrameworkCore.Relational" Version="{relationalVersion}" />
                  </ItemGroup>
                </Project>
                """);
            var output = Restore(project);
            Assert.Contains("NU1608", output, StringComparison.Ordinal);

            var pomelo = RestoredPackageVersionReader.ReadForTarget("Pomelo.EntityFrameworkCore.MySql", "net10.0", temp.FullName);
            var relational = RestoredPackageVersionReader.ReadForTarget("Microsoft.EntityFrameworkCore.Relational", "net10.0", temp.FullName);
            Assert.Contains(pomelo, version => version.Major == 9);
            Assert.Contains(relational, version => version.Major == 10);
        }
        finally
        {
            temp.Delete(recursive: true);
        }
    }

    private static string Restore(string projectPath)
    {
        var start = new ProcessStartInfo("dotnet")
        {
            Arguments = $"restore \"{projectPath}\" --verbosity minimal",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        using var process = Process.Start(start) ?? throw new InvalidOperationException("dotnet restore did not start.");
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        if (!process.WaitForExit(120_000))
        {
            process.Kill(entireProcessTree: true);
            throw new TimeoutException($"dotnet restore timed out for {projectPath}.");
        }

        var output = stdout.GetAwaiter().GetResult() + stderr.GetAwaiter().GetResult();
        Assert.True(process.ExitCode == 0, output);
        return output;
    }

    private static string? ReadProperty(string projectPath, string name)
    {
        var document = XDocument.Load(projectPath);
        return document.Descendants().FirstOrDefault(element => element.Name.LocalName == name)?.Value.Trim();
    }

    private static string ReadPackageVersion(string packagesPath, string packageId)
    {
        var document = XDocument.Load(packagesPath);
        var version = document.Descendants()
            .Where(element => element.Name.LocalName == "PackageVersion")
            .Where(element => string.Equals((string?)element.Attribute("Include"), packageId, StringComparison.OrdinalIgnoreCase))
            .Select(element => (string?)element.Attribute("Version"))
            .FirstOrDefault();
        return version ?? throw new InvalidOperationException($"{packageId} is missing from {packagesPath}.");
    }

    private static string ReadNet10PackageVersion(string packagesPath, string packageId)
    {
        var document = XDocument.Load(packagesPath);
        var version = document.Descendants()
            .Where(element => element.Name.LocalName == "ItemGroup")
            .Where(group => ((string?)group.Attribute("Condition") ?? string.Empty).Contains("net10.0", StringComparison.OrdinalIgnoreCase))
            .SelectMany(group => group.Elements())
            .Where(element => element.Name.LocalName == "PackageVersion")
            .Where(element => string.Equals((string?)element.Attribute("Include"), packageId, StringComparison.OrdinalIgnoreCase))
            .Select(element => (string?)element.Attribute("Version"))
            .FirstOrDefault();
        return version ?? throw new InvalidOperationException($"{packageId} is missing from the net10.0 group in {packagesPath}.");
    }

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Elsa.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not find Elsa.sln from the test output directory.");
    }
}
