using System;
using System.Collections.Generic;
using System.Linq;
using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.CI.GitHubActions;
using Nuke.Common.Git;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.Coverlet;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.SonarScanner;
using Nuke.Common.Utilities.Collections;
using Nuke.Components;
using Serilog;

[ShutdownDotNetAfterServerBuild]
partial class Build : NukeBuild, ITest, IPack
{
    public static int Main() => Execute<Build>(x => ((ICompile)x).Compile);

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    [GitRepository] readonly GitRepository GitRepository;

    AbsolutePath SourceDirectory => RootDirectory / "src";

    public AbsolutePath PackagesDirectory => RootDirectory / "packages";
    public AbsolutePath TestResultDirectory => RootDirectory / "testresults";

    string TagVersion => GitRepository.Tags.SingleOrDefault(x => "v".StartsWith(x))?[1..];
    bool IsTaggedBuild => !string.IsNullOrWhiteSpace(TagVersion);

    string VersionSuffix;

    [Parameter] string Version;

    [Parameter] bool AnalyseCode;

    protected override void OnBuildInitialized()
    {
        VersionSuffix = !IsTaggedBuild
            ? $"preview-{DateTime.UtcNow:yyyyMMdd-HHmm}"
            : "";

        if (IsLocalBuild)
        {
            VersionSuffix = $"dev-{DateTime.UtcNow:yyyyMMdd-HHmm}";
        }

        Log.Information("BUILD SETUP");
        Log.Information("Configuration:\t{Configuration}", Configuration);
        Log.Information("Version suffix:\t{VersionSuffix}", VersionSuffix);
        Log.Information("Version:\t\t{Version}", Version);
        Log.Information("Tagged build:\t{IsTaggedBuild}", IsTaggedBuild);
        Log.Information("AnalyseCode:\t\t{AnalyseCode}", AnalyseCode);
    }

    Target Clean => _ => _
        .Before<IRestore>(x => x.Restore)
        .Executes(() =>
        {
            SourceDirectory.GlobDirectories("**/bin", "**/obj").ForEach(x => x.DeleteDirectory());
            ((IHazArtifacts)this).ArtifactsDirectory.CreateOrCleanDirectory();
            
            TestResultDirectory.CreateOrCleanDirectory();
        });
    

    public Configure<DotNetBuildSettings> CompileSettings => _ => _
        // ensure we don't generate too much output in CI run
        // 0  Turns off emission of all warning messages
        // 1  Displays severe warning messages
        .SetWarningLevel(IsServerBuild ? 0 : 1);

    public IEnumerable<Project> TestProjects =>
        ((IHazSolution)this).Solution.AllProjects.Where(x => x.Name.EndsWith("Tests"));

    public Configure<DotNetTestSettings, Project> TestProjectSettings => (testSettings, project) => testSettings
        .When(GitHubActions.Instance is not null, settings => settings.AddLoggers("GitHubActions;report-warnings=false"))
        .When(AnalyseCode, settings => settings.SetCoverletOutputFormat(CoverletOutputFormat.opencover))
        .When(AnalyseCode, settings => settings.EnableCollectCoverage())
        .When(AnalyseCode, settings =>  settings.SetCoverletOutput($"{TestResultDirectory}/opencoverCoverage.xml"))
        .When(AnalyseCode, settings =>  settings.SetProcessArgumentConfigurator(args => args.Add($"/p:MergeWith={TestResultDirectory}/")))
    ;
}