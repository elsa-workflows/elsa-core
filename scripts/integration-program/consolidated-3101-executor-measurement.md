# 3.10.1 executor prototype — non-publication measurement

This is **not** a fit claim and **not** release GO. Network PUT is not
authorized. The 3.10.0 executor, pins, and operational policy are unchanged.

## Constants (reviewed, not waived)

| Name | Value |
| --- | --- |
| `MAX_SECONDS` | 1800 |
| `MAX_REQUESTS` | 5000 |
| HTTP cap | 25s |
| readback reserve | 900s |
| cleanup margin | 30s |
| job timeout | 60 min |
| concurrency | 4 (fixed semaphore; **not** a worst-case guarantee) |

`ceil(work/4)` is invalid once overlap locks and sequential multi-PDB proofs
exist. Concurrency 4 is an exchange semaphore, not a proof that a DAG fits
1800s.

## Reconciled DAG

Architect formula `465+2S+U` with **S=688 associations** and **U=462 archive
PUTs** (231 nupkg + 231 snupkg) → **2303**. `465` is `2×231+3` (two nupkg GET
passes plus three extras).

**U is not unique PDB bytes.** Treating U as 688 unique PDB SHA-256 values
produced **2529**, which is withdrawn and is not a reconciled HTTP count.

Separated operations:

| Kind | Count |
| --- | ---: |
| nupkg PUT | 231 |
| snupkg PUT | 231 |
| archive PUT (U) | 462 |
| associations (S) | 688 |
| unique symbol keys | not extracted without inspector (unique PDB SHA 688, unique names 231) |
| nupkg GET passes | 2 |
| metadata/index extras in 465 | 3 (GitHub paging unmeasured) |
| overlap rereads | conditional |
| proof GET | after PUT unless `definitely_not_sent` |

Local fixture (artifact `13609452227`, `original.zip` SHA-256
`76a79c05967f8d5f74cb29e551518d1a6d4a4e74e1fcca924044e252ffee7d29`): 231/231
packages, 688 PDB files, 0 shared PDB content. Latency, throttling, and live
overhead are unmeasured.

## 5800s is a pair counterexample, not a cold upper bound

`ceil(462/4)×50 = 5800s` shows that **462 independent PUT→GET pairs** at HTTP
cap 25s and concurrency 4 already exceed 1800s. That figure **omits**
observation GETs, overlap serialization, sequential multi-PDB proofs, metadata
paging, queue wait, cleanup margin, and receipt I/O. The full cold-publish
upper bound is therefore **higher and uncomputed**.

A zero-latency local mock can finish inside 1800s. That is **not** proof of fit.

## Conclusion

Safe 1800s fit is **not demonstrated**. Typical latency is unknown because even
real-network publication measurement is not authorized. No GO. No gate waiver.
No dispatch, tag, publish, or merge from this prototype. Writes still require
reviewed invariants, this DAG, QA, and a FINAL hash owner signoff.
