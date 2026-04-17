## Summary of `CShells` Changes

There were two separate runtime problems I was addressing in `CShells`:

1. **Duplicate endpoint registration during startup**
   - This surfaced as:
	 `System.InvalidOperationException: Not allowed to configure endpoints after startup! Culprit: [Verbs()]`
   - The root cause was that shell endpoints were getting mapped once during per-shell activation and then mapped again during the startup-wide `ShellsReloaded` flow.

2. **Ambiguous activation of `WebRoutingShellResolver`**
   - This surfaced as:
	 `Unable to activate type 'CShells.AspNetCore.Resolution.WebRoutingShellResolver'. The following constructors are ambiguous: ...`
   - The root cause was that dependency injection could satisfy more than one constructor, so type-based activation was ambiguous.

---

## `src/CShells.AspNetCore/Configuration/CShellsBuilderExtensions.cs`

### What changed

I made two important changes here.

#### 1. Switched web-routing resolver registration from type-based to factory-based creation

Instead of registering `WebRoutingShellResolver` by type alone, I changed the resolver pipeline registration to explicitly construct it with:

- `IShellHost`
- `WebRoutingShellResolverOptions`

#### 2. Changed endpoint-registration notification wiring to one shared concrete handler plus per-notification forwarders

Previously, `ShellEndpointRegistrationHandler` was registered separately as:

- `INotificationHandler<ShellActivated>`
- `INotificationHandler<ShellDeactivating>`
- `INotificationHandler<ShellRemoved>`
- `INotificationHandler<ShellsReloaded>`

I changed that to:

- register one singleton `ShellEndpointRegistrationHandler`
- register small forwarders for each notification type:
  - `ShellActivatedEndpointRegistrationForwarder`
  - `ShellDeactivatingEndpointRegistrationForwarder`
  - `ShellRemovedEndpointRegistrationForwarder`
  - `ShellsReloadedEndpointRegistrationForwarder`

### Why I did that

The factory-based resolver registration was to fix the `WebRoutingShellResolver` constructor ambiguity. Since DI could resolve multiple constructors, it was not safe to rely on type activation. Explicit construction forces one intended activation path.

The forwarder-based notification registration was needed because the endpoint deduplication logic depends on shared in-memory state. Registering the same implementation separately for multiple closed generic interfaces can lead to different singleton instances being used per interface type. That breaks state sharing.

By routing all notifications through one shared concrete `ShellEndpointRegistrationHandler`, the deduplication state is preserved across `ShellActivated`, `ShellDeactivating`, `ShellRemoved`, and `ShellsReloaded`.

---

## `src/CShells.AspNetCore/Notifications/ShellEndpointRegistrationHandler.cs`

### What changed

This file contains the main startup-endpoint fix.

I added:

- an optional `IShellRuntimeStateAccessor`
- a lock-protected dictionary of tracked applied generations
- helper methods to:
  - track generations
  - forget generations
  - reset tracked generations
- an `IsEndpointGraphCurrent(...)` guard

I also changed notification handling so that:

- `ShellActivated` registers endpoints and tracks the applied generation
- `ShellDeactivating` removes endpoints and forgets the generation
- `ShellRemoved` removes endpoints and forgets the generation
- `ShellsReloaded` now:
  - filters to routable shells
  - checks whether the current endpoint graph is already current
  - skips rebuild if nothing changed
  - otherwise clears and rebuilds the full endpoint graph and retracks generations

Finally, I added the forwarder classes at the bottom of the file.

### Why I did that

This was the core fix for the duplicate startup endpoint-registration path.

The failure mode was effectively:

1. A shell becomes active
2. Its endpoints are mapped
3. A startup-wide `ShellsReloaded` is published
4. All shell endpoints are rebuilt again

That second pass was a problem for `FastEndpoints`, which rejects endpoint configuration after startup has progressed beyond its expected window.

To fix that, I made `ShellEndpointRegistrationHandler` state-aware. It now checks whether the currently mapped shell IDs and applied generations already match runtime state. If so, it skips the aggregate rebuild instead of remapping the same endpoints again.

