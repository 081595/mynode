# Teacher Appointment API Rollout Runbook (v1)

## Scope
This checklist covers first production rollout for auth challenge, token/session, appointment workflow, and admin maintenance APIs.

## Pre-Deployment Checklist
- Confirm OpenSpec change `reference-docs-md` is fully complete and verified.
- Run `dotnet build TeacherAppointment.sln` and `dotnet test TeacherAppointment.sln` with no failures.
- Verify environment secrets for `Jwt:SigningKey` and cookie names are production-specific.
- Validate `Sqlite:ConnectionString` points to production storage path with backup policy enabled.
- Confirm reverse proxy preserves client IP headers for audit logging.
- Confirm TLS termination is enabled and HTTP is redirected to HTTPS.

## Deployment Steps
1. Create a database backup snapshot before rollout.
2. Deploy application binaries and configuration.
3. Restart service instance and verify health endpoint `GET /api/health`.
4. Execute smoke checks:
   - identify + verify-email + session exchange
   - refresh + logout
   - teacher appointment list and PDF download
   - admin teacher list and appointment maintenance endpoint access
5. Verify login logs are being written for success and failure paths.

## Rollback Toggles and Actions
- Toggle A: Disable new auth challenge flow by routing traffic away from `/api/auth/*` endpoints in gateway rules.
- Toggle B: Disable admin maintenance writes by denying `PUT/PATCH/DELETE /api/admin/*` at ingress.
- Toggle C: Disable refresh flow by blocking `/api/auth/sessions/refresh` while preserving logout.

Rollback procedure:
1. Stop incoming traffic to new routes using toggles above.
2. Redeploy previous stable release.
3. Restore database snapshot only if data corruption is detected.
4. Run health and auth smoke checks against rolled-back version.

## Post-Deployment Monitoring (First 24 Hours)
- 4xx/5xx rate on `/api/auth/*` and `/api/admin/*`.
- Rate-limit event frequency in `login_logs` (`rate_limited_*`, `resend_cooldown`).
- Token refresh failure rate (`session.refresh` with `refresh_token_invalid`).
- Average latency for identify, verify-email, and QR confirm endpoints.

## Incident Response Notes
- If resend abuse spikes: tighten `RateLimiting:ChallengeResend` limits and redeploy config.
- If invalid identity spikes: tighten `RateLimiting:Identify` and monitor source IP ranges.
- If SignalR QR completion fails: validate hub connectivity and fallback to email verification flow.
