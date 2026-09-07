# Guest Invitation Contract

## Issuance and delivery

Activity definitions and authorized managers may create multiple invitations. Each invitation has a challenge provider/configuration, allowed actions, and bounded expiry (default: earlier of seven days or `DueAt`). Core creates an unguessable one-time secret, stores only its hash, and gives raw material once to `IUserTaskInvitationDispatcher`. Retry uses a protected transient outbox encrypted through ASP.NET Core Data Protection; successful or expired delivery removes the entry.

## Verification

Anonymous endpoints disclose only generic invitation copy before verification, use rate limiting, and return indistinguishable failures for missing, expired, consumed, or invalid invitations. `IUserTaskInvitationVerifier` performs the configured challenge. Bearer-only verification is permitted only when explicitly enabled by the activity.

The first successful verification atomically:

1. claims the still-open task for a generated guest participant;
2. consumes the winning invitation and revokes its siblings;
3. issues a revocable, task-scoped session through `IUserTaskGuestSessionIssuer`.

The guest session expires at task close or its own host-bounded TTL and grants only configured read/complete capabilities. Guests cannot release, reassign, invite, update, cancel, or manage. A manager recovers abandoned guest work by reassignment or reissue. No raw challenge response, invitation secret, or protected task data is written to audit events.
