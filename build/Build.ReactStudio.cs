using Nuke.Common;
using Nuke.Common.Tooling;
using Nuke.Components;

public partial class Build
{
    // The shared solution now embeds the Preview assets in its feature libraries.
    // Complete asset generation before MSBuild enumerates embedded resources.
    Target ReactStudioAssets => _ => _
        .DependentFor<ICompile>(build => build.Compile)
        .DependentFor<ITest>(build => build.Test)
        .DependentFor<IPack>(build => build.Pack)
        .Executes(() =>
        {
            var directory = RootDirectory / "studio" / "react";
            var npm = ToolPathResolver.GetPathExecutable("npm");
            ProcessTasks.StartProcess(npm, "exec --yes --package=pnpm@10.33.0 -- pnpm install --frozen-lockfile", directory)
                .AssertZeroExitCode();
            ProcessTasks.StartProcess(npm, "exec --yes --package=pnpm@10.33.0 -- pnpm build", directory)
                .AssertZeroExitCode();
        });
}
