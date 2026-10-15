# Legacy solution settings and README template

Program #8194; story #8286. This decision covers three retained developer assets from the pinned history receipt. Their original `.source` files and Git ancestry remain available in the draft import.

| Retained source | Active disposition |
| --- | --- |
| Extensions `Elsa.Extensions.sln.DotSettings` at `33fa0bfd28c7585240e3d4f665058c067b17e287` | The old solution name is inactive. Add its distinct `IO` naming abbreviation and lowercase `telnyx` dictionary entry to the active `Elsa.sln.DotSettings`; the root already knows `Telnyx`. |
| Studio `Elsa.Studio.sln.DotSettings` at `9afd3e36fd1bc90dfdf8ea00b40d89e4a50c8822` | The old solution name is inactive. Add its distinct `JS` naming abbreviation and product terms `Blazilla`, `Blazored`, `Heroicons`, `localizer` and `Tabler` to the active root settings. Root already carries `UI` and the same JetBrains migration markers. Do not impose Studio's `WRAP_LINES=False` on all Core projects or add its `ffffff` color token as a dictionary word. |
| Extensions `README-TEMPLATE.md` at `33fa0bfd28c7585240e3d4f665058c067b17e287` | Retire the old root template from the active tree. It describes the former group-folder layout, placeholder implementation packages and the separate Studio repository. The current Core tree has no active reference to this file; the only tracked references are provenance inventory/ledger/receipt and the inert Extensions solution copy. A future connector-authoring template must describe the consolidated layout and be reviewed separately. |

`Elsa.sln` is the canonical active solution, so the two old solution-specific settings files should not be installed beside it. The selected settings above preserve useful editor behavior without importing the old files wholesale. These decisions do not change build or package behavior and do not authorize removing the inert source copies before final import review.
