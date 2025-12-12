# Use Case 1 - Sign in using a client certificate

## Metadata
| Element     | Description |
|-------------|-------------|
| Use Case ID | UC001       |
| Title       | Sign in using a client certificate |
| Level       | User Goal   |

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
1. The user navigate to the web application.

### Non-functional requirements
- The authentication process should complete within 3 seconds.

### Notes
- Client certificate authentication enhances security by eliminating the need for passwords, reducing the risk of phishing attacks.


<!-- Links to related artifacts can be added here -->
[TR001]: https://github.com/TirsvadWeb/DotNet.Portfolio/blob/main/docs/RiscAnalyze.md#technical-risk
[OR001]: https://github.com/TirsvadWeb/DotNet.Portfolio/blob/main/docs/RiscAnalyze.md#operational-risk
[LCR001]: https://github.com/TirsvadWeb/DotNet.Portfolio/blob/main/docs/RiscAnalyze.md#legal-and-compliance-risk