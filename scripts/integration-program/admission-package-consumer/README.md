# Admission and Connections package consumer

This external, nonpackable console fixture consumes one unpublished, same-source package candidate. It has no Elsa project references, repository/test-worker source imports, live provider credentials or provider API calls. Copy only this folder into clean staging outside the checkout. The lead proof runner supplies an isolated mapped NuGet feed/cache, candidate version, three installed runtimes, PostgreSQL 16 and fresh databases. It owns artifact/cache/source/container provenance, process observation and database/container cleanup.

Build and run independently for every `net8.0`, `net9.0`, `net10.0` × `classic`, `shell` cell. Override `TargetFrameworks` to that single framework during restore/build. `CandidateVersion` is mandatory; there is no release-version fallback. Example command shape (the connection strings must already be set by the private hosted runner):

```sh
dotnet restore AdmissionPackageConsumer.csproj -p:CandidateVersion="$ELSA_PACKAGE_CANDIDATE_VERSION" -p:TargetFrameworks=net8.0 --configfile NuGet.Config
dotnet build AdmissionPackageConsumer.csproj --no-restore -c Release -f net8.0 -p:CandidateVersion="$ELSA_PACKAGE_CANDIDATE_VERSION" -p:TargetFrameworks=net8.0
dotnet bin/Release/net8.0/AdmissionPackageConsumer.dll --feature classic --tfm net8.0 --output /absolute/new-cell-report.json
```

Required environment:

- `ELSA_PACKAGE_SOURCE_REVISION`: exact lower-case 40-character source revision selected by the proof runner.
- `ELSA_PACKAGE_CANDIDATE_VERSION`: the same immutable candidate version passed to restore/build.
- `ELSA_PACKAGE_ADMISSION_CONNECTION_STRING`: a fresh precreated database for this cell's guarded executor.
- `ELSA_PACKAGE_GRANT_CONNECTION_STRING`: a different fresh database for this cell's grant scenario.

The fixture queries each actual database name, compares it with the selected Npgsql connection's database, and exports only its SHA-256. A four-minute cell deadline and twenty-second host shutdown deadline bound execution. No sleeps, fake notification injection or synthetic workflow execution are used.

Each invocation runs two scenarios sequentially:

1. `admission` builds and starts the real fixed Admission host, migrates the selected contexts, populates registries, provisions and reloads inactive verified bootstrap, explicitly activates, admits and executes one synthetic event, and verifies durable suspension/checkpoint/bookmarks. It denies direct runner/workflow pipeline/cached delegate, activity pipeline/cached delegate/both public invoker overloads, initial client entry, cancellation, import, deletion, generic definition management and public connection background/lifecycle operations without extra notifications or persisted state changes. An actual active connection and real binding/grant stores discriminate missing-grant resolver denial from the later denied background facade. A real bookmark continuation completes, persists state and releases execution-cycle ownership.
2. `managed-secret-grant` uses an ordinary **non-Admission** local runtime and separate database. Real static API-key lifecycle creation persists its encrypted managed secret/generation; real binding management creates the logical reference. Two real workflows suspend before resolution. Only one actual instance receives a durable grant through the real grant manager and narrow synthetic policies. The first host is fully stopped/disposed before a new host restores/resumes the persisted instances. The real stored-grant policy denies the ungranted instance and permits exactly one local credential resolution. Per-instance durable counters prevent aggregate success/denial counts from substituting for correct identity. The provider throws and counts if invoked; zero provider calls is mandatory.

The positive host verifies absence of Admission services/context registrations rather than absence of the already-loaded Admission DLLs. It retains the real resolver, background lifecycle service, EF stores and StoredConnectionCredentialBindingUseAuthorizer. Fixture management/share/use policies permit only declared synthetic identities and operations; they do not define production owner policy. The synthetic credential is compared locally and never assigned to workflow output, state or evidence.

Classic cells use the actual module features. Shell cells invoke the actual workflow, runtime, normal PostgreSQL persistence, Admission and Admission PostgreSQL Shell feature implementations in dependency order. Existing Connections/grant and Secrets infrastructure retains classic registration in this supported hybrid. There are no invented Connections Shell adapters, HTTP mappings, Alterations, autonomous triggers, remote executors or two concurrent execution hosts.

All selected contexts migrate first, then reapply while populated. The fixture compares actual mapped table rows by private hashes and verifies complete selected migration IDs against actual history. Connections requires its three migrations; Admission requires its one migration and separate `Elsa.__AdmissionMigrationsHistory`. Secrets, Connections, Management and Runtime share the ordinary history; `appliedIds` is its intersection with each context's complete migration set, while `historyIds` records actual shared history. Reapplication must preserve both populated rows and raw history. No rollback, cross-store atomicity or cross-provider conversion is claimed.

The schema-1 cell report is written atomically only after both scenarios and all three executor lifetimes dispose successfully. It contains actual framework/process/database/loaded-assembly identities, two fixed scenario records, bounded assertion/counter sets and migration observations. Loaded assemblies use the existing consumer six-field provenance shape. All required seven assemblies must be loaded exactly once. Report absence, nonzero exit, duplicate/missing/unknown scenarios, failed assertions, unexpected counters, leaked credential markers or incomplete cleanup fail proof. The lead's strict validator supplies the normative schema and independently binds process, source, candidate/cache bytes and PostgreSQL identities; this fixture does not claim that its own source-version environment variable establishes artifact provenance.

Only fixed stage/category/assertion-code failures are printed. Exception messages, connection strings, credential values and raw database rows are never emitted. Logs are kept in memory for credential-marker checks and never exported. Do not upload private build/runtime logs or staged source containing synthetic markers as runtime receipts. Live Socket transport, real credentials, publisher approval and publication remain outside this fixture.
