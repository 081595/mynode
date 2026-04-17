# Capability: Teacher Auth and Session Security

## Purpose
Define teacher authentication, second-factor verification, cookie-based session issuance, and abuse-protection requirements for the appointment workflow.

## Requirements

### Requirement: Passwordless Identification Challenge Initialization
The system SHALL validate teacher identity using `id_no` and `birthday` and initialize a short-lived 2FA challenge when identity is valid.

#### Scenario: Identity matched and challenge is created
- **WHEN** a user submits valid `id_no` and `birthday` that match an active teacher record
- **THEN** the system creates a 6-digit challenge code, sets an expiration timestamp within 5 minutes, and records challenge metadata for audit

#### Scenario: Identity not matched
- **WHEN** a user submits `id_no` and `birthday` that do not match any active teacher record
- **THEN** the system rejects the request without revealing which field failed and records a failed login attempt

### Requirement: Dual 2FA Verification Paths
The system SHALL support both email-code verification and QR-session confirmation as equivalent second-factor completion paths.

#### Scenario: Email verification success
- **WHEN** a user submits the correct, unexpired email verification code for the active challenge
- **THEN** the system marks the challenge verified and transitions the session to authorization issuance

#### Scenario: QR confirmation success
- **WHEN** a mobile user confirms the active QR session within expiration
- **THEN** the system marks the same challenge verified and emits real-time completion events for connected clients

### Requirement: JWT Cookie Session Lifecycle
The system SHALL issue access and refresh tokens only after successful 2FA, store both as HttpOnly cookies, and require server-side refresh-token validation for renewal.

#### Scenario: Token issuance after successful verification
- **WHEN** challenge verification succeeds through either path
- **THEN** the system issues a short-lived access token and a longer-lived refresh token, persists refresh token state, and clears one-time challenge secrets

#### Scenario: Access token refresh
- **WHEN** an authenticated request fails due to expired access token and a valid refresh token is present
- **THEN** the system validates refresh token state in storage, rotates tokens, and returns updated HttpOnly cookies

#### Scenario: Logout invalidates session
- **WHEN** a user invokes logout
- **THEN** the system clears auth cookies and invalidates persisted refresh token state for that user

### Requirement: Verification Abuse Protection
The system SHALL enforce rate limits for challenge initialization and resend operations keyed by identity and client context.

#### Scenario: Resend too frequent
- **WHEN** a user requests challenge resend within the configured cooldown window
- **THEN** the system rejects the resend with a retry-later response and records the throttled event

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
