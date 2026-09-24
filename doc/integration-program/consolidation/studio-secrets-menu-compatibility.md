# Secrets navigation across the canonical and legacy hosts

The #8326 local Workbench exercise exposed a discovery mismatch: the authenticated
installed-features response contained `Elsa.Secrets`, while the pinned Studio menu
asked only for `Elsa.Secrets.ShellFeatures.Secrets`. Authorized users could manage
secrets through the direct URL, but the navigation item was absent.

`scripts/integration-program/consolidated-build/studio-secrets-menu.patch` accepts
either exact name for this menu. It leaves the existing public feature constant,
feature attribute, route, package identity and global discovery behavior unchanged.
An absent, similarly named or unavailable feature does not enable the menu. This
is capability discovery; server-side authorization remains mandatory.

The patch includes a focused `Elsa.Studio.Secrets.Tests` project because the pinned
module has no test project. Its tests cover canonical and legacy names, both names
together, unrelated names, discovery errors and cancellation-token forwarding.
The project follows the neighboring Studio module test conventions and references
the actual Secrets module.

## Existing work

[Studio PR #999](https://github.com/elsa-workflows/elsa-studio/pull/999), inspected at
`d5d7d1dabfea1a7bbbb698586b6528bb1ebdea5f`, adds `RequiredPermission = "secrets:view"`
to this menu as part of broader permission-aware navigation. This patch changes
the separate discovery condition and preserves that permission change when both
are applied. It does not duplicate the permission service or implement the
remaining unauthorized-page control behavior.

## Applying and verifying

Apply the patch to the recorded, prepared source rehearsal containing Studio
`9afd3e36fd1bc90dfdf8ea00b40d89e4a50c8822` at `src/studio`. First run `git apply
--check`; do not force it over changed upstream work. Then run the new test project
explicitly:

```sh
git apply /path/to/tools/scripts/integration-program/consolidated-build/studio-secrets-menu.patch
dotnet test src/studio/modules/Elsa.Studio.Secrets.Tests/Elsa.Studio.Secrets.Tests.csproj --framework net10.0
```

During the eventual import, include the new test project in canonical solution
discovery and repeat the actual-host menu smoke. Applying this source patch is
preparation work, not evidence that the source import has happened. It does not
establish tenant-membership policy, production security, other target frameworks
or release readiness.
