## Context

The project has functional and data documentation for a teacher appointment management system, but implementation-ready architecture decisions are not yet captured in OpenSpec artifacts. The target stack is ASP.NET Core Web API with Razor Pages, SQLite, JWT-based session control, and 2FA flows (email code + QR confirmation) with SignalR for real-time synchronization.

Key constraints from docs:
- Authentication is passwordless (IdNo + Birthday identification plus 2FA challenge).
- Access/refresh token flow uses HttpOnly cookies and server-side refresh token validation.
- Teacher and admin roles require strict permission boundaries.
- Appointment responses and download behavior are tracked with explicit status/count updates.
- Sensitive data and verification operations require auditability and abuse controls (rate limiting, operation logs).

## Goals / Non-Goals

**Goals:**
- Define architecture for secure login, MFA challenge handling, and token lifecycle management.
- Define end-to-end appointment response flow from retrieval to response completion.
- Define admin governance behaviors for teacher base data and appointment maintenance.
- Convert docs into testable requirement specs and an implementation task plan.

**Non-Goals:**
- No UI wireframes or detailed frontend styling decisions.
- No provider-specific email/SMS infrastructure design beyond integration boundaries.
- No immediate migration away from SQLite/blob storage in this change.

## Decisions

1. Authentication boundary split into three phases.
- Identification phase validates IdNo + Birthday and creates a short-lived MFA challenge token/code.
- Challenge phase supports two verification channels (email OTP, QR confirmation session).
- Authorization phase issues access/refresh cookies only after successful challenge and performs immediate one-time challenge invalidation.
- Alternative considered: password + OTP hybrid. Rejected because docs require passwordless flow.

2. Session and token policy is cookie-centric with server validation.
- Access token is short-lived and validated per request.
- Refresh token is persisted server-side and validated against DB before renewal.
- Token rotation occurs on refresh; logout invalidates DB token and clears cookies.
- Alternative considered: localStorage tokens. Rejected due to XSS exposure risk.

3. QR challenge uses ephemeral session IDs with SignalR orchestration.
- QR payload maps to one verification session with strict expiration.
- Mobile confirmation sends server-side status update.
- Server emits real-time events to close mobile flow and redirect desktop flow.
- Alternative considered: polling-only UX. Rejected due to delayed UX and higher backend load.

4. Data access patterns enforce role-scoped query paths.
- User role can only retrieve own appointment records by `(yr, empl_no)` mapping.
- Admin role can maintain both base table and response table with CRUD and upload operations.
- Download counter increments only on teacher-side downloads, not admin previews.

5. Security and observability are first-class behaviors.
- Rate limiting applies to identification and challenge regeneration actions.
- Login and verification operations are captured in audit logs (method, target email, status, reason, metadata).
- Sensitive fields are masked/encrypted by policy and never broadly selected with binary payload columns.

## Risks / Trade-offs

- [Risk] Passwordless IdNo + Birthday identification could be brute-forced. → Mitigation: per-identity + per-IP throttling, lockout windows, and audit alerts.
- [Risk] QR session replay or delayed confirmation race. → Mitigation: one-time session IDs, strict expiry checks, and idempotent confirmation endpoint.
- [Risk] SQLite blob reads can degrade performance at scale. → Mitigation: select-column discipline, deferred blob retrieval, future storage abstraction point.
- [Risk] Email delivery latency can block MFA completion. → Mitigation: resend with cooldown, alternate QR challenge path, and clear timeout UX.

## Migration Plan

1. Land API contracts and domain services for authentication/challenge/session first.
2. Implement appointment retrieval/response endpoints with authorization checks.
3. Implement admin maintenance endpoints and upload/record handling.
4. Add rate limiting, audit logging, and security hardening.
5. Validate with scenario-driven tests aligned to specs; then enable `/opsx-apply` execution.

Rollback strategy:
- Feature-gate new auth challenge endpoints and keep routing behind config toggles.
- Revert to prior stable endpoints if token/challenge regressions are detected.

## Open Questions

- Should temporary email updates be persisted automatically after successful verification, or require explicit user confirmation each time?
- What are final numeric thresholds for resend cooldown, failed-attempt lockout, and per-IP quotas?
- Is external blob storage an immediate requirement for the first production rollout, or a post-MVP optimization?