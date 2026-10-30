# Feature Specification: Configurable Login Themes

**Feature Branch**: `010-login-themes`
**Created**: 2026-07-25
**Status**: Draft
**Input**: User description: "Create an extensible, deploy-time configurable login-theme framework with a 3.7.0-style default and four modern Elsa workflow themes, while keeping all authentication behavior outside theme presentation."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Select a Login Theme at Deployment (Priority: P1)

As a deployer, I can select one registered login theme for the application so that the login experience reflects my preferred visual design without changing authentication behavior.

**Why this priority**: Deploy-time selection is the primary user value and the reason multiple built-in themes exist.

**Independent Test**: Start the application with each registered theme identifier and verify that the selected presentation appears while the same configured login methods remain available.

**Acceptance Scenarios**:

1. **Given** no theme is configured, **When** the application starts, **Then** the classic Elsa Studio 3.7.0-style presentation is used.
2. **Given** a registered theme identifier is configured, **When** the application starts, **Then** that theme presents the login experience.
3. **Given** an unknown theme identifier is configured, **When** the application starts, **Then** startup fails with a diagnostic that identifies the invalid value.
4. **Given** two extensions register the same theme identifier, **When** the application starts, **Then** startup fails with a diagnostic that identifies the duplicate identifier.

---

### User Story 2 - Authenticate Through Any Theme (Priority: P1)

As a user, I can use every available login method, warning, loading state, and error state regardless of the selected theme so that presentation never changes authentication capability or safety.

**Why this priority**: Theme selection must not compromise the ability to sign in or alter established authentication guarantees.

**Independent Test**: Exercise local and external authentication through different themes and verify equivalent methods, ordering, preferred guidance, safe return behavior, and failure handling.

**Acceptance Scenarios**:

1. **Given** local and external methods are available, **When** any built-in theme renders, **Then** local credentials appear first and external options remain available after a visual separator.
2. **Given** a preferred method is configured, **When** the login page appears, **Then** the method is identified visually but does not start without user action.
3. **Given** login methods are loading, unavailable, or fail to initialize, **When** any theme renders, **Then** the shared loading, unavailable, warning, or error state is presented.
4. **Given** an unsafe return destination is supplied, **When** authentication begins, **Then** the existing safe return-destination behavior is preserved.
5. **Given** an authentication method supports an optional control, **When** it renders in the shared panel, **Then** the control is available; unsupported controls are not simulated by the theme.

---

### User Story 3 - Extend Login Presentation (Priority: P1)

As a host or module developer, I can register a custom login presentation under a stable identifier so that I can deliver a branded experience without modifying the framework or duplicating login behavior.

**Why this priority**: The extension contract is required to satisfy the open/closed design goal.

**Independent Test**: Add a new presentation from an external module, select it by identifier, and verify that it receives shared branding and login content without changing existing framework sources.

**Acceptance Scenarios**:

1. **Given** a custom presentation is explicitly registered, **When** its identifier is configured, **Then** it presents the shared login experience.
2. **Given** an advanced custom presentation provider is registered, **When** its identifier is configured, **Then** it can compose a full page from the same presentation context.
3. **Given** a custom presentation requires its own visual settings or assets, **When** it renders, **Then** it can use module-owned configuration and assets without additions to the core configuration model.
4. **Given** a custom presentation throws during rendering, **When** the login page handles the failure, **Then** users receive a minimal recovery presentation with the functional shared login panel and the failure is recorded for operators.

---

### User Story 4 - Use the Classic Login Experience (Priority: P2)

As an existing Elsa Studio user, I see a familiar login page matching the 3.7.0 visual language so that an upgrade does not unexpectedly change the default experience.

**Why this priority**: Backward-compatible presentation protects existing deployments while the new themes remain opt-in.

**Independent Test**: Start without theme configuration and compare the resulting experience with the established 3.7.0 style, colors, proportions, branding pane, wave background, utilities, and version treatment.

**Acceptance Scenarios**:

1. **Given** no theme is configured, **When** the login page loads on a wide screen, **Then** it shows a pale wave background and a centered two-pane card with blue branding and a restrained login surface.
2. **Given** the host supplies custom branding, **When** classic renders, **Then** the configured logo, application name, tagline, background, utility-link visibility, and version data are honored.
3. **Given** several login methods are available, **When** classic renders, **Then** the card grows to contain the shared panel while the branding pane remains visually matched.
4. **Given** a narrow viewport, **When** classic renders, **Then** the experience collapses to a usable single-panel layout without horizontal scrolling.

---

### User Story 5 - Choose a Modern Elsa Theme (Priority: P2)

