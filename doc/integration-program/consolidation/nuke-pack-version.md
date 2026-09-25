# NUKE Pack version forwarding

Program #8194; Feature #8214; Story #8286. Core's `build/Build.cs` declared a
`Version` command-line parameter but did not pass it to `IPack`. The retained
Extensions build did pass it through `PackSettings`. This change gives the
consolidated build the same explicit-version behavior. When no version is
supplied, the existing MSBuild/project version remains in control.

The regression was reproduced with two ordinary disposable clones and the
same three-project local solution. Both ran `./build.sh Pack --version
0.0.0-proof.8214 --solution PackProof.sln` on macOS with .NET SDK 10.0.300 and
NUKE 10.1.0. `PackProof.sln` was generated only in the disposable clones by
adding `src/modules/Elsa.Common/Elsa.Common.csproj`; `dotnet sln` also added its
`Elsa.Features` and `Elsa.Mediator` project references. No feed or publish
target was invoked.

Running the changed build again without `--version` produced the original
`1.0.0` identities for the same three projects, confirming that the explicit
override is optional. The second run shared a local output directory with the
first, so NUKE's final artifact count included both runs; the filenames and
nuspec identities above refer to the newly produced proof packages only.

| Source | Actual local nupkg and snupkg versions | Pack result |
| --- | --- | --- |
| Core `bd853930ab09d642dabd8edf0a11a7f6c8c32fd4` before this change | `1.0.0` for `Elsa.Common`, `Elsa.Features`, `Elsa.Mediator` | succeeded, but ignored `--version` |
| This branch's `d37d352ca` implementation | `0.0.0-proof.8214` for those same three package IDs | succeeded; each nuspec ID/version matched its filename |

The proof version is deliberately local and cannot be used as a release
allocation. The source diff changes only `PackSettings`; it does not select
fewer packages or change a publisher workflow. Core's live `packages.yml`
also exports a `VERSION` environment variable before invoking NUKE. A prior
hosted release produced correctly versioned artifacts through that environment
path, so this bounded CLI regression does **not** show that historical hosted
packages were misversioned. Independent release units still need their own
scoped pack selection and single-publisher handoff; the existing all-solution
publisher must not be used for a connector-only release.

An ordinary clone was used because NUKE 10.1.0 could not inject
`GitRepository` from this host's linked Git worktree. The first disposable
clone used a local-path remote, which NUKE also rejected; setting that clone's
remote URL to the normal GitHub HTTPS identity resolved initialization.
Those two failed initialization attempts ran no Restore, Compile, or Pack
target. The successful before/after runs used the ordinary-clone layout and
the same proof command.
