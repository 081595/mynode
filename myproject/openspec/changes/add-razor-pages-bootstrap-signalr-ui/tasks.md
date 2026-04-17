## 1. Host and Shared UI Setup

- [ ] 1.1 Enable Razor Pages, static asset delivery, and shared layout composition in the existing TeacherAppointment.Api host.
- [ ] 1.2 Add Bootstrap 5 assets and shared frontend conventions for alerts, validation summaries, loading states, and async status regions.
- [ ] 1.3 Implement the shared portal shell with system branding, server-time display, masked identity summary, and role-aware navigation visibility.

## 2. Authentication Pages and Session Flow

- [ ] 2.1 Build server-rendered login and verification pages for identity entry, email-code verification, and QR challenge selection using existing auth services.
- [ ] 2.2 Wire AJAX handlers for resend countdowns, verification form feedback, and verified-session exchange without full page reloads.
- [ ] 2.3 Connect the verification UI to the existing auth challenge SignalR hub and provide a degraded fallback path when realtime transport is unavailable.

## 3. Teacher Appointment Workspace

- [ ] 3.1 Add teacher-facing Razor Pages for dashboard and appointment record listing with authenticated year-scoped retrieval.
- [ ] 3.2 Implement teacher document download and response completion interactions with partial page refresh and session-expiry handling.
- [ ] 3.3 Standardize teacher-facing success, error, and re-authentication feedback within the shared shell patterns.

## 4. Admin Maintenance Workspace

- [ ] 4.1 Add admin-only Razor Pages for teacher maintenance with filter, create, edit, deactivate, and inspection flows.
- [ ] 4.2 Add admin-only Razor Pages for appointment maintenance with record filtering, remark updates, and PDF upload/edit interactions.
- [ ] 4.3 Implement AJAX-assisted admin partial refresh behavior for row updates, modal forms, and upload validation feedback while preserving authorization and audit logging.

## 5. Route Protection and Page Conventions

- [ ] 5.1 Define unauthorized and expired-session page behavior so protected Razor Pages redirect or message consistently with the existing JWT cookie flow.
- [ ] 5.2 Add shared partials or helpers for Bootstrap feedback, form errors, and retry affordances across teacher and admin pages.
- [ ] 5.3 Ensure navigation and page availability remain consistent with teacher and admin role boundaries.

## 6. Validation and Delivery Readiness

- [ ] 6.1 Add integration tests covering SSR navigation, authentication page flow, QR realtime completion, and degraded fallback behavior.
- [ ] 6.2 Add integration tests covering teacher workspace actions, session-expiry redirects, and admin-only maintenance enforcement.
- [ ] 6.3 Update operational or rollout documentation to describe the new Razor Pages entry points, asset requirements, and rollback switch.