As a deployer, I can choose from four polished themes that communicate Elsa's workflow-orchestration capabilities so that the login experience represents the product's power.

**Why this priority**: The selected designs provide the visual differentiation promised by the feature.

**Independent Test**: Select each modern identifier and verify its distinctive composition on wide and narrow screens while shared login content remains usable.

**Acceptance Scenarios**:

1. **Given** `workflow-constellation` is selected, **When** login renders, **Then** users see the dark constellation-style workflow composition represented by concept 1.
2. **Given** `workflow-aurora` is selected, **When** login renders, **Then** users see the luminous workflow-aurora composition represented by concept 4.
3. **Given** `execution-timeline` is selected, **When** login renders, **Then** users see the dark execution-timeline composition represented by concept 9.
4. **Given** `human-automation` is selected, **When** login renders, **Then** users see the warm human-and-automation composition represented by concept 10.
5. **Given** artwork is still loading or unavailable, **When** a modern theme renders, **Then** the login panel remains available over an intentional fallback surface.

---

### User Story 6 - Preserve Branding and Shared Content (Priority: P2)

As a white-label host developer, I can provide application branding that every theme receives so that theme choice does not defeat host-level customization.

**Why this priority**: Shared branding is essential for host applications and avoids hardcoded Elsa identity in extension contracts.

**Independent Test**: Supply alternate branding and verify that every built-in theme uses the provided application identity and shared utilities while retaining its composition.

**Acceptance Scenarios**:

1. **Given** alternate branding is configured, **When** a built-in theme renders, **Then** its essential logo, application name, and tagline come from the host branding source.
2. **Given** documentation or source links are enabled, **When** a theme renders, **Then** the shared utility links are available in the theme's chosen position.
3. **Given** version information is available, **When** classic renders, **Then** it is displayed; modern and custom themes may choose whether to display it.
4. **Given** the application locale changes, **When** shared login content renders, **Then** common headings, labels, states, dividers, and actions use localized text.

---

### User Story 7 - Load Responsive, Private Artwork (Priority: P3)

As a user, I receive visually rich artwork without delaying login, leaking requests to third parties, or losing usability on a narrow screen.

**Why this priority**: The artwork provides visual quality, while authentication performance and privacy remain more important.

**Independent Test**: Load each built-in theme at wide and narrow viewports while observing asset origins, login readiness, artwork fallback, and responsive layout.

**Acceptance Scenarios**:

1. **Given** a built-in theme is selected, **When** its artwork loads, **Then** all built-in visual assets originate from the application.
2. **Given** a narrow viewport, **When** a built-in theme renders, **Then** artwork uses an appropriate crop or mobile asset and the shared panel remains centered and fully usable.
3. **Given** slow or failed artwork loading, **When** the page appears, **Then** users can interact with the login panel without waiting for artwork.
4. **Given** a built-in raster asset is inspected, **When** essential content is evaluated, **Then** no required branding, instructions, controls, or meaningful labels exist only inside the image.

### Edge Cases

