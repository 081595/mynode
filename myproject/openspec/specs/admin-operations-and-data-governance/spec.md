# Capability: Admin Operations and Data Governance

## Purpose
Define admin-only maintenance capabilities, appointment record administration behavior, audit logging expectations, and sensitive data handling rules for teacher appointment operations.

## Requirements

### Requirement: Admin-Only Data Maintenance
The system SHALL allow only admin-role users to perform create, update, and delete operations on teacher base data and appointment response records.

#### Scenario: Admin updates teacher base profile
- **WHEN** an authenticated admin submits a valid update for teacher base data
- **THEN** the system applies the update, preserves audit fields, and returns the updated record

#### Scenario: Non-admin attempts maintenance operation
- **WHEN** a non-admin user calls a maintenance endpoint
- **THEN** the system denies the request with an authorization failure and records the attempt

### Requirement: Appointment Record Administration
The system SHALL support admin maintenance of appointment records including PDF upload, remarks update, and response status inspection.

#### Scenario: Admin uploads appointment PDF
- **WHEN** an admin uploads a valid appointment PDF for a teacher/year record
- **THEN** the system stores file metadata and binary payload and updates maintenance timestamps

#### Scenario: Admin updates remarks
- **WHEN** an admin updates appointment remarks
- **THEN** the system persists remark changes without altering teacher response completion semantics

### Requirement: Security and Audit Logging
The system SHALL log authentication and verification operations with method, target, result status, and failure reason when applicable.

#### Scenario: Successful verification event is logged
- **WHEN** a user completes verification by email or QR
- **THEN** the system stores an audit entry with verification method and success status

#### Scenario: Failed verification event is logged
- **WHEN** verification fails due to invalid code, expiry, or policy limit
- **THEN** the system stores an audit entry with failure reason and client context metadata

### Requirement: Sensitive Data Handling Guardrails
The system SHALL enforce data handling rules for sensitive identity and binary content fields.

#### Scenario: Sensitive fields in non-privileged views
- **WHEN** non-privileged endpoints return teacher or appointment records
- **THEN** the system excludes or masks sensitive fields not required for that endpoint

#### Scenario: Non-download query on appointment table
- **WHEN** a list or summary query is executed for appointment responses
- **THEN** the system does not include `pdf_content` in the selected columns
