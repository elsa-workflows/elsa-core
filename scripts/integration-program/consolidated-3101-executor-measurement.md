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
| job timeout | 60 min |
| concurrency | 4 (reviewed constant; **not** a worst-case guarantee) |

The deadline is a single immutable clock from inspector start. It covers
admission, metadata, queue, PUT, GET, and cleanup. This prototype does not
inherit the 3.10.0 `GitHubApi` 120s / 100-request sidecar or mutate a shared
transport deadline.

## Measured vs assumed

Architect formula `465+2S+U` with assumed **S=688**, **U=462** → **2303**.
`465` matches `2×231+3` (two nupkg GET passes plus three extras).

Local measurement of unpublished artifact `13609452227`
(`original.zip` SHA-256 `76a79c05967f8d5f74cb29e551518d1a6d4a4e74e1fcca924044e252ffee7d29`):

| Count | Assumed | Measured |
| --- | ---: | ---: |
| nupkg | 231 | 231 |
| snupkg | 231 | 231 |
| artifacts | 462 | 462 |
| symbol associations S (PDB files in snupkgs) | 688 | 688 |
| unique PDB SHA-256 (bytes) | 462 | **688** |
| unique PDB names | — | 231 (each used 3 times) |
| shared PDB content | — | 0 |
| `465+2S+U` | 2303 | **2529** using U=688 unique bytes |

Assumed U=462 is the artifact count, not unique PDB bytes. Unique Feedz
symbol-store keys were not extracted (no inspector in this offline pass). Unique
PDB bytes (688) is the GET-target upper bound used in the formula. Unique names
(231) would be a lower bound if keys collided across TFMs; preflight rejects
same key / different bytes.

Bytes (uncompressed archive members): nupkg 33 506 468 (max 2 789 617),
snupkg 29 715 284 (max 1 682 082). Latency tails, throttling, and coordinator
overhead on a live Feedz endpoint are **unmeasured**.

## Worst-case time at HTTP cap 25s

Concurrency 4 does not shrink the 25s cap. PUT and its proof GET are sequential
per resource.

- Before-observation GETs (3 extras + index + 231 nupkg + 688 PDB) at cap:
  `ceil(923/4)×25 = 5775s` already **> 1800s** (margin **−3975s**).
- Cold PUT-then-GET pairs (462) at cap: `ceil(462/4)×50 = 5800s` **> 1800s**.
- Sequential cap for the measured HTTP total is tens of thousands of seconds.

A zero-latency local mock can finish inside 1800s. That is **not** proof of fit.

## Conclusion

Safe 1800s fit is **not demonstrated**. Typical latency is unknown because even
real-network publication measurement is not authorized. No GO. No gate waiver.
No dispatch, tag, publish, or merge from this prototype.
