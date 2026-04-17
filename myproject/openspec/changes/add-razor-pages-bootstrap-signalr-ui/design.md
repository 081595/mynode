## Context

The current solution already exposes controller-based APIs, JWT cookie authentication, and a SignalR hub from the ASP.NET Core host in the API project. The technical overview, however, expects a server-rendered experience built with Razor Pages and Bootstrap 5 for both teacher and admin users. This change must add that UI layer without introducing a separate SPA, without duplicating domain rules already implemented in the Application and Infrastructure layers, and without weakening the existing cookie-based security model.

## Goals / Non-Goals

**Goals:**
- Add a Razor Pages based portal to the existing ASP.NET Core host so public, teacher, and admin flows can be delivered through SSR.
- Reuse existing authentication, appointment, and admin services so page behavior stays aligned with the API contract and business rules.
- Use Bootstrap 5 for layout, forms, tables, alerts, and modal interactions while keeping custom JavaScript narrowly focused on AJAX and SignalR flows.
- Define where realtime updates are required, especially for QR confirmation and partial refresh flows, so the UI remains responsive without a SPA runtime.
- Preserve accessibility, cookie security, and role-based navigation semantics across teacher and admin experiences.

**Non-Goals:**
- Building a client-side SPA or introducing a JavaScript framework.
- Replacing existing controller endpoints or JWT/session behavior.
- Redesigning the underlying data model or authentication protocol.
- Defining pixel-perfect visual design tokens beyond the Bootstrap-based interaction and layout contract.

## Decisions

### Decision: Host Razor Pages inside the existing API web project
Razor Pages should be added to the existing ASP.NET Core host so the UI and API share one authentication configuration, one deployment unit, and one SignalR endpoint surface.

Alternatives considered:
- Separate MVC or SPA frontend project: rejected because it adds deployment and auth duplication that the current scope does not require.
- Pure API plus static HTML: rejected because it weakens SSR composition and page-model driven validation patterns.

### Decision: Keep domain logic in Application services and repositories, not in page models
Page models should orchestrate UI-specific concerns such as form binding, toast/validation messaging, and page-level redirects, while the existing Application and persistence abstractions remain the source of truth for challenge, session, appointment, and admin behavior.

Alternatives considered:
- Calling controller endpoints from server-side page models over HTTP: rejected because it introduces redundant serialization and obscures domain failures.
- Moving workflow logic into Razor Pages handlers: rejected because it would split behavior across UI and API paths.

### Decision: Use Bootstrap 5 plus targeted JavaScript modules for async slices
Bootstrap 5 should provide the core layout, navigation, modal, table, and feedback components. Small page-specific JavaScript modules should be used only for interactions that benefit from partial updates, such as challenge resend countdowns, inline admin CRUD refreshes, PDF upload progress, and realtime QR completion.

Alternatives considered:
- Full-page postbacks for every interaction: rejected because QR flow and admin maintenance would feel unnecessarily slow and brittle.
- Heavier frontend library: rejected because the requirement explicitly favors SSR with AJAX/SignalR, not a rich client rewrite.

### Decision: Reuse the existing auth SignalR hub for QR completion and add page-level session transition handling
The QR flow should keep the server as the authority for session confirmation, while browser pages subscribe to the existing auth challenge hub and react by updating UI state, exchanging verified sessions, and redirecting to the correct landing page.

Alternatives considered:
- Polling only: rejected because it increases latency and server load for a workflow already modeled as realtime.
- Client-issued redirect logic without server confirmation: rejected because final authorization issuance must remain server-controlled.

### Decision: Introduce a shared portal shell with role-aware navigation and flash-message conventions
The UI should define one shared layout for branding, server time, login state summary, and role-based menu items. Teachers and admins then branch into dedicated pages within the same shell to reduce duplicated markup and preserve navigation consistency.

Alternatives considered:
- Separate disconnected teacher and admin layouts: rejected because it duplicates session status and navigation logic.
- Embedding navigation rules separately in each page: rejected because access visibility would drift over time.

## Risks / Trade-offs

- [Risk] Razor Pages and controller endpoints may drift if they each implement their own validation or authorization checks. → Mitigation: require page models to reuse existing application services and policies, and keep JSON endpoints only for async slices that cannot be handled by standard page handlers.
- [Risk] JWT bearer auth sourced from cookies can be harder to reason about for SSR redirects than traditional cookie auth. → Mitigation: document the page-level unauthorized behavior and add integration coverage for redirect, refresh, and expired-session cases.
- [Risk] Mixed full-page and AJAX interactions can create inconsistent feedback patterns. → Mitigation: standardize Bootstrap alert, validation summary, modal, and toast conventions in the shared layout and partials.
- [Risk] Admin pages may become chatty if every row action reloads full tables. → Mitigation: define partial refresh boundaries for filters, inline edits, and upload outcomes so only affected table regions rerender.
- [Risk] SignalR dependency can degrade gracefully poorly on locked-down networks. → Mitigation: define fallback polling or manual refresh affordances for QR status when realtime transport is unavailable.

## Migration Plan

1. Enable Razor Pages and shared static asset delivery in the existing API host.
2. Add the shared portal shell, public dashboard, and authentication pages first so the SSR entry path is functional.
3. Add teacher appointment pages and admin maintenance pages behind existing authorization boundaries.
4. Wire AJAX and SignalR behavior for realtime or partial-refresh interactions after base page rendering is stable.
5. Expand integration coverage to include SSR navigation, auth flows, teacher actions, admin maintenance, and degraded realtime behavior.

Rollback strategy: disable or remove Razor Page route mapping while leaving the existing API endpoints and SignalR contracts intact, allowing the backend workflow to remain deployable independently.

## Open Questions

- Should unauthenticated root navigation land on a public dashboard page or redirect directly to the login page?
- Should admin maintenance prefer inline modals, dedicated edit pages, or a hybrid pattern for large forms such as PDF upload?
- Is a non-SignalR fallback required for all realtime interactions, or only for QR confirmation?