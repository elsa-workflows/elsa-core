# Picomatch for Elsa Studio builds

This private package derives from [Picomatch 4.0.7](https://github.com/micromatch/picomatch/releases/tag/4.0.7), released under the MIT license. All source and the license come from the registry tarball with the integrity recorded in the original lockfile.

The parser rejects repeated extglobs in every build-tool caller. Normal globstars, braces and non-repeating extglobs still work. This removes the affected syntax for [GHSA-fc5q-39xc-vjpf](https://github.com/advisories/GHSA-fc5q-39xc-vjpf) while the upstream package has no fixed release. Elsa Studio does not publish this build dependency. Remove the override and this package when the workspace can install a patched upstream release.
