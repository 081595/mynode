## ADDED Requirements

### Requirement: Shared Server-Rendered Portal Shell
The system SHALL provide a shared Razor Pages layout for public, teacher, and admin pages that renders system branding, current server time, authentication summary, and standard feedback regions.

#### Scenario: Public user opens the portal root
- **WHEN** an unauthenticated visitor requests the portal root page
- **THEN** the system renders the shared layout with system name, current server time, and a visible login entry point without exposing authenticated actions

#### Scenario: Authenticated user opens a portal page
- **WHEN** an authenticated teacher or admin requests a page within the portal
- **THEN** the system renders the shared layout with the masked identity summary, logout action, and page-level feedback containers for validation, alerts, and async status

### Requirement: Role-Aware Navigation Visibility
The system SHALL show navigation items according to the current authentication state and role while keeping login and logout actions mutually exclusive.

#### Scenario: Teacher navigation
- **WHEN** an authenticated teacher loads the shared navigation
- **THEN** the system shows teacher workflow entries such as dashboard and appointment actions, hides admin maintenance entries, and shows logout instead of login

#### Scenario: Admin navigation
- **WHEN** an authenticated admin loads the shared navigation
- **THEN** the system shows both appointment and admin maintenance entries appropriate to the admin role and hides the login action

### Requirement: Standard Async Interaction Conventions
The system SHALL define consistent Bootstrap-based interaction patterns for AJAX submissions, partial refreshes, and realtime updates across the portal.

#### Scenario: AJAX request succeeds
- **WHEN** a page issues an AJAX request for a supported partial interaction
- **THEN** the system updates only the targeted page region and renders a success or neutral status message in the shared feedback pattern without a full page reload

#### Scenario: AJAX or realtime request fails
- **WHEN** an AJAX or SignalR-driven interaction fails or disconnects
- **THEN** the system preserves the current page state, displays a recoverable warning message, and exposes a retry or manual refresh affordance