That preserves the ability to rebuild endpoints when shells truly change, while avoiding no-op startup remaps.

---

## `src/CShells/Hosting/ShellStartupHostedService.cs`

### What changed

I changed startup publication of `ShellsReloaded` to use a custom notification strategy:

- `startupShellsReloadedStrategy`

I also added a private `StartupShellsReloadedNotificationStrategy` implementation that filters out ASP.NET Core notification handlers during the startup `ShellsReloaded` publication.

### Why I did that

This was a startup-specific safety measure.

Even after adding deduplication logic, I wanted to prevent the startup-wide `ShellsReloaded` from immediately driving the ASP.NET Core endpoint-remapping path in the same startup cycle.

The intent was:

- keep publishing `ShellsReloaded` as a meaningful system event
- avoid redundant ASP.NET Core endpoint registration work during startup
- preserve other non-ASP.NET-Core `ShellsReloaded` observers

So this was not a general suppression of `ShellsReloaded`; it was a targeted startup filter to avoid duplicate endpoint mapping during application bootstrap.

---

## `src/CShells/Resolution/ResolverPipelineBuilder.cs`

### What changed

I added support for registering resolver strategies with a factory:

- `Use<TStrategy>(Func<IServiceProvider, TStrategy> factory, int? order = null)`

I also updated internal strategy registration so each strategy registration can now hold:

- a type
- an instance
- or a factory

During pipeline build, the resolver strategy can now be registered using that explicit factory.

### Why I did that

This was necessary to support the explicit-construction fix for `WebRoutingShellResolver` in `CShellsBuilderExtensions`.

Once I decided not to rely on DI constructor selection for that resolver, the pipeline needed a way to say:

> register this resolver strategy, but instantiate it exactly this way

Without factory support in `ResolverPipelineBuilder`, there was no clean way to do that.

---

## Supporting Test Changes

These test changes matter because they encode the design intent behind the production changes.

### `tests/CShells.Tests/Integration/AspNetCore/ApplicationBuilderExtensionsTests.cs`

#### What changed

I added a regression test that verifies:

- after a shell is activated and endpoints are mapped,
- a subsequent `ShellsReloaded` with the same applied generation state
- does **not** remap the endpoints again

I also added:

- a counting web feature
- a simple endpoint mapping counter

#### Why I did that

Because the production bug was about duplicate endpoint registration, I wanted a test that directly proves the bad sequence no longer happens.

The test protects the scenario that previously triggered the `FastEndpoints` startup exception.

---

### `tests/CShells.Tests/Integration/AspNetCore/ServiceCollectionExtensionsTests.cs`

#### What changed

I added a regression test that verifies the DI registration shape now consists of:

- one concrete `ShellEndpointRegistrationHandler`
- forwarders registered for each notification interface

#### Why I did that

The shared-state design only works if all notification types ultimately delegate to the same concrete handler instance.

This test protects that registration shape so it is not accidentally simplified back into a broken form later.

---

## Overall Rationale

The `CShells` changes were meant to make startup behavior more correct and deterministic.

### For endpoint registration

I was trying to prevent this sequence:

- shell activates
- endpoints are mapped
- startup-wide reload fires
- endpoints are rebuilt again
- `FastEndpoints` rejects the second configuration pass

So I:

- added endpoint graph/generation tracking
- ensured the tracking state is actually shared across notification types
- skipped unnecessary aggregate rebuilds
- added a startup-specific filter as a safety measure

### For web routing

I was trying to prevent DI from making an ambiguous constructor choice when building `WebRoutingShellResolver`.

So I:

- stopped relying on type-based activation
- added factory-based resolver registration support
- explicitly constructed the resolver with the intended dependencies

---

## Net Effect

These changes were intended to move `CShells` away from startup behavior that depended on:

- duplicate lifecycle-triggered endpoint registration
- DI constructor-selection heuristics

and toward behavior that is:

- state-aware
- deduplicated
- explicitly constructed where necessary

That is why each of those `CShells` files changed the way they did.

