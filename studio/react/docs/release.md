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

## Publication prerequisites

Publication remains disabled until the release program grants explicit cutover
approval. Before setting the repository variable `REACT_STUDIO_PUBLISH_ENABLED`
to `true`, Release must create the `react-studio-release` GitHub environment:

- Require Release-team reviewers and prevent self-review.
- Restrict deployments to approved release tags.
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
