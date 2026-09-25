# Relocated Extensions developer-local ignore rules

Program #8194; Story #8286. Extensions' retained standalone `.gitignore` at
`33fa0bfd28c7585240e3d4f665058c067b17e287` remains at
`doc/integration-program/legacy/extensions/.gitignore.source`. The active
Core root already ignores bin/obj, test results, NuGet package folders,
node_modules, Fody's generated schema, local databases and common IDE files.
Copying the entire Visual Studio template would hide unrelated Core and Studio
files, including intentionally authored samples.

The root `.gitignore` now adds only Extensions-scoped developer state
(`*.rsuser`, `*.userprefs`, `*.DotSettings.user`), local publishing profiles or
credentials (`*.pubxml`, `*.azurePubxml`, `*.publishsettings`, `*.pfx`,
`PublishScripts/`), MSBuild binary logs, and Python bytecode. Git's existing
tracked files remain tracked. The old `.codebase-memory/` rule is not copied:
a future shared graph artifact belongs to the consolidated repository root and
needs its own ownership decision. Broad legacy Visual Studio 6, BizTalk,
ClickOnce, Azure emulator and report-file patterns are retired for the imported
tree until an actual generated path requires one.

`git check-ignore --no-index -v` confirms the scoped examples and that an
Extensions C# source file and a similarly named Studio publish-profile fixture
remain visible. This decision changes source-control hygiene only. It neither
activates a publisher nor proves that an imported Extensions project builds or
runs; those remain separate #8286 gates.
