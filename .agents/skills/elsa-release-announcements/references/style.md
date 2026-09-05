## Announcement Shape

Use this structure for the announcement pack:

```markdown
# Elsa <version> Announcement Pack

## Facts

## Discord

## LinkedIn

## X single-post option

## X thread option

## Links
```

Channel guidance:

- Discord can be more direct, celebratory, and useful: mention the release, top changes, package availability, and links. Prefer `:rocket:`, `:point_right:`, `:sparkles:`, `:tools:`, `:test_tube:`, and similar Discord emoji shortcodes over raw Unicode emoji.
- Discord posts should suppress link previews. Use the webhook `SUPPRESS_EMBEDS` message flag and wrap links in angle brackets, for example `<https://github.com/elsa-workflows/elsa-core/releases/tag/3.7.0>`.
- Discord stable releases should say the stable version is available and ask for feedback on upgrades or regressions.
- Discord preview/RC releases should explicitly say they are intended for testing and validation before stable release.
- LinkedIn should explain the release in terms of developer value and project momentum, with fewer implementation details.
- X should be concise. Use a thread when there are more than two high-signal points.
- Stable releases may say packages are available on NuGet only after verifying that publish succeeded.
- Preview/RC announcements must clearly say preview/RC and avoid implying production stability.

## Discord Style

Use this shape for Discord drafts and adapt the details to the actual release:

```markdown
:rocket: **Elsa Workflows 3.7.0 is here!**

We've published the stable **Elsa 3.7.0** release across **Elsa Core** and **Elsa Studio**.

:point_right: Core: <https://github.com/elsa-workflows/elsa-core/releases/tag/3.7.0>
:point_right: Studio: <https://github.com/elsa-workflows/elsa-studio/releases/tag/3.7.0>

This release brings a solid set of improvements around **authentication**, **workflow diagnostics**, **Studio extensibility**, and the **modular server runtime**.

### :sparkles: Highlights

:closed_lock_with_key: **Modern authentication support in Elsa Studio**
Summarize the high-value change in one or two practical sentences.

:compass: **Improved workflow instance diagnostics**
Summarize the most user-visible diagnostics improvements.

:jigsaw: **Modular server runtime improvements**
Summarize the Core/runtime changes.

### :tools: Upgrade notes
Call out compatibility or dependency changes users should validate.

### :raised_hands: Feedback welcome
Ask users to report upgrade issues, regressions, and bugs.
```
