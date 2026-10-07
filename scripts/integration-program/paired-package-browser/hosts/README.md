# Disposable host glue

`source-glue.json` records immutable source paths and SHA-256 hashes. Where an
upstream file contained trailing whitespace, `source_sha256` preserves its
original digest and the declared fixture transform records the whitespace trim.
Other explicit consumer-host transforms include the CustomElements EventCallback
bridge, feature initialization and empty embedding mount; they do not modify packaged Elsa libraries.
`common`
contains the accepted candidate's host entrypoints; the 3.9.0 entrypoints have
the same startup composition. `3.8.4` overrides the release-specific project,
startup and page files. Source project XML is input metadata, never a buildable
Elsa library source dependency.

The materializer replaces package-library project references with exact aligned
NuGet references. It preserves each host's registration and delivery model,
adds the paired WorkflowContexts module and fixture runtime assembly metadata,
and selects the supported ElsaIdentity mode for local accounts. The only source
edge is the hosted wrapper's disposable, nonpackable WASM client. Host glue changes
are recorded separately from original package assets; a corrected consumer host
does not establish that an unchanged upstream host worked. Product defects must
remain failed evidence.

The Hosted wrapper registers the package's scoped default `IBrandingProvider`
for its server-rendered Razor page. The original host omitted that registration;
services registered inside its referenced WASM client do not populate the wrapper's
service container. The same correction is applied to Core's nonpackable Hosted
host, and the fixture transform preserves the original source hash. Package bytes
are unchanged. This is a corrected host composition, not acceptance of the original
host without that registration.

The native CustomElements wrappers synchronously apply their public backend and
authentication properties through `BackendComponentBase.OnInitialized` before
rendering `ThemedComponentWrapper`. Its asynchronous initialization then awaits
the package `IFeatureService.InitializeFeaturesAsync` API before rendering child
content. This supplies the feature-registration step normally owned by the full
shell's `MainLayout`, without adding shell navigation to the embedding page.
One initialization task is cached by the actual scoped feature-service identity,
so concurrent native roots and remounts share registration. This uses the common
3.8.4/3.9.0/3.10.0 API; the 3.8.4 interface has no `IsInitialized` property.
Initializer failures propagate and never mark the child ready. A fresh service
scope/browser runtime is required for another endpoint or permission/feature
profile, including retry after an initializer failure; feature registries and
catalog caching are not reset by this host glue. The standalone `BackendProvider`
still only applies backend configuration. Pure materialization contracts bind
these fixture bytes to their declared source transform; C# compilation and actual
feature/browser behavior remain separate required evidence.

The Python owner creates separate mutable runtime state for every record and
stops/reaps its own process groups on startup failure or browser exit. A private
in-memory runtime handle is passed to the browser using stdin. Credentials,
tokens, private process logs and data-protection keys are not portable receipts.
Shared builds belong to one verified version/framework/SDK/config/cache group;
every project's package provenance must be checked again before any listener.
Server assembly metadata is anonymous on the owned loopback listener; it does
not prove browser authentication. Backend metadata uses its normal bearer auth.

Candidate host requests may select `designer_mode="react-flow"`; the default is
`"x6"`. Released requests reject ReactFlow. Both candidate modes use the same
materialized source and verified build. Runtime configuration selects the public
`DesignerOptions.UseReactFlow` option: server options use the owned environment,
and native clients receive the matching public configuration value. Ambient
DesignerOptions overrides are removed. Selecting a mode is host setup, not proof
that its native editor callback or save journey passed.

`start_designer_phases(layout, validate_project=...)` owns one candidate backend
while sequential `owner.phase("x6")` and `owner.phase("react-flow")` contexts
restart Studio on the same origin. Each phase rechecks the unchanged verified
build and receives the same private backend credentials, keys, runtime database
and synthetic identifiers. The caller must create a fresh browser context for
each phase and separately bind their receipts. The owner checks process birth
identities and descendant cleanup before admitting the next phase; uncertain
ownership or cleanup prevents continuation. Its lifetime includes startup and is
bounded to 600 seconds. This API supports all four host layouts, including the
native CustomElements embedding. Candidate execution uses this owner
for the original X6 journey followed by a separate React edit/save/reload browser
phase. Actual browser verification of the composed journey remains pending.
