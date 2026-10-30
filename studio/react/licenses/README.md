# Dependency notices omitted from published packages

The dependency-license build check reads installed production dependencies and fails
on unreviewed licenses or missing notices. Three current artifacts omit their upstream
MIT license files, so their notices are retained here and bound to exact versions in
the checker. These are dependency notices, separate from donor-source licensing.

| Artifact | Retained notice source |
| --- | --- |
| `@uiw/react-codemirror@4.25.12`, `@uiw/codemirror-extensions-basic-setup@4.25.12` | [MIT license at the v4.25.12 source commit](https://github.com/uiwjs/react-codemirror/blob/1e2e11f865860c6bd5029d71a2a09bd894ed5bad/LICENSE) |
| `react-remove-scroll-bar@2.3.8` | [Upstream MIT license addition](https://github.com/theKashey/react-remove-scroll-bar/blob/8ca9ba5ea52de03308fe8ced94f7b159a44d28ff/LICENSE); the published artifact's `gitHead` is `b3b1287aad81def2e2ae707274b74531b61ddbaf` and declares MIT |

The vendored `src/styles/shadcn.css` is from `shadcn@4.21.4` and carries its complete
MIT notice. Keeping the stylesheet avoids installing the unused generation CLI and
its transitive dependency tree. Update these notices when updating the pinned inputs.

Every build includes `THIRD-PARTY-NOTICES.txt` with the production dependency notices.
