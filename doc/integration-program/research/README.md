# Integration research catalog

This folder contains the source-grounded research used to assess provider workflows for Elsa. The machine-readable [candidate catalog](candidate-catalog.json) is the detailed record; the [JSON Schema](schema.json) defines its shape, and [validate_catalog.py](validate_catalog.py) checks both structural and cross-record evidence rules.

The catalog contains 67 candidates: 18 receive endpoint-level deep assessments and 49 are discovery entries with a concrete workflow starting point and official API entry page. All 172 source records include a checked date; operation, authentication, trigger, polling, privacy, paid-tier and maintenance claims remain separately qualified as verified, inferred, mixed, hypothesis or unknown. An unknown means the evidence was not established in this pass. A source to an API family or overview does not establish every proposed operation or its required scopes.

The inventory snapshot records the inspected Elsa Core and Extensions revisions in the affected records. It found an existing Extensions Slack module and GitHub module, but no matching OneDrive or Moneybird module in those snapshots. The Slack source scan counted 36 activity classes; all six event-watch trigger classes currently throw `NotImplementedException`, and the Slack token is supplied as a raw activity input. These are source observations, not claims of supported or tested provider behavior. See the source IDs in each record for repository permalinks and exact revisions.

## Workflow hypotheses

Six end-to-end examples demonstrate distinct integration shapes. They are research hypotheses, not commitments, pilots or validated customer demand:

- **WF-01 — Microsoft controlled file intake and archive:** detect a changed OneDrive item, reconcile current state via delta, download bytes for an eligible current file and copy them to an explicitly selected location. Notifications are not a lossless change log; folders/deleted items are excluded, transfer size is bounded, and the temporary download URL must never be logged.
- **WF-02 — Moneybird invoice notification:** receive a Moneybird invoice event, hydrate the invoice, confirm its paid state in an Elsa guard, then send a concise notification to a selected Slack channel. This is a finance-to-notification workflow; it does not infer that arbitrary documents should become sales invoices.
- **WF-03 — GitHub issue to Jira:** route a selected GitHub issue event into an explicitly mapped Jira project, preserving links and duplicate controls.
- **WF-04 — Drive change to Sheets audit row:** record selected Drive metadata into a chosen Sheet. The workflow calls out that Sheets append has no assumed provider idempotency guarantee; an Elsa-owned durable ledger and reconciliation are design requirements.
- **WF-05 — Support context and human escalation:** look up ticket/contact context, pause on uncertain matches, and send a private Slack escalation only after policy checks; public customer replies are excluded.
- **WF-06 — Calendar deadline:** from an Elsa manual request, check a selected calendar and create a workflow deadline event. Pre-search is advisory; it cannot prevent concurrent duplicates or resolve every ambiguous timeout.

Every workflow stage links to a provider operation record. Failure controls and assumptions are recorded on each workflow itself. Any idempotency, reconciliation or retention behavior described there is an Elsa-side design requirement unless a provider guarantee is explicitly cited.

## Reading the recommendation

The four ranked cohorts are conditional validation order, not a calibrated popularity ranking or authorization to build. The rubric assigns 30% to evidenced demand, 25% to complete-workflow coverage, 15% each to feasibility, infrastructure reuse and sustainability. Because demand is not calibrated, every demand score remains null; score sensitivity shows hypothetical demand values of 0, 2.5 and 5. The resulting ranges overlap for every deep candidate, so there is no robust numeric winner.

The shortlist below exposes the proposed first slice, effort range and main readiness gate without requiring a reader to mine the JSON. Effort figures are engineering estimates in the catalog, not measured delivery times. They assume Elsa already has reusable credential/OAuth, webhook and rate-limit infrastructure; this assessment does not implement that infrastructure, and the estimates exclude provider app-review/approval time. Maintenance sponsor is unassigned for every provider.

