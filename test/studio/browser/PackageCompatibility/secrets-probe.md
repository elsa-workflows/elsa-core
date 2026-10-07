# Native Secrets reference probe

Candidate 3.10.0 X6 journeys use a separate workflow created through the native workflow dialog. The shared SetOutput authoring path declares the output and inserts the activity. Its Output Value syntax menu opens the Secret picker, whose native inline-create dialog creates an ephemeral synthetic text secret in the encrypted store. The journey selects that item, saves, reloads, and verifies the selected label and exact name/type/scope reference with stable workflow, root, and activity identities. The canonical JSON/React workflow stays separate.

Descriptor inventory and created-secret metadata are direct authenticated read-only API checks, explicitly labeled `backend_readonly_*` in the receipt. They do not observe Studio circuit or browser requests. Endpoint ownership is a separate fixture/assembly gate. This probe does not execute the secret expression or establish runtime secret resolution.

The synthetic payload remains in private process memory and the temporary backend store. The saved workflow must not contain it. Portable receipts allow only hashes of reference metadata and labels, bounded descriptor counts, and ordered boolean stages; they forbid plaintext payloads, payload hashes, raw references, and raw workflow documents. Feature absence, denied permissions, disconnects, and incomplete native stages cannot pass `secrets` or reduce candidate coverage.

`test_paired_package_secrets.py` exercises the portable receipt contract with synthetic fixtures. Those fixtures are not browser proof. Actual host coverage and TypeScript validation require the hosted package proof run.
