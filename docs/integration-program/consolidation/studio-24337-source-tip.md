# Studio source tip 24337549 (3.10 host branding)

After the import preserved Studio `5b34ec32`, Studio `main` merged elsa-studio#1073, reaching
`24337549a28be797b639708a87d5fcb9b8c693ee`. That change sets `ToolVersion` to 3.10, makes both hosts derive their
branding from it, and removes the server's `StudioBrandingProvider`. #8530 carried it into the import:

- `1043d3d9` applies the exact four-file delta to the mapped `src/studio` paths. The three kept files are byte-identical
  to upstream.
- `0653505a` joins the upstream tip as a second parent without changing the tree.
- `b42d9387` is the GitHub merge into the import branch. It keeps both parents.

The [ninth receipt](source-tip-refresh-2026-09-28-r9.json) pins those commits and each file's blobs.
`verify_import_source_tip_refresh_r9.py` fails if:
- the upstream delta reaches beyond these Studio paths;
- a kept file stops matching upstream;
- the deleted provider reappears;
- the history join is lost;
- the publisher workflows drift from the seventh receipt's reviewed bytes.

The mapped Slack package proof requires it. `Elsa.Studio.Host.Server` built with 0 errors, and `Elsa.Studio.Core.Tests`
passed 28/28, at the delta. No package was published.
