# Tasks: Configurable Login Themes

**Input**: Design documents from `specs/010-login-themes/`
**Prerequisites**: `plan.md`, `spec.md`, `research.md`, `data-model.md`, `contracts/`

**Tests**: Add only service-level registry/configuration tests and retain the
existing browser suite. Component-rendering and screenshot-baseline tests are
explicitly excluded.

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Create the optional theme package and non-component test boundary.

- [x] T001 Add `Elsa.Studio.Authentication.Themes` and `Elsa.Studio.Authentication.UI.Tests` projects to `Elsa.Studio.sln`
- [x] T002 [P] Create the Razor class library project in `src/modules/Elsa.Studio.Authentication.Themes/Elsa.Studio.Authentication.Themes.csproj`
- [x] T003 [P] Create the xUnit service-test project in `src/modules/Elsa.Studio.Authentication.UI.Tests/Elsa.Studio.Authentication.UI.Tests.csproj`
- [x] T004 Reference the optional theme project from the standard bundle in `src/bundles/Elsa.Studio/Elsa.Studio.csproj`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Establish the logic-free theme contract and shared login behavior.

- [x] T005 [P] Define theme IDs, options, registration, branding, and context models under `src/modules/Elsa.Studio.Authentication.UI/Models/` and `src/modules/Elsa.Studio.Authentication.UI/Options/`
- [x] T006 [P] Define `ILoginThemeProvider`, `ILoginThemeRegistry`, and `LoginThemeComponentBase` under `src/modules/Elsa.Studio.Authentication.UI/Contracts/`
- [x] T007 Implement component-provider adaptation, registration indexing, selection, and options validation under `src/modules/Elsa.Studio.Authentication.UI/Services/`
- [x] T008 Add source-compatible configuration and explicit component/provider registration APIs in `src/modules/Elsa.Studio.Authentication.UI/Extensions/ServiceCollectionExtensions.cs`
- [x] T009 Extract catalog loading, method ordering, safe-return normalization, and shared states into `src/modules/Elsa.Studio.Authentication.UI/Components/LoginPanel.razor`
- [x] T010 [P] Add shared utility-link rendering in `src/modules/Elsa.Studio.Authentication.UI/Components/LoginUtilityLinks.razor`
- [x] T011 Add public shared-panel CSS tokens and accessible defaults in `src/modules/Elsa.Studio.Authentication.UI/wwwroot/css/login.css`

**Checkpoint**: A theme can receive functional shared content without receiving
authentication services or private panel structure.

---

## Phase 3: User Story 1 - Select a Login Theme at Deployment (Priority: P1) 🎯 MVP

**Goal**: Select one registered theme from startup configuration, with classic
default and deterministic invalid-configuration failures.

**Independent Test**: Resolve the service provider with absent, known, unknown,
blank, differently cased, and duplicate IDs and observe deterministic selection
or startup failure.

- [x] T012 [P] [US1] Write options-validation cases in `src/modules/Elsa.Studio.Authentication.UI.Tests/LoginThemeOptionsValidatorTests.cs`
- [x] T013 [P] [US1] Write deterministic registry-selection cases in `src/modules/Elsa.Studio.Authentication.UI.Tests/LoginThemeRegistryTests.cs`
- [x] T014 [US1] Register startup validation and ordinary `classic` registration in `src/modules/Elsa.Studio.Authentication.UI/Extensions/ServiceCollectionExtensions.cs`
- [x] T015 [US1] Bind `Authentication:Login` and force validation in `src/hosts/Elsa.Studio.Host.Server/Program.cs`
- [x] T016 [P] [US1] Bind `Authentication:Login` and force validation in `src/hosts/Elsa.Studio.Host.Wasm/Program.cs`
- [x] T017 [P] [US1] Add documented default login-theme settings in `src/hosts/Elsa.Studio.Host.Server/appsettings.json` and `src/hosts/Elsa.Studio.Host.Wasm/wwwroot/appsettings.json`

**Checkpoint**: Configuration selects a single stable ID and invalid deployment
configuration fails before login traffic is accepted.

---

## Phase 4: User Story 2 - Authenticate Through Any Theme (Priority: P1)

**Goal**: Preserve every authentication method and state through one theme-
independent panel.

**Independent Test**: Run the existing external-authentication browser journeys
with the themed host and verify method discovery, preferred guidance, safe
return handling, warnings, errors, and accessibility remain intact.

- [x] T018 [US2] Reduce `/login` to route/query ownership and theme-host composition in `src/modules/Elsa.Studio.Authentication.UI/Pages/Login.razor`
- [x] T019 [US2] Preserve local-first ordering with an external-method divider in `src/modules/Elsa.Studio.Authentication.UI/Components/LoginPanel.razor`
- [x] T020 [US2] Keep login-method components logic-free from theme state in `src/modules/Elsa.Studio.Authentication.UI/Components/LoginPanel.razor`
- [x] T021 [US2] Update behavior and accessibility assertions only where markup-neutral selectors require it in `tests/browser/ExternalAuthentication/`

