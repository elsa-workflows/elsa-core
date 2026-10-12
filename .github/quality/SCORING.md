# Quality scoring

The quarterly re-baseline scores Elsa on eight dimensions from 1 to 5. The anchors below are fixed so that scores are comparable over time. Change them only by PR, and say in the next score post that the rubric changed.

Each dimension is scored by reading the code against [PRINCIPLES.md](PRINCIPLES.md) and the theme indicators from [metrics.sh](metrics.sh). A score must cite its evidence. An indicator alone never moves a score, but a score that contradicts its indicators needs a written reason.

| Dimension | 1 | 3 | 5 | Informed by |
|---|---|---|---|---|
| Product & first run | A new developer cannot get a server with Studio running from the docs. | One working path exists, but it is buried among alternatives. | One documented golden path: template or quick-start sample, Docker image and README all agree. | README setup indicator |
| Public API & DX | Several competing ways to register and configure; plumbing is public; nothing guards breaks. | One preferred way, documented; some plumbing public; breaks caught in review. | One way per concern; internal by default; package validation guards every release. | Public/internal ratio, dual registrations |
| Domain language | Core concepts undefined; synonyms common in public names. | Core concepts defined; some drift between glossary, code and UI. | Glossary covers every public concept, and code, docs and UI use it. | CONTEXT.md coverage |
| Simplicity | Parallel or abandoned subsystems kept alongside their replacements. | Few parallel subsystems, each with a written reason. | None without a written reason; deprecations finish on schedule. | Dual registrations |
| Architecture & structure | Copied build configuration; projects outside the solution without reason. | Build configuration inherited, with known exceptions. | One inherited build configuration; every project in exactly one product and in the solution. | Tracked .csproj not in Elsa.sln |
| Tests | Behavior untested or asserted many times over; flaky waits common. | Most public behavior tested once; some copied helpers and sleeps. | Each behavior tested once at the right level, with shared fixtures and no fixed sleeps. | Fixed sleeps |
| CI & build | Dead or duplicate workflows; temporary machinery without an exit. | Few duplicates; temporary machinery states its owner. | Every workflow earns its run; temporary machinery has an exit and leaves on time. | Program workflows without exit |
| Docs & hygiene | Facts in several places, often stale; generated or ignored files tracked. | One home per fact for most topics; little residue. | One home per fact; nothing tracked that .gitignore excludes; one copy of agent instructions. | Duplicated skills, tracked ignored files |

## History

The score history lives on the tracking issue [#8700](https://github.com/elsa-workflows/elsa-core/issues/8700), and each re-baseline republishes the report with all past scores. The baseline was taken on 2026-10-11 at `31450da77b`:

| Product | API | Language | Simplicity | Architecture | Tests | CI | Docs | Average |
|---|---|---|---|---|---|---|---|---|
| 2 | 2 | 2 | 2 | 2 | 3 | 2 | 2 | 2.1 |
