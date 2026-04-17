## ADDED Requirements

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