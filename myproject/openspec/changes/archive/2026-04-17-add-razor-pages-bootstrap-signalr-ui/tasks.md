## 1. Host and Shared UI Setup

- [x] 1.1 Enable Razor Pages, static asset delivery, and shared layout composition in the existing TeacherAppointment.Api host.
- [x] 1.2 Add Bootstrap 5 assets and shared frontend conventions for alerts, validation summaries, loading states, and async status regions.
- [x] 1.3 Implement the shared portal shell with system branding, server-time display, masked identity summary, and role-aware navigation visibility.

## 2. Authentication Pages and Session Flow

- [x] 2.1 Build server-rendered login and verification pages for identity entry, email-code verification, and QR challenge selection using existing auth services.
- [x] 2.2 Wire AJAX handlers for resend countdowns, verification form feedback, and verified-session exchange without full page reloads.
- [x] 2.3 Connect the verification UI to the existing auth challenge SignalR hub and provide a degraded fallback path when realtime transport is unavailable.

## 3. Teacher Appointment Workspace

- [x] 3.1 Add teacher-facing Razor Pages for dashboard and appointment record listing with authenticated year-scoped retrieval.
- [x] 3.2 Implement teacher document download and response completion interactions with partial page refresh and session-expiry handling.
- [x] 3.3 Standardize teacher-facing success, error, and re-authentication feedback within the shared shell patterns.

## 4. Admin Maintenance Workspace

- [x] 4.1 Add admin-only Razor Pages for teacher maintenance with filter, create, edit, deactivate, and inspection flows.
- [x] 4.2 Add admin-only Razor Pages for appointment maintenance with record filtering, remark updates, and PDF upload/edit interactions.
- [x] 4.3 Implement AJAX-assisted admin partial refresh behavior for row updates, modal forms, and upload validation feedback while preserving authorization and audit logging.

## 5. Route Protection and Page Conventions

- [x] 5.1 Define unauthorized and expired-session page behavior so protected Razor Pages redirect or message consistently with the existing JWT cookie flow.
- [x] 5.2 Add shared partials or helpers for Bootstrap feedback, form errors, and retry affordances across teacher and admin pages.
- [x] 5.3 Ensure navigation and page availability remain consistent with teacher and admin role boundaries.

## 6. Validation and Delivery Readiness

- [x] 6.1 Add integration tests covering SSR navigation, authentication page flow, QR realtime completion, and degraded fallback behavior.
- [x] 6.2 Add integration tests covering teacher workspace actions, session-expiry redirects, and admin-only maintenance enforcement.
- [x] 6.3 Update operational or rollout documentation to describe the new Razor Pages entry points, asset requirements, and rollback switch.