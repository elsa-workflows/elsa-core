# Secrets SQLite bridge contract

This is a **synthetic-only review fixture**, not a database migrator. It contains two separate checks: a Python candidate field projection checks the pinned source schema and rejects known unsafe ambiguities; separate C# processes prove synthetic Data Protection-to-AES-GCM decryption, write, and read-back. The C# runner constructs its test metadata directly; it does not consume the Python projection or read/write SQLite rows. Their combined redacted report is not an end-to-end database conversion proof.

## Pinned artifacts and source

`artifacts.json` pins the official NuGet package IDs, versions, package SHA-512 digests, and nuspec source commits. `packages.lock.json` in each runner pins the complete restored dependency graph. The verifier downloads the exact package bytes, checks their digest and nuspec provenance, and runs locked restores. The corresponding code links and field-by-field mapping are in [`mapping.json`](mapping.json). The pins match the official artifact evidence recorded by [#8276](https://github.com/elsa-workflows/elsa-core/issues/8276).

The old SQLite `Secrets` migration has 16 columns: per-version `Id`, shared logical `SecretId`, name/scope/value/description, version/latest/status, duration and absolute expiration, last access, tenant, timestamps, and owner. The candidate maps those fields into one Core aggregate and its serialized versions only when a source semantic exists. In particular, old row IDs remain audit metadata because Core 3.8.4 has no native version ID; this does not preserve old ID-based API behavior. Tenant and owner values fail closed. `ExpiresIn`, `LastAccessedAt`, and per-version `UpdatedAt` have metadata-only destinations, which retain values but do not recreate the old runtime behavior.

## Encryption proof

The Extensions runner uses `DataProtectionEncryptor` with the old purpose `Elsa.Secrets.Encryption`, a temporary synthetic Data Protection key ring, and a fixture-only application name. It writes only the synthetic ciphertexts to a temporary file. A separate Core process uses that synthetic key ring to decrypt both plaintexts, then writes each through Core's `EncryptedSecretStore` and `DefaultSecretValueProtector` with a distinct synthetic `SecretsOptions.EncryptionKey`. It verifies hash equality, version/expiry/status and metadata preservation, and rejection of copied old ciphertext, wrong/missing old key rings, and wrong/missing Core keys. The output contains only SHA-256 hashes and boolean results; it prints no plaintext, key, or ciphertext.

This proves only the controlled fixture handoff. A real deployment must retain and supply the original Data Protection key ring with the original application name and purpose, then configure the Core encryption key. The proof does not establish that a customer has the required old keys, that the new key is deployed safely, or that API clients can continue to address the same secrets.

## Run

From the repository root:

```sh
python3 scripts/integration-program/run_secrets_sqlite_bridge_contract.py \
  --report-out /tmp/secrets-sqlite-bridge-result.json
```

Use `--update-lockfiles` only when intentionally regenerating the pinned package graphs. The Python mapping contract is also covered by `test_secrets_sqlite_bridge_mapping.py` and included in the redacted proof report.

## Recommendation

**No-go for a direct or in-place 3.8.1 → 3.8.4 upgrade.** A reversible bridge remains feasible if a separately reviewed adapter and versioned sidecar preserve the legacy identities and source fields. The synthetic proof establishes crypto compatibility only when the old and new keys are deliberately supplied. Non-empty tenant or owner fields, unknown source migrations/schema, invalid statuses/timestamps, duplicate version numbers, ambiguous latest markers, normalized-name collisions, or unavailable keys must stop a future conversion without changing source data. Current Core is the intended target for a follow-on SQLite proof; it has native tenant support, but this PR does not test migration into its current schema. See [`futureBridgeDesign.md`](futureBridgeDesign.md) for the historical 3.8.4 design and the separately grounded current-target requirements. This PR adds no production DDL, live database, migration runner, real credentials, or converter.
