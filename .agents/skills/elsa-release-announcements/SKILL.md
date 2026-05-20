---
name: elsa-release-announcements
description: Draft, approve, and publish Elsa release announcements for Discord, LinkedIn, and X after an Elsa Core, Elsa Studio, Elsa Extensions, or similarly configured Elsa release has completed. Use when Codex needs to turn release notes, GitHub release URLs, package/feed availability, and build results into channel-specific community and social posts; supports direct Discord webhook posting and optional Buffer, Typefully, Zapier, Make, or manual publishing workflows for LinkedIn and X.
---

# Elsa Release Announcements

## Overview

Use this skill after a release has been published and packages are available. The output is an announcement pack with Discord, LinkedIn, and X copy tailored to each channel, plus optional publishing steps.

Keep this skill separate from `elsa-release`: release execution verifies tags, GitHub releases, and packages; announcements communicate the finished release.

## Recommended Publishing Setup

Use a draft-and-approval workflow by default.

- Discord: post directly with a Discord incoming webhook when `DISCORD_RELEASE_WEBHOOK_URL` is configured. If the target is an Announcement Channel, publish/crosspost the webhook message with `--crosspost` and a bot token in `DISCORD_BOT_TOKEN`.
- LinkedIn + X: prefer Buffer or Typefully for queueing/scheduling when accounts are connected.
- Single orchestration flow: use Zapier or Make when the team wants one approval-triggered workflow that can post Discord plus social channels.
- Manual fallback: produce copy-ready Markdown/plain-text drafts when no publishing service is configured.

Current service fit:

- Buffer supports publishing to LinkedIn and X/Twitter and has API support for creating posts across supported channels.
- Typefully supports multi-platform publishing for X/Twitter and LinkedIn through its API.
- Discord webhooks are the simplest reliable path for posting into a Discord channel.
- No service choice should be hard-coded into the release process; credentials, approval, and account ownership vary by team.

## Inputs

Collect or infer:

- Product/repository: Elsa Core, Elsa Studio, Elsa Extensions, or another Elsa project.
- Version and release kind: stable, preview, or RC.
- GitHub release URL.
- Release notes file or GitHub release body.
- Package availability: NuGet, feedz.io, Docker, npm, or other relevant feeds.
- Important callouts: breaking changes, upgrade notes, known issues, migration docs, docs links.
- Desired publish mode: `draft-only`, `discord`, `buffer`, `typefully`, `zapier`, `make`, or `manual`.

Do not publish anything until the user explicitly approves the final text and target channels.

## Workflow

1. Verify release readiness.
   - Confirm the release exists and is public.
   - Confirm package/feed publishing completed for the release kind.
   - Confirm links work: GitHub release, NuGet/feed package search, docs/changelog.

2. Generate an announcement pack.
   - Use `scripts/announcement_pack.py` with release notes as input when available.
   - Treat script output as a scaffold; rewrite it into polished copy.
   - Keep factual claims tied to the release notes and package availability.

3. Adapt by channel.
   - Discord: rich community post with a strong headline, Discord emoji shortcodes, release links near the top, grouped highlights, practical upgrade notes, and a clear testing/feedback ask for previews or RCs.
   - LinkedIn: polished product/developer narrative, stable release value, major improvements, and one clear link.
   - X: one compact post or a short thread. Put the release link in the first post and avoid overloading a single post.

4. Ask for approval.
   - Show the exact message for each channel.
   - State whether posting is direct, queued/scheduled, or manual.
   - Do not include secrets or webhook URLs in chat output.

5. Publish or prepare drafts.
   - Discord direct: use `scripts/post_discord.py` with `DISCORD_RELEASE_WEBHOOK_URL`.
   - Discord Announcement Channels: add `--crosspost` only when `DISCORD_BOT_TOKEN` is configured for a bot that can publish messages in that channel.
   - Buffer/Typefully: use their API only when credentials and account/channel IDs are already configured.
   - Zapier/Make: POST the approved payload to the configured webhook only when the user has provided the endpoint.
   - Manual: save or present the final channel-specific drafts.

6. Verify.
   - For direct posts, confirm the API call succeeded.
   - For queued posts, confirm the returned queue/schedule status or draft URL when available.
   - Record what was posted, where, and when.

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

## Helper Usage

Generate drafts:

```bash
python3 .agents/skills/elsa-release-announcements/scripts/announcement_pack.py \
  --product "Elsa Core" \
  --version 3.7.0 \
  --release-kind stable \
  --release-url https://github.com/elsa-workflows/elsa-core/releases/tag/3.7.0 \
  --notes-file doc/changelogs/3.7.0.md \
  --package-url 'https://www.nuget.org/packages?q=Elsa'
```

Post to Discord after approval:

```bash
DISCORD_RELEASE_WEBHOOK_URL="..." \
python3 .agents/skills/elsa-release-announcements/scripts/post_discord.py \
  --message-file announcements/discord-3.7.0.md
```

Post and publish to a Discord Announcement Channel after approval:

```bash
DISCORD_RELEASE_WEBHOOK_URL="..." \
DISCORD_BOT_TOKEN="..." \
python3 .agents/skills/elsa-release-announcements/scripts/post_discord.py \
  --message-file announcements/discord-3.7.0.md \
  --crosspost
```

For `--crosspost`, the webhook post is sent with `wait=true` so Discord returns the created message ID. The script then calls Discord's crosspost endpoint for that message. The bot must have access to the Announcement Channel; if the bot did not create the message, grant the channel permissions needed to publish another sender's message.

## Guardrails

- Never publish without explicit user approval of the exact channel text.
- Never expose webhook URLs, API tokens, access tokens, or account IDs in final responses.
- Do not claim package availability until verified.
- Do not claim a feature is new unless release notes or commits support it.
- Do not use the same wording blindly across all channels.
- Keep LinkedIn/X posts free of internal build details unless they matter to users.
- If a platform API fails or credentials are missing, fall back to copy-ready drafts.