**Checkpoint**: Changing themes changes no authentication capability or safety
behavior.

---

## Phase 5: User Story 3 - Extend Login Presentation (Priority: P1)

**Goal**: Allow host/module component themes and advanced providers while
recovering from valid provider render failures.

**Independent Test**: Register a test component and provider by ID, select each,
and verify the framework supplies context; make a provider throw and confirm a
logged minimal recovery shell retains the panel.

- [x] T022 [P] [US3] Implement the selected-provider host in `src/modules/Elsa.Studio.Authentication.UI/Components/LoginThemeHost.razor`
- [x] T023 [P] [US3] Implement logging render-failure handling in `src/modules/Elsa.Studio.Authentication.UI/Components/LoginThemeErrorBoundary.cs`
- [x] T024 [US3] Implement the selector-independent recovery shell in `src/modules/Elsa.Studio.Authentication.UI/Components/LoginThemeRecovery.razor`
- [x] T025 [US3] Document host/module component and provider examples in `src/modules/Elsa.Studio.Authentication.UI/README.md`

**Checkpoint**: A custom module can extend presentation without framework edits
or copied login logic, and cosmetic runtime failure does not block sign-in.

---

## Phase 6: User Story 4 - Use the Classic Login Experience (Priority: P2)

**Goal**: Make the no-configuration default visually match the Elsa Studio
3.7.0 login experience while using the new shared panel.

**Independent Test**: Review the default at desktop and mobile widths against
the supplied 3.7.0 reference, including pale waves, split card, blue branding,
utilities, version, growth for multiple methods, and narrow collapse.

- [x] T026 [US4] Build the ordinary registered Classic Unified theme in `src/modules/Elsa.Studio.Authentication.UI/Components/Themes/ClassicUnifiedLoginTheme.razor`
- [x] T027 [US4] Add responsive classic composition and branding treatment in `src/modules/Elsa.Studio.Authentication.UI/wwwroot/css/login.css`
- [x] T028 [US4] Project host branding and client version into the context in `src/modules/Elsa.Studio.Authentication.UI/Services/LoginThemeContextFactory.cs`
- [x] T029 [US4] Verify classic background and branding fallback assets under `src/framework/Elsa.Studio.Shell/wwwroot/img/`

**Checkpoint**: `classic` is a normal theme and the recognizable compatibility
default.

---

## Phase 7: User Story 5 - Choose a Modern Elsa Theme (Priority: P2)

**Goal**: Deliver the four approved raster-rich workflow themes as an optional
pack with maximum shared structure.

**Independent Test**: Select each stable ID and compare desktop/mobile renders
to approved concepts 1, 4, 9, and 10 while confirming shared controls remain
semantic and usable.

- [x] T030 [P] [US5] Generate clean decorative raster plates from approved concepts for `src/modules/Elsa.Studio.Authentication.Themes/wwwroot/images/`
- [x] T031 [US5] Optimize first-party raster plates and record measured sizes plus 20% thresholds in `src/modules/Elsa.Studio.Authentication.Themes/wwwroot/images/asset-budget.json`
- [x] T032 [P] [US5] Create the shared modern-theme frame in `src/modules/Elsa.Studio.Authentication.Themes/Components/ModernLoginThemeFrame.razor`
- [x] T033 [P] [US5] Implement `workflow-constellation` in `src/modules/Elsa.Studio.Authentication.Themes/Components/WorkflowConstellationLoginTheme.razor`
- [x] T034 [P] [US5] Implement `workflow-aurora` in `src/modules/Elsa.Studio.Authentication.Themes/Components/WorkflowAuroraLoginTheme.razor`
- [x] T035 [P] [US5] Implement `execution-timeline` in `src/modules/Elsa.Studio.Authentication.Themes/Components/ExecutionTimelineLoginTheme.razor`
- [x] T036 [P] [US5] Implement `human-automation` in `src/modules/Elsa.Studio.Authentication.Themes/Components/HumanAutomationLoginTheme.razor`
- [x] T037 [US5] Add responsive variants and fallback surfaces in `src/modules/Elsa.Studio.Authentication.Themes/wwwroot/css/login-themes.css`
- [x] T038 [US5] Register all four stable IDs in `src/modules/Elsa.Studio.Authentication.Themes/Extensions/ServiceCollectionExtensions.cs`
- [x] T039 [US5] Register the optional theme pack in `src/hosts/Elsa.Studio.Host.Server/Program.cs` and `src/hosts/Elsa.Studio.Host.Wasm/Program.cs`

**Checkpoint**: The standard distribution offers four distinctive opt-in
themes; omitting the theme package still leaves core classic functional.

