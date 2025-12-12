# Scenarios

This document describes high-level scenarios for the Portfolie application. Scenarios are written to drive design, testing and milestone planning.

## Scenario 1 — Certificate login (primary)

- Goal: Authenticate a user using a client certificate and establish an authenticated session in the Blazor WebAssembly app.
- Actors: End user (owns a client certificate), Browser, Server (API / reverse proxy), Portfolio.Client (Blazor WebAssembly)
- Preconditions:
  - The server or reverse proxy is configured to request or require client certificates.
  - The user has a valid client certificate installed in the browser or OS certificate store.
  - App settings contain certificate validation configuration.
- Main flow:
  1. User navigates to the app landing page.
  2. Server requests a client certificate from the browser (TLS handshake).
  3. Browser prompts the user to select a certificate and sends it to the server.
  4. Server validates the certificate chain and business rules (expiration, issuer, revocation as required).
  5. On success, the server issues an authentication token / session or forwards identity to the Blazor client.
  6. `Portfolio.Client` updates authentication state and shows protected UI.
- Success criteria:
  - User is authenticated and can access protected features.
  - Authentication state persists according to configured session policy.
- Alternate flows / errors:
  - No certificate available: show friendly guidance about installing a certificate.
  - Certificate invalid or revoked: show error with remediation steps.
  - Network/TLS error: retry guidance and logging for diagnostics.

## Scenario 2 — App shell and offline-resilient UI (WebAssembly)

- Goal: Provide a minimal app shell that loads quickly in the browser and gracefully handles connectivity loss.
- Actors: End user, Browser, Portfolio.Client (Blazor WebAssembly)
- Preconditions: Static client files served; optional backend available.
- Main flow:
  1. User loads the app URL.
  2. `Portfolio.Client` shell downloads and renders navigation and basic layout.
  3. The app shows cached or placeholder content if backend is unavailable.
- Success criteria:
  - Shell renders quickly and navigation works offline for static pages.
  - Clear messaging when server-side features are unavailable.

## Notes
- These scenarios map to the "Login with certificate" milestone in the project roadmap. Each scenario should be broken down into smaller tasks or issues (implementation, tests, documentation).
- Keep authentication logic separated: certificate validation on the server, identity propagation to the client, and local auth state handling in `Portfolio.Client`.

---

Generated for the docs folder. Feel free to split scenarios further into per-page subdocuments as implementation progresses.
