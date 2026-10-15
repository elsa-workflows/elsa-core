# Elsa Studio 3.6.0-rc1

## Highlights

- **Workflow Designer improvements**
  - **Vertices support** for more precise connector routing and cleaner diagrams ([#480]).
  - **TreeView-based Activity Picker** for faster navigation in large catalogs ([#544]).
  - **Flowchart handling + performance improvements**, including better node handling and editor responsiveness ([#499], [#669], [#670], [#686]).
  - **Activity testing support** to speed up iteration while designing workflows ([#541], [#612]).

- **Workflow Editor productivity**
  - **Save As** support ([#625]).
  - **Ctrl+S / Cmd+S hotkey** support ([#635]).
  - **Duplicate workflow from definition list** ([#569]).

- **Auth & integration**
  - **PKCE support** for OIDC authorization code flow ([#519]).
  - **JWT support** for `WorkflowInstanceObserverFactory` ([#463]).

- **UI/UX polish**
  - **Redesigned & modernized login page** ([#696]).
  - Multiple UI consistency fixes and improvements across the Studio ([#511], [#512], [#555]).

- **Localization**
  - **French translations** ([#532]).

## What’s Changed

- Fixed bug where clicking on sub workflow incident/journal would not g… by @MariusVuscanNx ([#504])
- Added name field to the instances. by @MariusVuscanNx ([#505])
- Fixed checkbox deselect bug on page size update by @MariusVuscanNx ([#506])
- Add per-activity MergeMode selector by @sfmskywalker ([#507])
- Updates all Nuget Packages and Elsa to 3.6.0-preview.2935 by @KnibbsyMan ([#510])
- Normalise UI Inconsistencies by @KnibbsyMan ([#511])
- Fixes missing closing tag and adds dense setting to new elements. by @KnibbsyMan ([#512])
- Implement Vertices by @sfmskywalker ([#480])
- Enhance flowchart handling and improve performance by @MaxBrooks114 ([#499])
- Remove Agents from Studio. by @KnibbsyMan ([#513])
- Remove Secrets, Webhooks and WorkflowContexts by @KnibbsyMan ([#517])
- Add JWT support to WorkflowInstanceObserverFactory by @LarsNijholt ([#463])
- Add customizable error component and user message service by @lanseth ([#490])
- French translations by @agriffard ([#532])
- Adds RadioList UI Hint Support by @KnibbsyMan ([#537])
- Remove duplicate components from workflow properties tab. by @KnibbsyMan ([#539])
- Add PKCE support for OIDC auth code flow by @michael-livingston ([#519])
- Enhancment - Allow to override modulloading by @Schneggo ([#543])
- Fix module URL for `JsInteropBase` in `Elsa.Studio.Workflows.Designer`. by @Chryssie ([#545])
- Replace Workflow Editor Activity Picker with TreeView by @KnibbsyMan ([#544])
- Activity Testing Support by @sfmskywalker ([#541])
- New Content Formatting Viewer by @KnibbsyMan ([#549])
- Minor UI improvements by @KnibbsyMan ([#555])
- Update README.md to include updated localization setup. by @KnibbsyMan ([#556])
- Add CategoryDisplayResolver to AccordionActivityPicker by @KnibbsyMan ([#557])
- Re-factor ContentFormatter to ContentVisualizer. by @KnibbsyMan ([#559])
- Fix Directory.Packages.props file by @BasJanssenQuadira ([#560])
- [FEAT] Make resizing configurable by @BasJanssenQuadira ([#561])
- Duplicate Workflow Option From Defintion List by @KnibbsyMan ([#569])
- Component refresh on the editor load to show the latest model from server by @lukhipolito ([#571])
- Fix/component losing focus when editing by @lukhipolito-nexxbiz ([#573])
- Add studio labels by @RalfvandenBurg ([#497])
- Bump the npm_and_yarn group across 1 directory with 4 updates by @dependabot[bot] ([#567])
- Bump the npm_and_yarn group across 1 directory with 8 updates by @dependabot[bot] ([#590])
- Merge 3.5.1 into 3.6.0 by @sfmskywalker ([#600])
- Workflow input as activity input by @mberthillot-flowwa ([#601])
- Add XML documentation across public API by @sfmskywalker ([#609])
- Add fault exception messages to the TestTab component. by @KnibbsyMan ([#612])
- Adding workflow state as JSON to details tab by @mcook-dev ([#603])
- Add Save As Functionality to Workflow Editor by @KnibbsyMan ([#625])
- Update JetBrains.Annotations to also have PrivateAssets="all" by @RalfvandenBurg ([#628])
- Add hotkey support for saving workflows with Ctrl+S/Cmd+S by @KnibbsyMan ([#635])
- Add Markdown Descriptions Support by @KnibbsyMan ([#659])
- Fix: Prevent extra Flowchart node from appearing by correctly detecting Elsa.Flowchart activities by @sfmskywalker ([#669])
- Fix “one-interaction-late” UI updates in InputsTab by avoiding debounced rebuilds on value changes by @sfmskywalker ([#670])
- Add .github/copilot-instructions.md for repository onboarding by @Copilot ([#671])
- Add XML comments for public members to resolve CS1591 warnings by @Copilot ([#608])
- Add .NET 10 LTS target support with multi-targeting by @Copilot ([#674])
- Redesign and Modernise the Elsa Login page. by @KnibbsyMan ([#696])
- Optimize workflow designer activity size calculation with batching and caching by @Copilot ([#686])

## New Contributors

- @LarsNijholt made their first contribution in [#463]
- @lanseth made their first contribution in [#490]
- @agriffard made their first contribution in [#532]
- @michael-livingston made their first contribution in [#519]
- @Schneggo made their first contribution in [#543]
- @BasJanssenQuadira made their first contribution in [#560]
- @lukhipolito made their first contribution in [#571]
- @RalfvandenBurg made their first contribution in [#497]
- @mberthillot-flowwa made their first contribution in [#601]
- @mcook-dev made their first contribution in [#603]

**Full Changelog**: [3.5.1...3.6.0-rc1]

<!-- Link references (clean copy/paste for GitHub Releases) -->
[#463]: https://github.com/elsa-workflows/elsa-studio/pull/463
[#480]: https://github.com/elsa-workflows/elsa-studio/pull/480
[#490]: https://github.com/elsa-workflows/elsa-studio/pull/490
[#497]: https://github.com/elsa-workflows/elsa-studio/pull/497
[#499]: https://github.com/elsa-workflows/elsa-studio/pull/499
[#504]: https://github.com/elsa-workflows/elsa-studio/pull/504
[#505]: https://github.com/elsa-workflows/elsa-studio/pull/505
[#506]: https://github.com/elsa-workflows/elsa-studio/pull/506
[#507]: https://github.com/elsa-workflows/elsa-studio/pull/507
[#510]: https://github.com/elsa-workflows/elsa-studio/pull/510
[#511]: https://github.com/elsa-workflows/elsa-studio/pull/511
[#512]: https://github.com/elsa-workflows/elsa-studio/pull/512
[#513]: https://github.com/elsa-workflows/elsa-studio/pull/513
[#517]: https://github.com/elsa-workflows/elsa-studio/pull/517
[#519]: https://github.com/elsa-workflows/elsa-studio/pull/519
[#532]: https://github.com/elsa-workflows/elsa-studio/pull/532
[#537]: https://github.com/elsa-workflows/elsa-studio/pull/537
[#539]: https://github.com/elsa-workflows/elsa-studio/pull/539
[#541]: https://github.com/elsa-workflows/elsa-studio/pull/541
[#543]: https://github.com/elsa-workflows/elsa-studio/pull/543
[#544]: https://github.com/elsa-workflows/elsa-studio/pull/544
[#545]: https://github.com/elsa-workflows/elsa-studio/pull/545
[#549]: https://github.com/elsa-workflows/elsa-studio/pull/549
[#555]: https://github.com/elsa-workflows/elsa-studio/pull/555
[#556]: https://github.com/elsa-workflows/elsa-studio/pull/556
[#557]: https://github.com/elsa-workflows/elsa-studio/pull/557
[#559]: https://github.com/elsa-workflows/elsa-studio/pull/559
[#560]: https://github.com/elsa-workflows/elsa-studio/pull/560
[#561]: https://github.com/elsa-workflows/elsa-studio/pull/561
[#567]: https://github.com/elsa-workflows/elsa-studio/pull/567
[#569]: https://github.com/elsa-workflows/elsa-studio/pull/569
[#571]: https://github.com/elsa-workflows/elsa-studio/pull/571
[#573]: https://github.com/elsa-workflows/elsa-studio/pull/573
[#590]: https://github.com/elsa-workflows/elsa-studio/pull/590
[#600]: https://github.com/elsa-workflows/elsa-studio/pull/600
[#601]: https://github.com/elsa-workflows/elsa-studio/pull/601
[#603]: https://github.com/elsa-workflows/elsa-studio/pull/603
[#608]: https://github.com/elsa-workflows/elsa-studio/pull/608
[#609]: https://github.com/elsa-workflows/elsa-studio/pull/609
[#612]: https://github.com/elsa-workflows/elsa-studio/pull/612
[#625]: https://github.com/elsa-workflows/elsa-studio/pull/625
[#628]: https://github.com/elsa-workflows/elsa-studio/pull/628
[#635]: https://github.com/elsa-workflows/elsa-studio/pull/635
[#659]: https://github.com/elsa-workflows/elsa-studio/pull/659
[#669]: https://github.com/elsa-workflows/elsa-studio/pull/669
[#670]: https://github.com/elsa-workflows/elsa-studio/pull/670
[#671]: https://github.com/elsa-workflows/elsa-studio/pull/671
[#674]: https://github.com/elsa-workflows/elsa-studio/pull/674
[#686]: https://github.com/elsa-workflows/elsa-studio/pull/686
[#696]: https://github.com/elsa-workflows/elsa-studio/pull/696

[3.5.1...3.6.0-rc1]: https://github.com/elsa-workflows/elsa-studio/compare/3.5.1...3.6.0-rc1

