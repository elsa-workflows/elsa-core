---
name: elsa-release-announcements
description: Draft and publish verified Elsa release announcements on Discord, LinkedIn, and X, including safe recovery and publication receipts. Use after Elsa releases or when explicitly asked to announce a release.
---

# Elsa Release Announcements

Use after the requested releases and packages are verified. Read [publishing and recovery](references/publishing.md), and use [channel style](references/style.md) while writing the copy.

## Authorization

An end-to-end `$elsa-release` request includes the standard announcements unless the user excludes them. “Announce/publish this release” also authorizes drafting and posting factual channel-specific copy to the configured release channels. Review the concrete text and targets as part of execution; do not ask for repeated approval already covered by the request. A request to draft, assess, or plan remains draft-only. An unexpected account/channel or materially expanded claim requires resolving that choice first.

## Workflow

1. Verify public releases, exact versions and required package/feed results. Use the release train's manifests/reports and current GitHub state. Never announce availability based only on an upload job or an open PR.
2. Generate a scaffold with `scripts/announcement_pack.py` if useful, then curate it using the release notes. Stable copy includes useful upgrade notes; RC/preview copy explicitly asks for testing and does not imply production stability. Claim only features supported by the final release range.
3. Resolve live configured channels. Default Elsa destinations are the Discord releases Announcement Channel, Elsa Workflows on LinkedIn, and sfmskywalker on X, through the configured Buffer organization Valence Works. Read the release profile or an explicit override. Discover current channel IDs and access; never guess private connector IDs or expose credentials.
4. Persist publication intent before sending. Use the Discord helper for bot/webhook posting and `buffer_receipt.py` around connector calls. Publish now for “announce when done”; schedule only when requested. Do not silently replace a required publication with drafts or a queued post.
5. Verify actual sent content, target, public URL, and Discord crossposting. Save the receipts and record them in the release checkpoint. A known message ID is reused on retry; an uncertain create is reconciled before any further send.

Keep this skill responsible for communication; the release skill owns version, source, build and package gates. No special image, blog article, extra channel, or mass mention is required for a release announcement unless requested.
