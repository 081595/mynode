## 1. Foundation and Project Wiring

- [x] 1.1 Establish solution/module structure for auth, appointment workflow, admin maintenance, and shared persistence abstractions.
- [x] 1.2 Configure JWT settings, cookie policies, and secure defaults (HttpOnly, SameSite, expiry) in environment-specific configuration.
- [x] 1.3 Add infrastructure integrations (email sender abstraction, QR code generator, SignalR hub) with testable interfaces.

## 2. Authentication and Challenge Flow

- [ ] 2.1 Implement identification endpoint for `id_no` + `birthday` with teacher lookup and generic failure behavior.
- [ ] 2.2 Implement challenge initialization (6-digit code, expiry, resend cooldown, audit events) and persistence updates.
- [ ] 2.3 Implement email verification endpoint with expiry/code validation and one-time challenge completion semantics.
- [ ] 2.4 Implement QR session creation/confirmation flow with unique session IDs and expiration checks.
- [ ] 2.5 Implement SignalR notifications for QR confirmation completion (desktop redirect + mobile close event contract).

## 3. Token Lifecycle and Session Management

- [ ] 3.1 Implement post-verification token issuance (access + refresh) and refresh-token persistence.
- [ ] 3.2 Implement refresh endpoint with server-side token validation and rotation logic.
- [ ] 3.3 Implement logout endpoint that clears cookies and invalidates persisted refresh tokens.
- [ ] 3.4 Add middleware/interceptor handling for expired access token and controlled refresh flow.

## 4. Appointment Response Workflow

- [ ] 4.1 Implement teacher-scoped appointment retrieval by `(yr, empl_no)` and admin-scoped filtered retrieval.
- [ ] 4.2 Implement appointment PDF delivery endpoint with role-specific counter behavior (teacher increments, admin does not).
- [ ] 4.3 Implement response completion endpoint that updates `resp_status` only after verified authentication context.
- [ ] 4.4 Ensure response-side state updates include timestamps and preserve data consistency under concurrent requests.

## 5. Admin Maintenance and Governance

- [ ] 5.1 Implement admin-only CRUD endpoints/pages for teacher base data (`teach_appo_empl_base`).
- [ ] 5.2 Implement admin-only maintenance endpoints/pages for appointment records (`teach_appo_resp`) including PDF upload and remarks updates.
- [ ] 5.3 Enforce authorization policy checks on all maintenance operations and record denial events for unauthorized attempts.

## 6. Security Hardening and Auditability

- [ ] 6.1 Implement rate limiting for identification and challenge resend actions by identity and client context.
- [ ] 6.2 Implement LoginLogs/audit event recording for success/failure paths with verification method and failure reason.
- [ ] 6.3 Add sensitive-data handling guards (field masking policy, avoid selecting `pdf_content` in non-download queries).

## 7. Validation and Delivery Readiness

- [ ] 7.1 Add scenario-based integration tests mapped to spec requirements for auth, token, workflow, and admin behaviors.
- [ ] 7.2 Add negative/security tests for invalid identity, expired challenge, resend abuse, and unauthorized admin actions.
- [ ] 7.3 Validate end-to-end QR and email verification flows with expected SignalR events and state transitions.
- [ ] 7.4 Prepare deployment checklist, rollback toggles, and operational runbook notes for first rollout.