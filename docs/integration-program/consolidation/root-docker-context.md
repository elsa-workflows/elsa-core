# Root Docker build context during consolidation

Program #8194; Story #8286. Core's Docker smoke workflow builds four images
with `docker build -f docker/<image>.Dockerfile .` from repository root. The
Datadog compose file also uses `../.` as its context. The existing
`docker/.dockerignore` does not govern either root context. Studio's retained
`.dockerignore` at `9afd3e36fd1bc90dfdf8ea00b40d89e4a50c8822` assumes a
standalone repository and excludes `LICENSE` and `README.md`; copying it to
Core root would change unrelated image inputs without a need.

The active root `.dockerignore` now excludes developer-local configuration,
credential-shaped files, SQLite databases and generated build/test/package
outputs before Docker sends the context to a builder. It leaves source,
`NuGet.Config`, root props, `docker/entrypoint.sh`, legal notices and READMEs
available to the existing Dockerfiles. The Docker certificate smoke workflow
now runs for `.dockerignore` PR changes and can be dispatched for an exact
history-import head; it builds all four existing images and runs their TLS
smoke. It does not publish images or packages.

This is a root-context policy, not a claim that the old Studio host Dockerfiles
under `src/studio/hosts/` are usable. Those files reference old .NET 7 and
standalone project paths and are not used by Core's active Docker workflow.
The retained Studio `.dockerignore` ledger row stays pending until the
consolidated source import's image build and final container ownership are
reviewed. Docker context filtering is not a substitute for keeping credentials
out of tracked files or for a production image review.
