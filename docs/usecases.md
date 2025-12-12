# Use Cases

This document lists concrete use cases derived from the scenarios. Use cases provide step-by-step interactions and acceptance criteria for implementation and testing.

## Use Case 1 — Sign in using a client certificate

- Identifier: UC-001
- Description: User authenticates to the portfolio application using an installed client certificate.
- Primary Actor: End user
- Preconditions:
  - Server/Reverse proxy is configured to request client certificates.
  - User has a valid client certificate installed.
- Trigger: User navigates to the portfolio app.
- Basic Flow:
  1. Browser performs TLS handshake and the server requests a client certificate.
  2. User selects a certificate when prompted by the browser.
  3. Server validates certificate and maps it to an application identity.
  4. Server returns an authentication token or signals success to the client.
  5. Client stores auth state (in memory or secure storage) and updates UI.
- Postconditions:
  - User is authenticated and can access protected routes.
- Acceptance Criteria:
  - Valid certificate results in authenticated session.
  - Invalid or missing certificate shows clear error.
  - Authentication events are logged for auditing.

## Use Case 2 — View and edit basic profile

- Identifier: UC-002
- Description: Authenticated user views and edits their profile information (name, title, summary).
- Primary Actor: Authenticated user
- Preconditions: User is authenticated.
- Trigger: User navigates to the profile page.
- Basic Flow:
  1. Client requests profile data from the API.
  2. Server returns profile DTO.
  3. Client displays profile fields in an editable form.
  4. User updates fields and submits changes.
  5. Server validates and persists changes.
  6. Client refreshes UI showing updated data.
- Acceptance Criteria:
  - Profile changes are saved and displayed.
  - Unauthorized users cannot edit profiles.

## Use Case 3 — Add a portfolio project (placeholder)

- Identifier: UC-003
- Description: Authenticated user adds a new portfolio project entry.
- Primary Actor: Authenticated user
- Preconditions: User is authenticated.
- Basic Flow:
  1. User opens "Add project" dialog/page.
  2. User fills required fields (title, description, tech stack).
  3. User submits form.
  4. Server validates and stores the project entry.
  5. Client updates project list.
- Acceptance Criteria:
  - Project appears in project list after submission.

## Notes
- Each use case should become one or more tracked tasks or issues in the project board. Ensure acceptance criteria are testable.
