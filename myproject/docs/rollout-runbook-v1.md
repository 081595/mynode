# Teacher Appointment API Rollout Runbook (v1)

## Scope
This checklist covers first production rollout for auth challenge, token/session, appointment workflow, and admin maintenance APIs.

## SSR Portal Entry Points
- Public dashboard: `GET /`
- Authentication pages: `GET /Auth/Login`, `GET /Auth/Verify`
- Teacher workspace: `GET /Teacher/Index`
- Admin workspaces: `GET /Admin/Teachers`, `GET /Admin/Appointments`
- Existing API routes remain available under `/api/*` for integration and automation.

## Pre-Deployment Checklist
- Confirm OpenSpec change `reference-docs-md` is fully complete and verified.
- Run `dotnet build TeacherAppointment.sln` and `dotnet test TeacherAppointment.sln` with no failures.
- Verify environment secrets for `Jwt:SigningKey` and cookie names are production-specific.
- Validate `Sqlite:ConnectionString` points to production storage path with backup policy enabled.
- Confirm reverse proxy preserves client IP headers for audit logging.
- Confirm TLS termination is enabled and HTTP is redirected to HTTPS.
- Confirm static web assets are served for `wwwroot/css/portal.css`, `wwwroot/js/portal-async.js`, and `wwwroot/js/auth-verify.js`.
- Confirm outbound access policy allows frontend CDNs used by Bootstrap 5 and SignalR browser client, or mirror these assets internally.
- Confirm `RoleBasedTestAccountProvisioning:Enabled=false` for production deployments.
- Confirm `RoleBasedTestAccountProvisioning:EligibleEnvironments` excludes production-like environment names.

## Role-Based Test Account Provisioning

Configuration contract:
- `RoleBasedTestAccountProvisioning:Enabled`: explicit toggle for provisioning behavior.
- `RoleBasedTestAccountProvisioning:EligibleEnvironments`: allowed environment names; provisioning runs only when current environment is listed.

Seeded account contract (non-production only):
- `user`: `empl_no=TST-U-0001`, `id_no=A123456789`, `birthday=1985-03-17`
- `admin`: `empl_no=TST-A-0001`, `id_no=B223456789`, `birthday=1978-10-04`

Deterministic key convention:
- Reserved employee number prefix `TST-` is dedicated to role-based test accounts to reduce collision risk with non-test records.
- Application should avoid assigning `TST-*` employee numbers to real identities.

Expected startup logs:
- Enabled path: `Role-based test account provisioning enabled. ...`
- Skipped path: `Role-based test account provisioning skipped: enabled flag is false. ...`
- Blocked path: `Role-based test account provisioning blocked by environment guardrail. ...`
- Per-account outcomes: `action=created|updated|skipped role=user|admin ...`
- Summary: `Role-based test account provisioning summary: user=... admin=...`

Troubleshooting:
1. If provisioning is unexpectedly skipped, verify `Enabled=true` in the effective runtime config.
2. If provisioning is blocked, compare current `ASPNETCORE_ENVIRONMENT` with `EligibleEnvironments`.
3. If no test account can log in, inspect startup logs for per-account outcomes and any provisioning failure error entry.
4. If duplicate-like behavior is suspected, query `teach_appo_empl_base` for `empl_no IN ('TST-U-0001','TST-A-0001')`; expected count is exactly one row per employee number.

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
- Toggle D: Disable Razor Pages portal by removing or feature-flagging `MapRazorPages` route mapping while keeping `/api/*` endpoints live.

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
