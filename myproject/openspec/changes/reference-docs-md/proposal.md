## Why

The project currently has rich domain and data documentation in `docs/*.md` but lacks executable OpenSpec artifacts that can drive implementation. We need a structured change proposal that translates those documents into concrete capabilities, requirements, and tasks so development can start with traceable scope.

## What Changes

- Establish a documentation-derived OpenSpec change that maps the teacher appointment system vision into implementable capabilities.
- Define capability boundaries for authentication/security, appointment response workflow, and administration operations.
- Capture explicit data and security constraints (2FA, JWT cookie flow, audit logging, rate limiting, role-based access control) as spec-level requirements.
- Produce implementation tasks that prioritize core flows: identity verification, MFA challenge, token lifecycle, appointment PDF delivery/response, and admin maintenance tools.

## Capabilities

### New Capabilities
- `teacher-auth-and-session-security`: Passwordless identity verification with 2FA challenge paths (email and QR), JWT access/refresh lifecycle, secure cookie handling, and session completion/cleanup behaviors.
- `appointment-letter-response-workflow`: Teacher-facing appointment retrieval and response status lifecycle, including PDF download behavior and response completion semantics.
- `admin-operations-and-data-governance`: Admin-only maintenance for teacher base data and appointment records, plus auditability, indexing guidance, and sensitive-data protection rules.

### Modified Capabilities
- None.

## Impact

- Affected systems: ASP.NET Core Web API, Razor Pages admin UI, JWT/auth middleware, SignalR real-time flow, SQLite persistence.
- Affected data model: `teach_appo_empl_base`, `teach_appo_resp`, and `LoginLogs` with documented constraints and indexes.
- External/runtime dependencies: JWT token stack, QR generation, secure hashing, and email delivery integrations.
- Delivery impact: creates the baseline OpenSpec artifacts needed for implementation planning and later `/opsx-apply` execution.