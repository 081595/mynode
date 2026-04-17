## Context

The current authentication workflow depends on records in the teacher base table, and manual creation of those records is currently required before QA and demonstration scenarios can be exercised. This makes repeatable validation difficult across fresh environments and increases risk of inconsistent role setup (user vs admin). A provisioning mechanism is needed so non-production environments can reliably bootstrap deterministic test identities without affecting production data governance.

## Goals / Non-Goals

**Goals:**
- Provide deterministic test account generation for both `user` and `admin` roles.
- Ensure provisioning is idempotent so repeated startup or explicit seed execution does not create duplicates.
- Restrict provisioning to approved non-production environments with explicit guardrails.
- Keep seeded identities compatible with existing login flow (`id_no` + `birthday`) and role-based authorization behavior.
- Provide operator-facing visibility of which test accounts were created or updated.

**Non-Goals:**
- Introducing a full user management UI for test accounts.
- Changing production authentication rules or token semantics.
- Replacing existing admin maintenance endpoints for real data operations.
- Seeding large synthetic datasets beyond a minimal role-based account set.

## Decisions

### Decision: Use startup-time idempotent seeding in Infrastructure
Seed logic should run during application bootstrap after schema initialization, using repository/data access paths that can upsert by stable identity keys.

Alternatives considered:
- Manual SQL scripts only: rejected because this reintroduces human setup drift and weakens repeatability.
- Runtime-on-demand API endpoint only: rejected because environment bootstrap should be self-sufficient without privileged interactive steps.

### Decision: Gate test-account provisioning by environment and explicit config flag
Provisioning should require both non-production environment and a dedicated enable flag (for example, development/test profile setting) to avoid accidental seeding in production-like deployments.

Alternatives considered:
- Environment-only gating: rejected because non-production environments may still require clean/realistic datasets without test identities.
- Flag-only gating: rejected because misconfiguration in production would be too risky.

### Decision: Define deterministic account templates per role
Use fixed templates for at least one `user` and one `admin` test account (stable employee number, id number pattern, birthday, display name, and email) so tests and docs can reference known credentials safely.

Alternatives considered:
- Randomized identities each startup: rejected because it breaks reproducibility for QA scripts and integration checks.
- Single shared account for all roles: rejected because authorization-path testing requires role separation.

### Decision: Log provisioning results and expose discoverability in operational docs
Provisioning should emit clear startup logs indicating whether accounts were created, updated, skipped, or blocked by guardrails, and rollout/runbook docs should reference the behavior.

Alternatives considered:
- Silent provisioning: rejected because operators and testers need confidence in seed state.

## Risks / Trade-offs

- [Risk] Accidental provisioning in production due to misconfiguration → Mitigation: dual guard (environment + explicit flag) and startup warning/error when rule mismatch is detected.
- [Risk] Seeded identities collide with real records in shared data stores → Mitigation: reserve deterministic key range/prefix and upsert with explicit ownership markers.
- [Risk] Test accounts become stale as schema evolves → Mitigation: centralize templates and include integration checks validating seeded account usability.
- [Risk] Operational confusion about available test credentials → Mitigation: keep seeded account contract documented and include startup log summaries.

## Migration Plan

1. Add configuration options for enabling/disabling role-based test-account provisioning.
2. Implement deterministic seed templates and idempotent upsert logic in bootstrap path.
3. Add startup logging and guardrail checks for environment eligibility.
4. Add or update integration tests for login and authorization with seeded `user` and `admin` identities.
5. Update operational documentation with the test account seeding behavior and safety notes.

Rollback strategy: disable provisioning flag and redeploy; remove seeded identities manually (or via maintenance path) only if needed for a clean environment.

## Open Questions

- Should seeded account identity values be configurable per environment, or fixed globally for all non-production deployments?
- Should provisioning fail startup on guardrail violations, or continue without seeding and emit a high-severity warning?