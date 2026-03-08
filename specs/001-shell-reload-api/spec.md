# Feature Specification: Shell Reload API Endpoints

**Feature Branch**: `001-shell-reload-api`  
**Created**: 2026-03-08  
**Status**: Draft  
**Input**: User description: "Add API endpoints to reload all shells and reload a specific shell so administrators can apply updated feature enablement and configuration without restarting the application. Until selective reload is supported by the shell platform, the specific-shell action should temporarily perform a full reload."

## Clarifications

### Session 2026-03-08

- Q: How should a full reload behave when one shell configuration is invalid while others remain valid? → A: Partial apply: valid shells reload, invalid shells stay on their previous configuration, and the API reports partial success.
- Q: How should the API behave when a reload request arrives while another reload is already in progress? → A: Reject: return a conflict or busy outcome while a reload is already in progress, and require the caller to retry.
- Q: For a targeted reload request, should the API succeed only if the requested shell itself refreshed successfully? → A: Requested-shell strict: if the requested shell's new configuration is invalid, the targeted reload returns a non-success outcome even if other shells reload successfully.
- Q: Should reload responses include detailed per-shell outcome information or only an overall outcome? → A: Detailed results: reload responses include per-shell outcome details for affected shells, including which shells succeeded and which failed.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Reload all shells after configuration changes (Priority: P1)

An administrator updates shared or per-shell configuration that affects enabled features or feature settings and needs the running application to pick up those changes immediately without restarting the host.

**Why this priority**: Reloading all shells is the existing operational fallback and is required to make changed configuration effective across the system.

**Independent Test**: Can be fully tested by changing shell configuration for one or more shells, invoking the full reload action, and verifying that subsequent requests use the updated configuration without an application restart.

**Acceptance Scenarios**:

1. **Given** one or more shells have configuration changes waiting in the active configuration source, **When** an authorized administrator triggers the full reload action, **Then** all running shells are reloaded from the latest configuration and begin using the updated feature state and settings.
2. **Given** the full reload action completes successfully, **When** the administrator uses functionality affected by the configuration change, **Then** the behavior reflects the updated configuration without requiring a host restart.
3. **Given** a full reload completes with a mix of successful and unsuccessful shell refreshes, **When** the API returns its result, **Then** the response identifies which shells refreshed successfully and which shells did not.

---

### User Story 2 - Request reload for one shell (Priority: P2)

An administrator changes configuration for a single shell and wants to target that shell explicitly through the API so operational tooling can express intent at the shell level.

**Why this priority**: Targeted reload is the desired long-term operational model and should be available in the API contract now, even if the underlying platform initially fulfills it with a broader reload.

**Independent Test**: Can be fully tested by changing configuration for a known shell, invoking the targeted reload action with that shell identifier, and verifying that the requested shell reflects the updated configuration after the action completes.

**Acceptance Scenarios**:

1. **Given** a shell with a valid identifier has pending configuration changes, **When** an authorized administrator triggers the targeted reload action for that identifier, **Then** the system completes a reload operation that results in the requested shell reflecting the latest configuration.
2. **Given** the platform does not yet support selective shell reload, **When** the administrator triggers the targeted reload action, **Then** the request still succeeds by using the currently supported reload behavior while ensuring the requested shell reflects the updated configuration.
3. **Given** the administrator specifies a shell identifier that does not correspond to a known shell, **When** the targeted reload action is invoked, **Then** the system rejects the request with a clear not-found outcome and does not report success.
4. **Given** a targeted reload falls back to a full reload and the requested shell's updated configuration is invalid while other shells can still reload, **When** the targeted reload action is invoked, **Then** the request returns a non-success outcome for the caller and the requested shell keeps its previous active configuration.
5. **Given** a targeted reload action completes, **When** the API returns its result, **Then** the response includes outcome detail for the requested shell and any other affected shells.

---

### User Story 3 - Handle failed reload attempts safely (Priority: P3)

An administrator needs clear failure behavior when reload cannot complete because configuration is invalid, unavailable, or the system is already processing a conflicting reload request.

**Why this priority**: Operators need predictable failure handling so they do not assume configuration changes are active when they are not.

**Independent Test**: Can be fully tested by making the configuration source unavailable or invalid, invoking either reload action, and verifying that the API communicates failure without falsely indicating that shells were refreshed.

**Acceptance Scenarios**:

