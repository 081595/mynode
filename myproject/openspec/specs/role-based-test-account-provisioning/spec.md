# Capability: Role-Based Test Account Provisioning

## Purpose
Enable deterministic provisioning of role-based test accounts for non-production environments so QA and demo workflows can authenticate with stable user/admin identities while preserving environment safety guardrails.

## Requirements

### Requirement: Deterministic Role-Based Test Account Templates
The system SHALL define deterministic non-production test account templates for at least one `user` role and one `admin` role identity that are compatible with the existing authentication input model.

#### Scenario: Seed templates are available for both roles
- **WHEN** the application evaluates test-account provisioning configuration in an eligible environment
- **THEN** it resolves template data for both `user` and `admin` test accounts including identity fields required by login (`id_no`, `birthday`) and authorization role assignment

### Requirement: Idempotent Provisioning Behavior
The system SHALL provision role-based test accounts using idempotent semantics so repeated execution does not create duplicate identities.

#### Scenario: First provisioning run
- **WHEN** provisioning executes and target test identities are not present
- **THEN** the system creates the configured `user` and `admin` test records and reports successful creation

#### Scenario: Repeated provisioning run
- **WHEN** provisioning executes and target test identities already exist
- **THEN** the system updates or preserves the same records without creating duplicates and reports idempotent completion

### Requirement: Environment Safety Guardrails
The system SHALL allow role-based test-account provisioning only when both environment and explicit configuration guardrails permit it.

#### Scenario: Eligible non-production configuration
- **WHEN** runtime environment is non-production and provisioning flag is enabled
- **THEN** the system runs role-based test-account provisioning

#### Scenario: Guardrail not satisfied
- **WHEN** runtime environment or provisioning flag does not satisfy eligibility rules
- **THEN** the system skips provisioning and emits a clear operational message describing the reason

### Requirement: Provisioning Observability
The system SHALL emit operationally useful output for test-account provisioning outcomes.

#### Scenario: Provisioning completes
- **WHEN** provisioning logic finishes
- **THEN** the system logs per-account outcomes (`created`, `updated`, `skipped`) and role mapping summary for user/admin test identities
