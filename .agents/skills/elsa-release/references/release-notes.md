## Curated Release Notes

Recommendation: use curated release notes for stable releases and meaningful previews. GitHub generated notes are useful raw input, but the final release should explain why changes matter to developers consuming Elsa packages.

Use this structure:

```markdown
Compare: <from-ref>...<to-ref>

---

## 🌟 Highlights

---

## ⚠️ Breaking changes / upgrade notes

---

## ✨ New features

### Component or theme

---

## 🔧 Improvements

---

## 🐛 Fixes

---

## 🔒 Security

---

## 🧩 Developer-facing changes

---

## 🧪 Tests

---

## 🔁 CI / Build

---

## 📦 Dependencies

---

## 📦 Full changelog (short)
```

Omit empty sections. Put the highest-signal user-facing changes in `Highlights` first, limited to 3-6 bullets. Keep `Full changelog` comprehensive so every commit or PR in the range is represented somewhere.

Writing rules:

- Prefer PR titles and labels when available; otherwise use commit subjects.
- Do not paste a flat generated changelog as the final result.
- Group related changes under component-oriented subsection headings when that improves scanning, e.g. `#### Workflows`, `#### Shells`, `#### HTTP`, `#### Persistence`.
- Follow the Elsa `3.7.0-rc1` style: compare line first, `---` separators, `##` category headings with small icons, component prefix before the colon, and a short full changelog at the end.
- For breaking changes, include who is affected and what to do.
- For fixes, explain the observable problem that was corrected, not only the implementation detail.
- For dependency/package changes, include package names and versions when available.
- End bullets with a PR number or short SHA when available, e.g. `(#7400)` or `(b88af1e02)`.
- Never invent PR numbers, affected components, migration steps, or known issues.

Generate a scaffold:

```bash
python3 .agents/skills/elsa-release/scripts/release_notes.py \
  --repo-path . \
  --from-ref 3.6.2 \
  --to-ref 3.7.0 \
  --version 3.7.0 \
  --output doc/changelogs/3.7.0.md
```

Then edit the scaffold into polished notes and release with:

```bash
python3 .agents/skills/elsa-release/scripts/release.py \
  --repo-path . \
  --source-ref origin/release/3.7.0 \
  --tag 3.7.0 \
  --release-kind stable \
  --notes-file doc/changelogs/3.7.0.md
```

If the GitHub release already exists and only the notes need improvement, update it with:

```bash
gh release edit 3.7.0 \
  --repo elsa-workflows/elsa-core \
  --notes-file doc/changelogs/3.7.0.md
```
