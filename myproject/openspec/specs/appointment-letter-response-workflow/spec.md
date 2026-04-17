# Capability: Appointment Letter Response Workflow

## Purpose
Define how appointment response records are retrieved, how appointment PDFs are delivered, and when response completion state may transition.

## Requirements

### Requirement: Role-Scoped Appointment Retrieval
The system SHALL return appointment response records according to caller role and identity scope.

#### Scenario: Teacher retrieves own records
- **WHEN** an authenticated teacher requests appointment records
- **THEN** the system returns only records mapped to that teacher's `empl_no` in the requested year scope

#### Scenario: Admin retrieves cross-user records
- **WHEN** an authenticated admin requests appointment records
- **THEN** the system allows filtering and retrieval across all teachers according to query constraints

### Requirement: Appointment PDF Delivery Rules
The system SHALL provide PDF download for appointment documents while applying role-specific counter behavior.

#### Scenario: Teacher download increments counter
- **WHEN** a teacher downloads an appointment PDF from their own record
- **THEN** the system increments `download_count` for that appointment response record

#### Scenario: Admin preview does not increment counter
- **WHEN** an admin accesses appointment PDF content for administrative review
- **THEN** the system serves the file without incrementing the teacher download counter

### Requirement: Response Completion State Transition
The system SHALL update appointment response status after successful verification and response completion.

#### Scenario: Response marked complete on successful verification
- **WHEN** a teacher completes required verification and confirms response action
- **THEN** the system sets `resp_status` to completed and persists update timestamps

#### Scenario: Verification not complete
- **WHEN** a teacher attempts to finalize appointment response without completed verification
- **THEN** the system rejects completion and preserves existing response status

### Requirement: Teacher Appointment Workspace Pages
The system SHALL provide Razor Pages that let authenticated teachers review their appointment records, inspect response status, and start document actions without exposing other teachers' records.

#### Scenario: Teacher opens appointment dashboard
- **WHEN** an authenticated teacher navigates to the appointment workspace page
- **THEN** the system renders only that teacher's appointment records for the selected year with response status, download action, and completion affordances

#### Scenario: Session expired while loading workspace
- **WHEN** a teacher requests the appointment workspace after the authenticated session is no longer valid
- **THEN** the system redirects the browser to the authentication flow and preserves a message indicating that re-authentication is required

### Requirement: Partial Response and Download Interactions
The system SHALL support Bootstrap-based partial interactions for appointment download and response completion while preserving the existing role and state rules.

#### Scenario: Teacher completes response from workspace
- **WHEN** a teacher submits a response completion action from the workspace page for an eligible record
- **THEN** the system updates the affected record status in place and shows the resulting completion timestamp or state message without reloading unrelated rows

#### Scenario: Download action unavailable
- **WHEN** a teacher attempts to download a missing or unauthorized appointment document from the workspace page
- **THEN** the system keeps the user on the current page and renders a non-destructive error message in the page feedback region
