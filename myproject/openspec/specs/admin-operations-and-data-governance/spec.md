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

### Requirement: Admin Maintenance Workspace Pages
The system SHALL provide admin-only Razor Pages for teacher and appointment maintenance that expose filtering, record inspection, and editing workflows within the shared portal shell.

#### Scenario: Admin opens teacher maintenance page
- **WHEN** an authenticated admin navigates to the teacher maintenance workspace
- **THEN** the system renders filter controls, paged or tabular teacher results, and actions for create, edit, deactivate, and record inspection

#### Scenario: Non-admin requests maintenance page
- **WHEN** a non-admin user requests an admin maintenance page
- **THEN** the system denies access, does not render maintenance content, and records the denied attempt according to governance rules

### Requirement: Async Admin Editing and Upload Feedback
The system SHALL support AJAX-assisted admin operations for inline updates, modal forms, and PDF upload outcomes while preserving auditability and authorization enforcement.

#### Scenario: Admin updates appointment remark from maintenance UI
- **WHEN** an admin submits a remark update through an asynchronous maintenance interaction
- **THEN** the system refreshes the affected row or detail panel with the persisted values and shows operation feedback without reloading the full workspace

#### Scenario: Admin PDF upload validation fails
- **WHEN** an admin submits an invalid or rejected PDF upload from the maintenance UI
- **THEN** the system keeps the admin in the current editing context, shows validation feedback next to the upload interaction, and does not silently discard the failure
