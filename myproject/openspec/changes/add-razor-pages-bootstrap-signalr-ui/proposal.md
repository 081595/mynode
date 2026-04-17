## Why

The current specification set defines the API, authentication, appointment workflow, and admin governance behavior, but it does not yet define the server-rendered user experience that staff and teachers will actually operate. The technical direction already calls for Razor Pages with Bootstrap 5 and AJAX/SignalR interactions, so this change is needed now to establish the UI contract before implementation starts.

## What Changes

- Add a server-rendered portal shell for shared layout, navigation, status messaging, and role-aware entry points.
- Define Razor Pages login and verification experiences that use Bootstrap 5 components and AJAX/SignalR for challenge polling, resend, QR confirmation, and session transition feedback.
- Define teacher-facing appointment pages for dashboard, document download, and response completion without requiring a client-side SPA.
- Define admin-facing maintenance pages for teacher records and appointment records, including modal or partial-page interactions for CRUD, PDF upload, filtering, and audit-friendly feedback.
- Clarify how server-rendered pages interact with existing API/session rules so the UI remains aligned with cookie-based authentication and realtime notification behavior.

## Capabilities

### New Capabilities
- `portal-shell-and-navigation`: Shared Razor Pages layout, Bootstrap navigation, role-based menu visibility, server time display, and common async feedback conventions.

### Modified Capabilities
- `teacher-auth-and-session-security`: Extend requirements to cover the server-rendered login, email-code, and QR verification flows, including AJAX and SignalR driven transitions.
- `appointment-letter-response-workflow`: Extend requirements to cover teacher-facing Razor Pages for viewing appointment records, downloading PDFs, and submitting response actions.
- `admin-operations-and-data-governance`: Extend requirements to cover admin-facing Razor Pages maintenance screens, asynchronous CRUD interactions, and governance-oriented feedback.

## Impact

- Affected specs: `portal-shell-and-navigation` (new), plus requirement updates to teacher auth, appointment workflow, and admin operations.
- Expected code impact: new Razor Pages, shared layout/components, page models, frontend assets, SignalR client wiring, and integration coverage for SSR flows.
- Dependencies: ASP.NET Core Razor Pages, Bootstrap 5 assets, existing authentication cookies/JWT flow, and existing SignalR infrastructure.