| Cohort | Provider | Proposed first slice | Effort estimate | Main readiness gate |
| --- | --- | --- | --- | --- |
| 1 | OneDrive | selected-file change, delta reconciliation, content download and upload | L: 15–25 days | test tenant/consent, bounded byte handling, and durable change processing |
| 1 | Moneybird | invoice search/create, signed invoice event, revoke connection | M: 10–18 days | administration owner authorization; sandbox/account and token lifecycle |
| 1 | Slack | credential repair, message post/search, event audit | M: 10–18 days; comprehensive repair can be L | current trigger classes throw; resolve token handling and event lifecycle |
| 2 | GitHub | issue search/comment plus issue event; audit existing module | M: 8–15 days | reconcile existing code, token scopes and webhook repair |
| 2 | Google Drive | selected-file search/update and change watch | L: 15–25 days | `drive.file` selection and push-channel lifecycle |
| 2 | Google Sheets | values read/append after Drive file change | M: 8–14 days | app verification, selected-file flow and write concurrency |
| 2 | Jira | JQL search, issue create/update and event | L: 15–25 days | Forge vs OAuth distribution and webhook registration |
| 3 | SharePoint | list/list-item read/write and list change | L: 15–25 days | dynamic field schema, operation roles and tenant consent |
| 3 | Outlook | narrow mail/calendar actions and message-change watch | L: 18–30 days | public app approval, mail privacy and subscription repair |
| 3 | Teams | delegated chat/channel send and destination picker | M: 8–15 days; triggers raise to L | exact operation permissions and event/RSC behavior |
| 3 | Google Calendar | event search/create; optional event watch | M: 8–15 days; trigger adds 6–10 days | app verification, channel expiry and sync-token recovery |
| 3 | Zendesk | ticket search/update and configured webhook | M: 10–18 days | webhook setup/retries, plan limits and ticket privacy |
| 3 | Notion | shared-page search/create and page-change watch | M: 8–15 days | OAuth review and shared-page setup friction |
| 4 | Stripe | customer read, refund only behind safeguards, payment event | L: 12–22 days | user-owned vs Connect keys and financial-write policy |
| 4 | Gmail | narrow message read/send and change watch | XL: 25–45 days plus security review | approved use case, restricted-scope review and Pub/Sub ownership |
| 4 | HubSpot | contact search/update and contact event | L: 15–25 days plus privacy review | reconcile OAuth scopes with effective user access |
| 4 | Salesforce | record query/update and change-data capture | XL: 25–45 days | client app setup, replay coverage and org-specific schema |
| 4 | Shopify | order search/note and order event | L: 16–26 days | app distribution, protected order data and missed-event recovery |

One public Extensions request for Slack approval workflows is documented as a historical lead: it was created on 2024-06-09 and closed on 2025-10-10. It is one request, not a current sponsor, repeat-volume measure or proof of working Slack triggers. Broad connector directories and task-creation issues are treated as candidate discovery signals, not demand counts. OneDrive, Moneybird and Slack appear in the first validation cohort because the program charter calls for their research and because Slack requires an explicit audit/repair gate; none is an approved implementation commitment.

## Validation and limits

Run `python3 validate_catalog.py` from this directory after installing `jsonschema`. The validator checks the Draft 2020-12 schema, IDs and source references, claim provenance, provider-operation ownership in workflows, minimum catalog/deep counts, score arithmetic and ranking coverage. The isolated validation run also exercised malformed-catalog fixtures for duplicate operations, missing evidence, wrong workflow ownership, weight drift and duplicate ranking membership.

Provider documentation was checked on 2026-09-23 unless an individual source says otherwise. This is a bounded documentation review, not live account testing, tenant consent review, performance testing, provider contact, production access or a security approval. OAuth app verification, tenant/admin consent, data residency, plan limits, sandbox coverage and trigger lifecycle often depend on the app, tenant or plan; the records keep those limits explicit. No provider credentials, private customer records or production accounts were used.
