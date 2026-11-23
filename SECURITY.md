# Security policy

## Supported versions

| Version | Supported |
| --- | --- |
| 3.x | Yes |
| 2.x and earlier | No |

Security fixes are released as patch versions of the supported line.

## Reporting a vulnerability

Do not report security vulnerabilities through public GitHub issues, pull requests, discussions, or Discord.

Report them privately through GitHub instead:

1. Open the [Security tab](https://github.com/elsa-workflows/elsa-core/security) of this repository.
2. Select **Report a vulnerability**.
3. Describe the issue, the affected versions and packages, and the steps to reproduce it. A proof of concept helps us confirm it faster.

GitHub's guide to [privately reporting a security vulnerability](https://docs.github.com/en/code-security/security-advisories/guidance-on-reporting-and-writing-information-about-vulnerabilities/privately-reporting-a-security-vulnerability) explains the form.

## What to expect

- We aim to acknowledge new reports promptly and keep you updated in the private advisory.
- We confirm or reject the report and share our assessment there.
- Once a fix is available, we publish the advisory with the fixed versions and credit the reporter unless they ask us not to.

Please give us a reasonable amount of time to release a fix before you disclose the issue publicly.

## Monitoring published dependencies

The [release inventory monitor](.github/workflows/release-sbom-monitor.yml) runs weekly on Tuesday and can also be run manually from `main`. It follows the latest stable public GitHub Release in each supported 3.x minor line, independently for NuGet and each Community image. Previews, source trees and downstream applications have different dependency boundaries. A newer release with missing or invalid evidence produces an error; the monitor also tries an earlier signed release in that line so its coverage continues. Releases published before the November 23, 2026 rollout are listed as predating evidence when their inventories are unavailable. The monitor also checks for backfilled NuGet evidence on historical releases and republished Docker Hub version tags.

For NuGet the monitor checks the signed release and candidate indexes, publisher workflows, product tag, bundle, per-document hashes and public feed bytes. It scans each package's framework inventories and labels browser build-input inventories separately. For Docker it verifies the official version tag's index digest and signed publisher, both platform manifests, product revision and version, and BuildKit's SPDX and provenance records. The 3.10 line ships Server and Server and Studio. Other current lines also include Studio. Applications consuming Elsa should generate and scan their own resolved deployment inventory.

A fresh Grype database (maximum age five days) is required. The report records the scanner version, database archive hash and build date, verified artifact and inventory digests, affected components, advisory IDs, fix information and inventory associations. Each affected component and advisory is counted once per artifact digest, including when it occurs in multiple inventories. Zero matches is a successful scan at the stated database revision. Missing evidence, invalid input, an expired database, and verification or tooling failures do not produce zero-match results. Findings are report-only; an operational error fails the run.

The [monitoring issue](https://github.com/elsa-workflows/elsa-core/issues/9315) retains compact receipts for new outcomes, with full JSON reports on the linked Actions runs for up to 90 days. Maintainers investigate operational errors and repeat the manual run after correction. For a match, check the advisory, affected component version, platform and runtime or build-input context. Record confirmed exposure, affected and fixed releases in a private security advisory, then publish the advisory with the fix. For an exception record the affected digest, advisory, rationale, owner and a review date in the private triage record. Revisit it when the database or release changes. The issue carries scan counts and operational state; use the private route above for new vulnerability reports. Source dependency scanning and Dependabot continue to serve their separate development workflows.
