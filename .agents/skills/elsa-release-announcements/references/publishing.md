# Publishing and recovery

Read the release profile at `../../elsa-release/references/elsa-profile.json` for the default destination names, Discord channel ID, and token environment name. Verify those against live account/channel discovery. Store run state outside the Git repositories beside the release checkpoint. Never store tokens, webhook URLs, or private author/account details in the receipts.

## Discord

Prefer the configured direct bot path when the release webhook is absent. The default environment name is `DISCORD_BOT_TOKEN_SUPPORT`; `--bot-token-env` permits an explicit alternative. A configured `DISCORD_RELEASE_WEBHOOK_URL` is also supported. Verify the release channel and bot permissions before sending. Do not reuse the blog channel just because its credentials exist.

Dry-run:

```bash
python3 <announcement-skill>/scripts/post_discord.py \
  --message-file <run>/discord.md --channel-id <verified-channel-ID> \
  --state-file <run>/discord-state.json --crosspost
```

Review the exact payload, then repeat with `--execute` under existing authorization. Webhook mode omits `--channel-id` and reads the webhook environment variable. Use environment variables for secrets, not CLI token flags.

The helper rejects oversize messages, suppresses embeds, disables mentions, records intent before creation and the message ID before crossposting. Repeating the command verifies and reuses the recorded message. After ambiguous bot creation it reconciles by nonce; if it cannot prove the outcome, inspect the channel before any new create. An ambiguous webhook create needs explicit reconciliation because webhook creation has no reliable deduplication key. Never delete state merely to retry.

The helper's final verified message ID/channel/crosspost result can be normalized to a release-train receipt with `id`, `url`, `text`, `status: published`, `error: null`, `crossposted: true`. Derive the guild/channel/message URL from the live channel/message result. The final bot GET must confirm content and the CROSSPOSTED flag.

## LinkedIn and X through Buffer

1. Discover callable Buffer tools, then `get_account`, `list_channels`, and `get_channel`. Use the exact returned channel IDs for the configured organization and account names. Preserve an explicit user override. Check connectivity before reaching this phase during release preflight.
2. Write the curated message to a file. Persist the intent:

   ```bash
   python3 <announcement-skill>/scripts/buffer_receipt.py begin \
     --state-file <run>/linkedin-state.json --platform linkedin \
     --channel-id <discovered-ID> --message-file <run>/linkedin.txt
   ```

3. Only an output action of `publish` authorizes the next already-requested create attempt. Call the connector `create_post` with the exact channel/text, `mode: shareNow`, and `schedulingType: automatic`. Capture the returned ID immediately. Fetch that ID using `get_post`; save the raw tool result or its JSON payload locally.
4. Verify and normalize the connector result:

   ```bash
   python3 <announcement-skill>/scripts/buffer_receipt.py record \
     --state-file <run>/linkedin-state.json \
     --response-file <run>/linkedin-get-post.json \
     --receipt-file <run>/linkedin-receipt.json
   ```

   The helper checks sent state, nonempty public URL, target channel and exact content hash. Record the receipt with `release_train.py record-announcement`. Repeat for `--platform x`.

## Interrupted or uncertain Buffer calls

`begin` on an existing pending intent returns `reconcile`, never `publish`. Inspect a known post ID with `get_post`; if the response was lost, use `list_posts` for the exact channel and creation window, paginate completely, and compare exact text/content hash. One matching sent post can be recorded. A sending/queued post must be watched using its ID. Conflicting/multiple matches require inspection, not another create.

Only after the connector proves that no post was created may the agent run `begin --absence-evidence <run>/absence.json` to allow another create attempt. The helper requires fresh, complete query evidence and rejects pagination gaps, filtered-out statuses, the wrong channel or creation window, and any matching post. Authentication failures, observation timeouts and missing local output are not proof of absence. Preserve discovered post IDs in the state with `note-id` as soon as a create returns, even before sent verification.

A connector outage does not complete a requested announcement. Keep the actual remaining action in the checkpoint and report a genuine access failure if necessary. For a user-requested draft-only task, return the files and do not begin publication state.

The absence-evidence file has this shape; `request` and `response` are the actual connector call inputs and returned payload for each page, in order. `observed_at` is the time the last response was received. Include all pages until `hasNextPage` is false; the query must cover all statuses from at or before the saved intent time through now.

```json
{
  "observed_at": "<ISO-8601 timestamp with timezone>",
  "pages": [{
    "request": {"channelIds": ["<verified channel>"], "createdAt": {"start": "<intent time or earlier>"}},
    "response": {"edges": [], "pageInfo": {"hasNextPage": false, "endCursor": null}}
  }]
}
```

Do not manufacture an empty response or treat a connector error as an empty result. If the tool response shape changes, inspect it and update the adapter before publication.
