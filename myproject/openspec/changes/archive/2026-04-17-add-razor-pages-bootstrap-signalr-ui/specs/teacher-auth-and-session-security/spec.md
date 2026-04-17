## ADDED Requirements

### Requirement: Server-Rendered Authentication Experience
The system SHALL provide Razor Pages for identity entry, email verification, and QR verification that use Bootstrap 5 form and state components while preserving the existing passwordless challenge rules.

#### Scenario: Identity challenge initiated from Razor Page
- **WHEN** a user submits `id_no` and `birthday` from the login page with a valid identity match
- **THEN** the system transitions the browser to a verification experience that shows challenge expiration, masked email state, available verification paths, and resend timing without exposing raw secrets

#### Scenario: Identity challenge rejected from Razor Page
- **WHEN** a user submits invalid identity data from the login page
- **THEN** the system stays within the server-rendered authentication flow and displays a generic failure message that does not reveal which identity field failed

### Requirement: Realtime QR and Session Transition UX
The system SHALL let the verification pages use AJAX and SignalR to update QR session state, resend status, and post-verification redirects without requiring manual full-page refreshes.

#### Scenario: QR confirmation completes on another device
- **WHEN** the mobile device confirms an active QR session and the desktop verification page is connected to the auth challenge hub
- **THEN** the desktop page updates its state in place, exchanges the verified challenge for an authenticated session, and redirects the user to the correct authenticated landing page

#### Scenario: SignalR unavailable during verification
- **WHEN** realtime transport is unavailable while the verification page is waiting for QR completion
- **THEN** the system shows a degraded-mode message and provides a polling or manual refresh path that still allows the user to complete verification