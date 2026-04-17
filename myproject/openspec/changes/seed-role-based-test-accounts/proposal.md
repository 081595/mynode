## Why

Current environments require manual preparation of login identities before QA or demo flows can be executed, which slows down validation and increases setup mistakes. We need deterministic test accounts for both user and admin roles so teams can start verification immediately after deployment.

## What Changes

- Add a role-based test-account provisioning capability that creates deterministic non-production test accounts for `user` and `admin` roles.
- Define account generation rules (identity data, role assignment, active state, and uniqueness guards) so repeated startup or script execution is idempotent.
- Define environment safety rules so test-account generation is allowed only in approved non-production contexts.
- Define discoverability/output expectations so operators know which seeded accounts are available for login validation.

## Capabilities

### New Capabilities
- `role-based-test-account-provisioning`: Seed and maintain deterministic test accounts for user/admin roles with environment guardrails and idempotent behavior.

### Modified Capabilities

## Impact

- Affected code: startup/bootstrap flow, persistence initialization path, and potential seed utilities in Infrastructure.
- Affected data: teacher identity base records used by auth and admin flows.
- Operational impact: reduced manual setup for QA/demo; requires clear non-production toggle/guard.
- Dependencies: existing SQLite persistence and current auth lookup rules by `id_no` + `birthday`.