---

## Phase 8: User Story 6 - Preserve Branding and Shared Content (Priority: P2)

**Goal**: Honor host branding, shared utilities, available version information,
and localization across all themes.

**Independent Test**: Substitute a branding provider and locale, then verify
semantic application identity, utility visibility, shared labels/states, and
classic version output in all appropriate themes.

- [x] T040 [US6] Apply projected light/reverse logos, application name, and tagline in `src/modules/Elsa.Studio.Authentication.UI/Components/Themes/ClassicUnifiedLoginTheme.razor`
- [x] T041 [US6] Apply the same projected branding contract in `src/modules/Elsa.Studio.Authentication.Themes/Components/ModernLoginThemeFrame.razor`
- [x] T042 [US6] Localize shared headings, states, divider, and utility labels through `ILocalizer` in `src/modules/Elsa.Studio.Authentication.UI/Components/LoginPanel.razor` and `src/modules/Elsa.Studio.Authentication.UI/Components/LoginUtilityLinks.razor`

**Checkpoint**: Theme choice does not defeat white-label identity or shared
localization.

---

## Phase 9: User Story 7 - Load Responsive, Private Artwork (Priority: P3)

**Goal**: Keep rich same-origin imagery non-blocking, responsive, and
non-essential.

**Independent Test**: Observe network origins and interaction timing at wide and
narrow widths, block each image request, and inspect artwork for embedded
essential content.

- [x] T043 [US7] Use same-origin `_content` sources and non-blocking responsive image selection in `src/modules/Elsa.Studio.Authentication.Themes/Components/ModernLoginThemeFrame.razor`
- [x] T044 [US7] Add narrow crops, no-overflow rules, reduced-motion handling, and artwork-failure fallbacks in `src/modules/Elsa.Studio.Authentication.Themes/wwwroot/css/login-themes.css`
- [x] T045 [US7] Add an asset-origin and byte-budget verification script in `src/modules/Elsa.Studio.Authentication.Themes/verify-assets.mjs`

**Checkpoint**: Artwork quality does not delay, leak, or become essential to
authentication.

---

## Phase 10: Polish & Cross-Cutting Concerns

**Purpose**: Documentation, verification, cleanup, and final integration review.

- [x] T046 [P] Document built-in IDs, selection, restart semantics, options/assets ownership, CSS tokens, and the custom-module example in `src/modules/Elsa.Studio.Authentication.UI/README.md`
- [x] T047 [P] Document each optional theme and asset budget in `src/modules/Elsa.Studio.Authentication.Themes/README.md`
- [x] T048 Update the standard bundle inventory in `src/bundles/Elsa.Studio/Readme.md`
- [x] T049 Run format/build/tests across supported targets and the focused service/browser suites from `Elsa.Studio.sln`
- [x] T050 Execute `specs/010-login-themes/quickstart.md` including desktop/mobile manual screenshots and failed-artwork checks
- [x] T051 Review the full diff for authentication regressions, public API clarity, DRY theme composition, unrelated-file preservation, and absence of component/pixel tests

---

## Dependencies & Execution Order

### Phase Dependencies

- Setup has no dependencies.
- Foundational depends on Setup and blocks every user story.
- US1, US2, and US3 form the P1 framework increment. US2 depends on the shared
  foundation; US3 depends on selection and panel fragments.
- US4 depends on the host/context framework from US1-US3.
- US5 depends on the same framework but is otherwise isolated in the optional
  project.
- US6 integrates branding/localization across US4 and US5.
- US7 depends on final US5 artwork and frame behavior.
- Polish depends on all selected stories.

### User Story Completion Order

```text
Foundation
├── US1 Selection ──┐
├── US2 Shared auth ├── US3 Extension/recovery ──┬── US4 Classic ──┐
└───────────────────┘                            └── US5 Modern ───┴── US6 Branding
                                                                    └── US7 Assets
```

### Parallel Opportunities

- T002 and T003 create independent projects.
- T005, T006, and T010 define separate foundational files.
- T012 and T013 validate different service rules.
- Server and WebAssembly wiring tasks can proceed in parallel.
- T030 can generate artwork while T032 prepares the shared modern frame.
- T033-T036 implement separate thin theme wrappers after T032.
- T046 and T047 document separate packages.

## Implementation Strategy

### MVP

Complete Setup, Foundational, US1, US2, and US3. This yields a configurable,
extensible framework with `classic`, shared login behavior, and runtime recovery.

### Incremental Delivery

1. Framework and non-component service tests.
2. Classic compatibility experience.
3. Optional modern package and optimized artwork.
4. Branding/localization and asset hardening.
5. Full cross-target and browser verification.

## Format Validation

All 51 tasks use the required checkbox, sequential task ID, optional `[P]`
marker, user-story label within story phases, an actionable description, and an
explicit file path.
