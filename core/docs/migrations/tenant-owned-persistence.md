# Tenant-owned persistence

## MySQL bulk upsert

MySQL bulk upserts match only the key selected by the caller. If another unique constraint conflicts, the database rejects the write. When Elsa owns the transaction it rolls back the batch. When a caller supplies the transaction, that caller owns the rollback. No schema migration is required.
