# Use Case 1 - Sign in using a client certificate
| Element     | Description |
|-------------|-------------|
| Use Case ID | UC001       |
| Title       | Sign in using a client certificate |
| Level       | User Goal   |

## TOC
- [User Story](#user-story)
- [Use Case Breif](#use-case-breif)
  - [Primary Actor](#primary-actor)
  - [Stakeholders and Interests](#stakeholders-and-interests)
  - [Preconditions](#preconditions)
  - [Postconditions](#postconditions)
  - [Main Success Scenario](#main-success-scenario)
  - [Non-functional requirements](#non-functional-requirements)
  - [Notes](#notes)
- [Use Case Casual](#use-case-casual)
- [SD - Sequence Diagram](#sd---sequence-diagram)
- [OC - Operations Contracts](#oc---operations-contracts)
- [Related artifacts](#links-to-related-artifacts)

## User Story
As a user, 
I want to sign in to a web application using a client certificate
so that I can securely authenticate without using a password.

## Use Case Breif
### Metadata
| Element     | Description |
|-------------|-------------|
| ID          | UC001-B     |
| Title       | Sign in using a client certificate - Brief |
| Cross reference | [User Story](#user-story)<br/>Technical Risk password phising [TR001] |

### Primary Actor
- User

### Stakeholders and Interests
- **User:** Wants a secure and convenient way to authenticate.
- **Web Application Owner:** Wants to ensure secure access to the application.
- **Regulatory Authorities:** Wants to ensure that authentication methods comply with data protection regulations.

### Preconditions
- The user has a valid client certificate issued by a trusted certificate authority.
- Optional: The user has a valid self-signed client certificate if the application supports it.
- The web application is configured to accept client certificate authentication.
- The user has installed the client certificate in their browser or operating system.

### Postconditions
- The user is authenticated and granted access to the web application.
- Optional: An audit log entry is created for the authentication event.

### Main Success Scenario
1. User navigates to the web application.
2. System requests a client certificate from the user's browser/OS.
3. User selects and sends a valid client certificate.
4. System validates the certificate chain, revocation status, and matching subject (or mapped account).
5. System authenticates the user and establishes an authenticated session.
6. System logs the authentication event (audit entry).

### Non-functional requirements
- The authentication process should complete within 3 seconds.

### Notes
- Client certificate authentication enhances security by eliminating the need for passwords, reducing the risk of phishing attacks.

## Use Case Casual

This casual (alternate) use case describes the two main outcomes when a user attempts to authenticate with a client certificate: success or failure.

### Metadata
| Element     | Description |
|-------------|-------------|
| ID          | UC001-C     |
| Title       | Sign in using a client certificate - Casual |

### Primary Flow — Successful Authentication
1. User navigates to the web application.
2. System requests a client certificate from the user's browser/OS.
3. User selects and sends a valid client certificate.
4. System validates the certificate chain, revocation status, and matching subject (or mapped account).
5. System authenticates the user and establishes an authenticated session.
6. System logs the authentication event (audit entry).

Postconditions:
- User is granted access to authorized resources.
- An audit log entry is recorded.

### Alternate Flow — Failed Authentication (invalid/no certificate)
4a. System requests a client certificate from the user's browser/OS.
  1. User either does not provide a certificate or provides an invalid/expired/revoked certificate.
  2. System denies authentication and displays an error message with next steps (e.g., instructions to install a certificate or contact support).

Postconditions:
- User is not authenticated.
- Authentication failure is recorded in audit logs and, if configured, triggers alerting for repeated failures.

### Exceptions and Notes
- If certificate validation services (CRL/OCSP/OCSP stapling) are unavailable, system should follow a defined fail-safe policy (e.g., deny access or allow with restricted privileges) and record the condition for investigation.
- For locked or blocked accounts, the system should surface guidance for remediation (account unlock, certificate re-issuance).
- Provide clear user-facing guidance to reduce support calls (how to install certificates, supported browsers/OS).


<!-- Links to related artifacts can be added here -->
[TR001]: https://github.com/TirsvadWeb/DotNet.Portfolio/blob/main/docs/RiscAnalyze.md#technical-risk
[OR001]: https://github.com/TirsvadWeb/DotNet.Portfolio/blob/main/docs/RiscAnalyze.md#operational-risk
[LCR001]: https://github.com/TirsvadWeb/DotNet.Portfolio/blob/main/docs/RiscAnalyze.md#legal-and-compliance-risk