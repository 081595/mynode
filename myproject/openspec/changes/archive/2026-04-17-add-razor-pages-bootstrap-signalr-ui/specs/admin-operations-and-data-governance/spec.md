## ADDED Requirements

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