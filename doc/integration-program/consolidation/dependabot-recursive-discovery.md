# Dependabot recursive discovery correction

Task [#8636](https://github.com/elsa-workflows/elsa-core/issues/8636) corrects the
product selectors to `/src/extensions/**/Elsa.*` and `/src/studio/**/Elsa.*`. A terminal
`/**` matches one directory level. NuGet entry-point discovery searches only the
selected directory for projects or solutions, so selecting category directories
does not discover the project manifests inside them.

The [official recursive example](https://docs.github.com/en/code-security/how-tos/secure-your-supply-chain/manage-your-dependency-security/controlling-dependencies-updated)
uses `**/*`. The failed hosted runs used updater
`9c06d60057ba9e7e79210e6618f932a28cf6a158`. At that revision:

- [NuGet PathHelper.GetMatchingDirectoriesUnder](https://github.com/dependabot/dependabot-core/blob/9c06d60057ba9e7e79210e6618f932a28cf6a158/nuget/helpers/lib/NuGetUpdater/NuGetUpdater.Core/Utilities/PathHelper.cs#L266)
  handles recursive `**/` explicitly; Job.GetAllDirectories removes
  duplicate directory paths and sorts matches without case sensitivity.
- [DiscoveryWorker.FindEntryPoints](https://github.com/dependabot/dependabot-core/blob/9c06d60057ba9e7e79210e6618f932a28cf6a158/nuget/helpers/lib/NuGetUpdater/NuGetUpdater.Core/Discover/DiscoveryWorker.cs#L262)
  enumerates files directly in each directory.
- The [Ruby fetcher](https://github.com/dependabot/dependabot-core/blob/9c06d60057ba9e7e79210e6618f932a28cf6a158/updater/lib/dependabot/file_fetcher_command.rb#L210)
  uses `Dir.glob(pattern, File::FNM_DOTMATCH)` and filters for directories.

## Tracked-source coverage

At source baseline `a307bd9bdff3b81f55715e23b748fc77457c1f65`, the recursive
selectors cover all tracked project manifests under each product:

| Product | Project manifests | In Elsa.sln | Matching directories | Directories without projects |
| --- | ---: | ---: | ---: | ---: |
| Extensions | 77 | 76 | 77 | 0 |
| Studio | 72 | 72 | 72 | 0 |

All current project directories are named `Elsa.*`. The selectors recursively
match those directories at any depth, including the retained non-solution
project. A fixed `/*/*` would miss future deeper projects. The completeness test
compares against every tracked project, so a new project with a different
directory name fails the gate and requires an explicit selector reconciliation.
Directory counts exclude local build output and count normalized paths once.
The broader `/**/*` alternative selects 627 Extensions and 522 Studio directories,
of which 550 and 450 have no project manifests. Matching the actual project
directories avoids that extra empty discovery work without omitting a project.
The standard-library Ruby regression can be run with:

```sh
ruby scripts/integration-program/test_dependabot_discovery.rb
```

It models the native NuGet pattern algorithm and corroborates it with Ruby globbing,
reproduces the terminal-glob miss, checks a deeper synthetic project, reconciles
all tracked manifests with the solution, and verifies the existing update policies.
These checks establish selector coverage, not restore or feed resolution.

## Remaining hosted evaluation gap

`src/extensions/Elsa.Testing.Extensions/Elsa.Testing.Extensions.csproj` is a
pre-existing Core project outside `Elsa.sln`, as recorded in the
[repository inventory](../inventory/README.md). It remains included. No retirement
decision was found, and solution membership alone does not justify excluding it.

[Extensions run 37467995784](https://github.com/elsa-workflows/elsa-core/actions/runs/37467995784)
found this project but failed restore: its explicit
`Microsoft.AspNetCore.Mvc.Testing` PackageReference versions cause NU1008 under
the inherited central package management; its net7.0 target also causes NU1010
for Core references whose central versions are defined for net8.0/net9.0/net10.0.
[Studio run 37467996957](https://github.com/elsa-workflows/elsa-core/actions/runs/37467996957)
reported no product projects. Both ran against the baseline above and returned
green Actions status without satisfying the task.

The old Extensions run returned `Projects=[]`, `IsSuccess=true`, and `Error=null`
for the retained testing project after the restore warnings, then continued to
the other directories. Those warnings did not abort discovery. The project must
remain separately accounted for until restore/evaluation succeeds or a reviewed
disposition changes its status. Discovery checks ancestor dotnet-tools manifests
for every directory, and the worker clears its processed-project set between
directories. No runtime duration or successful completion is inferred from
local matching.

New hosted runs must demonstrate discovered-project evaluation, product central
package versions, update/current-version results, and configured-feed resolution
where exercised. This correction changes no project files, dependencies,
registries, schedules, PR limits, grouping, root exclusions, or publication flows.
