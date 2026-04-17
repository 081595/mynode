## 1. Provisioning Configuration and Guardrails

- [x] 1.1 Add configuration options for role-based test account provisioning (enable flag and environment eligibility policy).
- [x] 1.2 Wire startup bootstrap logic to evaluate dual guardrails (non-production + explicit enable flag) before any seed writes.
- [x] 1.3 Add clear startup logging for guardrail outcomes (enabled, skipped, blocked) to improve operator visibility.

## 2. Deterministic Role Templates

- [x] 2.1 Define deterministic template records for at least one `user` and one `admin` test account with stable identity fields.
- [x] 2.2 Implement template-to-persistence mapping so seeded records satisfy existing login requirements (`id_no`, `birthday`) and role assignment.
- [x] 2.3 Reserve and document deterministic key/prefix conventions to reduce collision risk with non-test records.

## 3. Idempotent Seed Execution

- [x] 3.1 Implement idempotent upsert logic for role-based test accounts in the infrastructure bootstrap path.
- [x] 3.2 Ensure repeated provisioning does not create duplicates and reports per-account action results (`created`, `updated`, `skipped`).
- [x] 3.3 Add negative-path handling for persistence failures with actionable log output.

## 4. Validation and Test Coverage

- [x] 4.1 Add integration tests verifying seeded `user` and `admin` accounts can be used with the existing auth flow.
- [x] 4.2 Add tests validating guardrail behavior (disabled flag or ineligible environment skips provisioning).
- [x] 4.3 Add tests confirming idempotent reruns do not duplicate role-based test accounts.

## 5. Operational Documentation

- [x] 5.1 Update rollout/runbook docs with provisioning controls, seeded account contract, and environment safety notes.
- [x] 5.2 Document expected startup logs and troubleshooting steps for skipped/blocked provisioning states.