1. **Given** the configuration source is unavailable or cannot provide any usable shell configuration, **When** either reload action is invoked, **Then** the request fails with a clear error outcome and shells continue using their last known active configuration.
2. **Given** a reload operation is already in progress, **When** another reload action is invoked, **Then** the system rejects the new request with a conflict or busy outcome and requires the caller to retry later.
3. **Given** a full reload encounters invalid configuration for one shell while configuration for other shells remains valid, **When** the full reload action is invoked, **Then** valid shells adopt their updated configuration, invalid shells keep their previous active configuration, and the API reports a partial-success outcome.

### Edge Cases

- A targeted reload request is made for a shell identifier that existed previously but has since been removed from the current configuration source.
- Configuration for one shell is invalid while other shells remain valid during a full reload request, resulting in partial success rather than a full rollback.
- A reload is requested while affected shells are actively serving traffic.
- Multiple administrators or automation jobs issue reload requests within a short time window.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST provide an authenticated and authorized API action that triggers a reload of all shells.
- **FR-002**: The system MUST provide an authenticated and authorized API action that accepts a shell identifier and triggers a reload request for that specific shell.
- **FR-003**: The targeted reload action MUST validate that the supplied shell identifier refers to a known shell before reporting success.
- **FR-004**: When the full reload action succeeds, the system MUST rebuild running shell state from the latest available shell configuration source.
- **FR-005**: When the targeted reload action succeeds, the requested shell MUST reflect the latest available configuration by the time the action reports success.
- **FR-006**: Until selective shell reload is supported by the underlying shell platform, the targeted reload action MUST use the currently supported reload behavior while preserving the targeted API contract.
- **FR-007**: If a targeted reload request references an unknown shell identifier, the system MUST return a not-found outcome and MUST NOT report the request as successful.
- **FR-008**: If reload cannot begin because the configuration source is unavailable or cannot provide any usable shell configuration, the system MUST return a failure outcome and MUST leave the last active shell configuration in effect.
- **FR-009**: The system MUST provide responses that let callers distinguish successful reload completion from partial success, validation errors, authorization failures, busy rejections, and reload execution failures.
- **FR-010**: Successful reload operations MUST cause changed feature enablement and changed feature configuration to be reflected in subsequent interactions with affected shells without restarting the host application.
- **FR-011**: If a full reload encounters invalid configuration for only a subset of shells, the system MUST reload shells with valid configuration, MUST keep affected invalid shells on their last active configuration, and MUST report the operation as partial success.
- **FR-012**: If a reload request is received while another reload is already in progress, the system MUST reject the new request with a conflict or busy outcome and MUST NOT queue it automatically.
- **FR-013**: The targeted reload action MUST return a non-success outcome if the requested shell does not refresh successfully, even if the underlying fallback reload updates other shells successfully.
- **FR-014**: Reload responses MUST include per-shell outcome details for affected shells so callers can identify which shells refreshed successfully and which shells remained unchanged.

### Key Entities *(include if feature involves data)*

- **Shell**: A runtime-isolated application context identified uniquely within the host and configured with a set of enabled features and feature-specific settings.
- **Reload Request**: An administrative action asking the system to refresh shell runtime state either for all shells or for one named shell.
- **Reload Outcome**: The result of a reload request, including the top-level status and per-shell details showing whether each affected shell refreshed successfully, remained unchanged, was invalid, was unknown, or was not refreshed because the request was rejected.

## Assumptions

- Shell identifiers are unique and stable enough for administrators and automation to target the intended shell.
- Only privileged operational users or automation are expected to invoke shell reload actions.
- The current shell platform supports full reloads today, while selective per-shell reload will become available later.

## Dependencies

- The long-term optimization for truly selective reload depends on future shell-platform support for reloading a single shell without reloading all shells.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Administrators can apply configuration changes across all shells with a single API call and without restarting the host application.
- **SC-002**: After a successful reload request, updated feature enablement and feature configuration are observable in affected shells within 1 minute.
- **SC-003**: 100% of targeted reload requests for unknown shell identifiers return a non-success outcome.
- **SC-004**: 100% of reload attempts where the configuration source is unavailable return a failure outcome rather than a false success, and 100% of partial full reloads caused by shell-specific invalid configuration report partial success.
- **SC-005**: 100% of partial-success and targeted reload responses include shell-level outcome details for every affected shell.