- No login methods are registered.
- A login-method catalog fails while others are available.
- A preferred method key does not match any returned method.
- Many external login methods cause the panel to exceed the original theme mockup height.
- Branding omits a logo, tagline, background, or utility link.
- The configured artwork cannot be decoded or loaded.
- A custom theme provider fails before producing content.
- A custom theme identifier differs only by letter case.
- A theme's optional configuration is absent or invalid.
- The viewport changes after initial rendering.
- Localization resources are missing for theme-specific optional copy.
- Browser motion preferences request reduced motion.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST select one application-wide login theme from deployment configuration at startup.
- **FR-002**: The system MUST use `classic` when no theme is configured.
- **FR-003**: The system MUST provide the stable built-in identifiers `classic`, `classic-unified`, `classic-brand-canvas`, `workflow-constellation`, `workflow-aurora`, `execution-timeline`, and `human-automation`. `classic` MUST remain a compatibility alias for `classic-unified`.
- **FR-004**: The system MUST reject unknown configured theme identifiers during startup.
- **FR-005**: The system MUST reject duplicate registered theme identifiers during startup.
- **FR-006**: Theme identifiers MUST resolve consistently without depending on registration order.
- **FR-007**: Hosts and modules MUST be able to register a custom presentation explicitly under a stable identifier.
- **FR-008**: Advanced extensions MUST be able to supply a full presentation composition from the standard presentation context.
- **FR-009**: The standard presentation context MUST provide host branding, shared login content, shared utility links, and version information.
- **FR-010**: Themes MUST NOT own login-method discovery, ordering, validation, authentication initiation, safe return handling, loading state, or failure reporting.
- **FR-011**: The system MUST render all login methods and shared authentication states through one reusable login panel.
- **FR-012**: The shared panel MUST place local authentication first when available and preserve all external authentication options.
- **FR-013**: The shared panel MUST retain existing preferred-method, warning, error, loading, unavailable, and safe-return behavior.
- **FR-014**: Themes MUST be able to choose the position and visual treatment of the shared login panel through a documented presentation contract.
- **FR-015**: The presentation contract MUST expose stable styling controls without requiring access to private panel structure.
- **FR-016**: Every built-in theme MUST use host-provided logo, application name, tagline, utility-link visibility, and available version information as specified by that theme.
- **FR-017**: Shared login text MUST use the application's localization system.
- **FR-018**: Themes MUST be able to own optional configuration and assets without expanding the core theme-selection schema.
- **FR-019**: Built-in modern themes MUST use decorative raster artwork derived from the approved concepts.
- **FR-020**: Built-in raster artwork MUST NOT be the sole carrier of essential text, branding, controls, instructions, or meaningful workflow labels.
- **FR-021**: Built-in theme assets MUST load only from the application origin.
- **FR-022**: Artwork loading or failure MUST NOT block interaction with the shared login panel.
- **FR-023**: Every built-in theme MUST provide a usable wide-screen and narrow-screen composition without horizontal scrolling.
- **FR-024**: Themes MUST NOT be automatically inverted based on application light/dark preference.
- **FR-025**: The system MUST render a minimal functional recovery presentation and record the failure when a valid selected custom theme fails during rendering.
- **FR-026**: The classic theme MUST visually match the established 3.7.0 style, colors, proportions, blue branding pane, pale wave background, utility placement, and version treatment.
- **FR-027**: The four modern themes MUST remain optional so minimal hosts can omit their assets.
- **FR-028**: The standard Elsa distribution MUST make all four modern themes available for configuration.
- **FR-029**: Theme changes MUST take effect after application restart; runtime theme switching is not required.
- **FR-030**: Public guidance MUST document theme selection, built-in identifiers, custom registration, advanced rendering, theme-owned settings and assets, styling controls, and a custom-module example.
- **FR-031**: Existing login keyboard operation, visible focus, landmarks, labels, text fallbacks, same-origin presentation guarantees, and serious/critical accessibility baseline MUST be preserved.
- **FR-032**: Final optimized raster-size thresholds MUST be recorded from fidelity-preserving outputs with 20 percent regression headroom.

### Key Entities

- **Login Theme Registration**: Associates a stable identifier with a presentation extension and establishes identifier uniqueness.
- **Login Theme Selection**: The startup-resolved choice of one registered theme for the application.
- **Login Presentation Context**: The logic-free content supplied to a selected theme, including branding, shared login content, utilities, and version information.
- **Shared Login Panel**: The single reusable presentation of login methods and authentication states.
- **Theme Artwork**: Optional theme-owned decorative assets and their responsive presentation metadata.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Deployers can select any of the five built-in themes through one configuration value and see the selected presentation after one application restart.
- **SC-002**: All login methods available before the feature remain available and operable in every built-in theme.
- **SC-003**: An external module can add and select a custom theme without modifying existing framework source files.
- **SC-004**: Unknown and duplicate theme identifiers are reported before the application accepts user traffic.
- **SC-005**: The shared login panel is interactive without waiting for decorative artwork in every built-in theme.
- **SC-006**: All five built-in themes remain fully usable at representative desktop and narrow mobile viewport widths without horizontal scrolling.
- **SC-007**: 100 percent of built-in theme asset requests are same-origin.
- **SC-008**: 100 percent of essential login text and controls remain available outside raster artwork.
- **SC-009**: Existing automated login checks report no new serious or critical accessibility findings, keyboard regressions, labeling regressions, or unsafe external presentation assets.
- **SC-010**: The classic default is recognizably consistent with the 3.7.0 login experience in stakeholder review.
- **SC-011**: Each modern theme is recognizably consistent with its approved concept in stakeholder review.
- **SC-012**: Final optimized artwork remains within the measured fidelity-preserving budget plus no more than 20 percent headroom.

## Assumptions

- Existing authentication-method implementations and security contracts remain authoritative.
- Theme selection is per deployment, not per tenant or user.
- Hosts restart the application when configuration changes.
- Built-in modern themes are packaged separately from the core authentication UI.
- Custom themes are trusted application extensions and are responsible for their own optional settings, assets, and content-security-policy compatibility.
- The legacy login module remains available and unchanged.
- Visual stakeholder review uses the approved concepts and the 3.7.0 reference as source material rather than demanding literal reuse of legacy markup.
