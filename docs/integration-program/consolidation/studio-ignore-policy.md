# Studio static-source ignore policy

Program #8194; Feature #8214; Story #8286. The history import retains the
Extensions and Studio `.gitignore` files under inert `.source` paths. This
decision handles the import's immediate source-asset risk without copying
either repository's entire ignore template into Core.

At import draft `1b52430a8523d06da1f671f369f77a76b814b5a2`, Core's active
`.gitignore` was blob `490309417d866479f30bf155045288dc65f89554`.
Its unanchored `wwwroot/` rule ignored new files under **all 11** Studio
`wwwroot` directories that already contain tracked, hand-authored files. A
tracked file remains tracked, but a new CSS, JavaScript, image or host static
file would be absent from ordinary `git status` and easy to omit from a PR.
The retained Studio `.gitignore` is blob
`06afca4eb6353c6eb153b29aec7e3c302187cb37` at Studio
`9afd3e36fd1bc90dfdf8ea00b40d89e4a50c8822`; its allowlist covers only
some older module directories and misses newer StructuredLogs and Localization
assets. A direct copy would therefore not solve the current import.

The active root now admits authored files under `src/studio/**/wwwroot/` while
keeping three verified generated directories ignored:

| Generated directory | Source of output |
| --- | --- |
| `src/studio/framework/Elsa.Studio.DomInterop/wwwroot/` | ClientLib webpack output |
| `src/studio/modules/Elsa.Studio.Workflows.Designer/wwwroot/` | ClientLib webpack output |
| `src/studio/modules/Elsa.Studio.Alterations/wwwroot/` | MSBuild copies authored `Assets/` files here |

The Studio-specific `appsettings.Local.json` pattern also prevents a local
host override from appearing as a source file. It is not a credential store;
developers must still keep credentials out of tracked configuration.

`git check-ignore --no-index` was run on proposed source files in each of the
11 tracked Studio static directories and a future module: all were visible.
The three generated directories, a Studio local host override, and an
unrelated Core-generated `wwwroot` remained ignored. The authored
`Elsa.Studio.Alterations/Assets` path remained visible. These checks exercise
Git's pattern behavior without creating or committing synthetic files.

The retained Extensions `.gitignore` is blob
`4150e8ad0639cd367acf8f2c4ece518355b2a8f2` at Extensions
`33fa0bfd28c7585240e3d4f665058c067b17e287`. Core already covers its
common build outputs; its broad release, publish, help and generated-doc
patterns are not copied because they could hide source or handoff artifacts.
The retained Studio `.dockerignore` remains separate: current Docker builds
use repository-root context, so a Studio-root ignore copy would change other
images. Its disposition needs a container-context review. No publisher,
package, or Docker workflow changes here.
