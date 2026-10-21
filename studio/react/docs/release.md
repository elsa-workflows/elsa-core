# React Studio Preview release preparation

Release owns image and npm publication for [#8857](https://github.com/elsa-workflows/elsa-core/issues/8857)
and [#8858](https://github.com/elsa-workflows/elsa-core/issues/8858). React Studio starts
at **3.11.0** and remains labelled **Preview**. The older 3.10 references in child
issues are superseded by the parent epic's release-train decision. This guide
prepares artifacts; it does not allocate a version or enable a publisher.

## Image preparation

The manual **React Studio Preview release** workflow accepts a `3.11.0-preview.N`
version first, then the plain `3.11.0` release after Preview acceptance. Its default
`publish: false` builds an OCI image archive with SBOM and build provenance and
retains it as a seven-day workflow artifact. It does not log in to a registry.
The image is Linux AMD64; other architectures have not been validated.

To build and exercise the same version locally:

```sh
export ELSA_REACT_STUDIO_VERSION=3.11.0-preview.1
docker compose -f studio/react/compose.yaml up --build -d
```

The version argument controls embedded .NET assembly/package identities and the
OCI version label. Frontend dependencies still use the frozen workspace lockfile.
The runtime is non-root, contains no Node runtime and supports the documented
`STUDIO_*` overrides. The [deployment guide](deployment.md) covers configuration,
trusted private build CAs and the paired development example.

## npm artifacts and template installation

The same manual workflow builds the SDK, UI, API-client and auth packages, stages
public tarballs and records their integrity plus the source commit in `release.json`.
Workspace manifests stay private and unchanged. Release tarballs carry the source
notice, Preview description, canonical repository metadata and exact internal
versions; they contain no `workspace:` references. No donor license grant is invented.

From `studio/react`, after building the workspace packages:

```sh
node scripts/prepare-npm-release.mjs 3.11.0-preview.1 /tmp/react-studio-npm
pnpm test:packages --npm-feed /tmp/react-studio-npm
```

Use a clean committed checkout and a fresh output directory. Uncommitted source is
rejected so the bundle cannot misidentify its source revision. The consumer check installs those exact tarballs and
compiles/renders an external module, then imports the UI style/token exports and
builds their theme and component utilities with Vite/Tailwind. Staging preserves
the package file lists, including CSS dependencies such as shadcn.css. To verify the generated RCL and host, first
pack `Elsa.ReactStudio.slnf` at the matching version, then run:

```sh
pnpm test:module --nuget-feed /path/to/local-nuget-feed \
  --studio-version 3.11.0-preview.1 --npm-feed /tmp/react-studio-npm
```

The workflow does both installation checks before building/publishing the image.
A separate npm publication job downloads the verified artifact, rechecks its source
and integrity, then uses GitHub OIDC trusted publishing with `--provenance`.
The public registry is explicit. Node 24 with npm >= 11.5.1 is required for publishing;
normal development remains compatible with the documented Node 22 minimum.
On a local Node 24 machine using an HTTPS proxy, `NODE_USE_ENV_PROXY=1` enables
Node's supported proxy handling for the registry preflight.

Previews use the `preview` dist-tag. Plain releases use `latest` and require a
matching-base preview of **each** package to exist first. Registry version checks
complete for all four packages before the first publish; a version with different
bytes fails. The publisher also refuses to move either dist-tag backwards. A partially completed publication can resume only when existing
versions have the exact recorded integrity. Image and npm registry writes are not
atomic: a later failure can leave the image or some packages published. Retain the
verified artifacts and rerun the same version to resume; never replace their bytes.

`node scripts/publish-npm-release.mjs /tmp/react-studio-npm --dry-run` performs the
registry preflight and npm dry run without publishing. A plain-release dry run
also enforces prior previews; artifact preparation alone does not allocate or
publish that version.

## Publication prerequisites

Publication remains disabled until the release program grants explicit cutover
approval. Before setting the repository variable `REACT_STUDIO_PUBLISH_ENABLED`
to `true`, Release must create the `react-studio-release` GitHub environment:

- Require Release-team reviewers and prevent self-review.
- Restrict deployments to approved release tags.
- Configure an npm trusted publisher for each of the four package names, bound
  to `elsa-workflows/elsa-core`, `studio-react-release.yml` and this environment.
  Release must provision package ownership/initial registration where required
  before trusted publishing can work; this workflow does not create ownership or
  supply an npm token.
- Store the existing Docker Hub `DOCKER_USER` and `DOCKER_PASS` bindings there.
- Confirm independent Elsa 3 approval, exact-head CI and required bot reviews on
  the merged implementation; do not use a local proof number as a release allocation.

After approval, dispatch the workflow on the exact matching Git tag with
`publish: true`. Selection rejects another repository, missing cutover flag,
nonmatching tag, versions below 3.11.0 and tags not contained in `main`. The
protected environment supplies the final Release approval before registry login.
The intended image is `elsaworkflows/elsa-react-studio:<version>`. Only the exact
version tag is pushed; the workflow does not move `latest` or a stable minor tag.
Keep Preview metadata for plain 3.11.0 as well.

A successful local image, archive or dry run is preparation evidence. Publication,
registry installation and final release acceptance must be recorded separately.
