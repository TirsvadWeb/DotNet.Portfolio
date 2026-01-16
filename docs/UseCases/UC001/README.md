# Use Case 1 - Sign in using a client certificate
A detailed use case documentation for "Sign in using a client certificate" authentication method in a web application.
It is intended to provide a comprehensive understanding of the use case, including the domain model, user story, use case brief, system sequence diagram, operations contracts, sequence diagram, domain class diagram (DCD), and entity-relationship (ER) diagram.

## Table of Contents
- [User Story](#user-story)
- [Domain Model](#domain-model)
- [Use Case Brief](#use-case-brief)
  - [Primary Actor](#primary-actor)
  - [Stakeholders and Interests](#stakeholders-and-interests)
  - [Preconditions](#preconditions)
  - [Postconditions](#postconditions)
  - [Main Success Scenario](#main-success-scenario)
  - [Non-functional requirements](#non-functional-requirements)
  - [Notes](#notes)
- [Use Case Casual](#use-case-casual)
- [System Sequence Diagram](#system-sequence-diagram)
- [Operations Contracts](#operations-contracts)
- [Sequence Diagram](#sequence-diagram)
- [DCD](#dcd)
- [ER Diagram](#er-diagram)
- [Related artifacts](#related-artifacts)

---

## Metadata

| **ID** | **Description** | Cross Reference links |
|--------|-----------------|-----------------------|
| UC-001 | Sign in using a client certificate | [Domain Model][DM]<br/>[DCD][DCD]<br/>[System Sequence Diagram](#system-sequence-diagram)<br/>[Operations Contracts](#operations-contracts)<br/>[Sequence Diagram](#sequence-diagram)<br/>[ER Diagram](#er-diagram) |

| Element     | Description |
|-------------|-------------|
| Level       | User Goal   |

---

## User Story
As a user, 
I want to sign in to a web application using a client certificate
so that I can securely authenticate without using a password.

---

## Domain Model
This is a initial version of the Domain Model for the use case "Sign in using a client certificate". It captures the key entities, their attributes, and relationships relevant to the authentication process using client certificates.
The maintained Domain Model can be found [here][DM].

### Metadata

| **ID** | **Description** | Cross Reference links |
|--------|-----------------|-----------------------|
| UC-001-DM | Sign in using a client certificate - Domain Model | [User Story](#user-story)<br/>[DCD](#dcd)<br/>[ER Diagram](#er-diagram) |

### Diagram

```mermaid
classDiagram
    class ApplicationUser {
        Email
    }

    class ClientCertificate {
        Subject
        Issuer
        "Valid From"
        "Valid To"
        SerialNumber
    }

    %% Relations
    User "1" o-- "0..1" ClientCertificate : has a
```

---

## Use Case Brief
### Metadata

| **ID** | **Description** | Cross Reference links |
|--------|-----------------|-----------------------|
| UC-001-B | Sign in using a client certificate - Brief | [User Story](#user-story)<br/>[TR-001][TR-001] |

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

---

## Workflow Diagram
```mermaid
flowchart TD
    A[User navigates to web application] --> B[System requests client certificate]
    B --> C{User provides certificate?}
    C -- Yes --> D[System validates certificate]
    D --> E{Certificate valid?}
    E -- Yes --> F[System authenticates user]
    F --> G[Establish authenticated session]
    G --> H[Log authentication event]
    E -- No --> I[Display error message: Invalid certificate]
    C -- No --> J[Display error message: No certificate provided]
```

---

## Use Case Casual

This casual (alternate) use case describes the two main outcomes when a user attempts to authenticate with a client certificate: success or failure.

### Metadata

| **ID** | **Description** | Cross Reference links |
|--------|-----------------|-----------------------|
| UC-001-C | Sign in using a client certificate - Casual | [Use Case Brief](#use-case-brief)<br/>[System Sequence Diagram](#system-sequence-diagram) |

### Primary Flow — Successful Authentication
1. User navigates to the web application.
2. System requests a client certificate from the user's browser/OS.
3. User selects and sends a client certificate.
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

---

## System Sequence Diagram
### Metadata
| **ID** | **Description** | Cross Reference links |
|--------|-----------------|-----------------------|
| UC-001-SSD | Sign in using a client certificate - System Sequence Diagram | [Use Case Brief](#use-case-brief)<br/>[System Sequence Diagram](#system-sequence-diagram) |

### Diagram
```mermaid
sequenceDiagram
  participant User as "User (Actor)"
  participant Browser as "Browser (Client TLS)"
  participant Blazor as "Blazor WASM (UI/Presentation)"
  participant API as "Backend API (Application Layer)"
  participant AuthSvc as "Certificate Validation (Infrastructure)"
  participant Identity as "Identity Store (Domain/Infrastructure)"
  participant Audit as "Audit Log (Infrastructure)"

  User->>Browser: Navigate to application URL
  Browser->>Blazor: Load Blazor WASM app
  Blazor->>API: Request protected resource / Authenticate
  note right of API: TLS layer attempts to locate client certificate
  Browser->>Browser: findDefaultCertificate() : X509Certificate?
  alt default certificate found
    Browser->>API: Present client certificate (TLS handshake)
  else no default certificate
    Browser->>User: promptSelectCertificate() : void
    User->>Browser: selectCertificate(cert: X509Certificate) : void
    Browser->>API: Present client certificate (TLS handshake)
  end
  API->>AuthSvc: Validate certificate (chain, revocation, subject)
  AuthSvc-->>API: Validation result
  API->>Identity: Map certificate to user / fetch claims
  Identity-->>API: User identity and claims
  API->>Audit: Record authentication success/failure
  API-->>Blazor: Return auth result / token
  Blazor-->>Browser: Store token / establish session
```

---

## Operations Contracts
### Metadata
| **ID** | **Description** | Cross Reference links |
|--------|-----------------|-----------------------|
| UC-001-OC | Sign in using a client certificate - Operations Contracts | [System Sequence Diagram](#system-sequence-diagram) |

| Element     | Description |
|-------------|-------------|
| Operation | `AuthenticateUserWithClientCertificate()` |
| Preconditions | - User has a valid client certificate installed in their browser/OS.<br/>- System is configured to accept client certificate authentication. |
| Postconditions | - The user is authenticated and granted access to the web application.<br/>- An audit log entry is created for the authentication event. |

---

## Sequence Diagram
### Metadata
| **ID** | **Description** | Cross Reference links |
|--------|-----------------|-----------------------|
| UC-001-SD | Sign in using a client certificate - Sequence Diagram | [Use Case Brief](#use-case-brief)<br/>[System Sequence Diagram](#system-sequence-diagram) |

### Diagram
```mermaid
sequenceDiagram
  participant Browser as "Browser (Client TLS)"
  participant Blazor as "Blazor WASM (UI/Presentation)"
  participant API as "Backend API (Application Layer)"
  participant AuthSvc as "Certificate Validation (Infrastructure)"
  participant UserRepo as "UserRepository (IUserRepository / EF Core)"
  participant Db as "ApplicationDbContext (EF Core)"
  participant AuditRepo as "AuditRepository (EF Core)"

  Browser->>Blazor: downloadWasm(bundleUrl: string) : void
  Blazor->>API: POST /authenticate(payload: AuthRequest) : HttpResponse<AuthResult>
  Browser->>Browser: findDefaultCertificate() : X509Certificate?
  alt default certificate found
    Browser->>API: tlsProvideCertificate(cert: X509Certificate) : TLSHandshakeResult
  else no default certificate
    Browser->>Browser: promptForCertificateSelection() : void
    Browser->>Browser: selectCertificateFromStore() : X509Certificate
    Browser->>API: tlsProvideCertificate(cert: X509Certificate) : TLSHandshakeResult
  end
  API->>AuthSvc: verifyCertificate(cert: X509Certificate) : ValidationResult
  AuthSvc-->>API: ValidationResult(isValid: bool, reason?: string)
  API->>UserRepo: FindByCertificateSubject(subject: string) : UserDto?
  UserRepo->>Db: QueryUserBySubject(subject: string) : UserEntity?
  Db-->>UserRepo: UserEntity?
  alt user not found
    UserRepo->>Db: CreateUser(entity: UserEntity) : UserEntity
    Db-->>UserRepo: UserEntity(created)
  end
  UserRepo-->>API: UserDto(id: Guid, username: string, roles: string[])
  API->>AuditRepo: RecordAuthEvent(event: AuthEvent) : void
  API-->>Blazor: 200 OK (body: AuthResult { token: string }) : void
  Blazor->>Browser: persistToken(token: string, storage: string) : void
```

---

## DCD
Domain Class Diagram (DCD) for the use case "Sign in using a client certificate".
It captures the key entities, their attributes, and relationships relevant to the authentication process using client certificates.
The solution DCD can be found [here][DCD-001].

### Metadata
| **ID** | **Description** | Cross Reference links |
|--------|-----------------|-----------------------|
| UC*001-DCD | Sign in using a client certificate - Domain Class Diagram | [Domain Model](#domain-model)<br/> |

### UC001 Domain Class Diagram
This diagram shows the application domain entities and how they integrate with the Identity model for the UC-001 use case.

```mermaid
classDiagram
  namespace Domain.Abstracts {
    class IEntityBase {
      <<interface>>
      +guid Id
    }
  }

  namespace Domain.Entities {
    class ApplicationUser {
      +guid Id
      +X509Certificate Certificate
      -- extends Identity user for authentication/authorization --
    }

    class Certificate {
      +string Subject
      +string Issuer
      +DateTime ValidFrom
      +DateTime ValidTo
      +string SerialNumber
    }
  }

  namespace Core.Abstracts {
    class IRepository~T~ {
      <<interface>>
      +GetByIdAsync(Guid id) Task~TEntity~
      +GetAllAsync() Task~List__TEntity~
      +AddAsync(TEntity entity) Task
      +UpdateAsync(TEntity entity) Task
      +DeleteAsync(Guid id) Task
    }

    class IApplicationUserRepository {
      <<interface>>
      +FindByCertificateSubject(subject: string): ApplicationUser?
    }

    class IAuthenticationService {
      <<interface>>
      +AuthenticateUserWithClientCertificate(cert: X509Certificate): AuthResult
    }
  }

  namespace Core.DTOs {
    class AuthResult {
      +string Token
      +DateTime ExpiresAt
    }
  }

  namespace Core.Services {
    class AuthenticationService {
      +AuthenticateUserWithClientCertificate(cert: X509Certificate): AuthResult
    }
  }

  namespace Infrastructure.Repositories {
    class RepositoryBase~T~ {
    }

    class ApplicationUserRepository {
      +FindByCertificateSubject(subject: string): ApplicationUser?
      +CreateUser(entity: ApplicationUser): ApplicationUser
    }
  }

  %% Composition
  ApplicationUser "1" o-- "0..1" Certificate : has a

  %% Associations
  ApplicationUserRepository --* ApplicationUser : manages

  %% Inheritance
  ApplicationUser --|> Domain.Abstracts.IEntityBase : inherits
  ApplicationUserRepository --|> Infrastructure.Repositories.RepositoryBase~ApplicationUser~ : inherits

  %% Identity integration (reference to Identity model in the Identity diagram)
  ApplicationUser --|> Identity.IdentityUser : extends
  Identity.IdentityUser --* Identity.IdentityUserRole : has many

  %% Implementation
  Infrastructure.Repositories.RepositoryBase~T~ --|> Core.Abstracts.IRepository~T~ : implements
  Core.Abstracts.IApplicationUserRepository --|> Core.Abstracts.IRepository~ApplicationUser~ : implements
  Infrastructure.Repositories.ApplicationUserRepository --|> Core.Abstracts.IApplicationUserRepository : implements
  Core.Services.AuthenticationService --|> Core.Abstracts.IAuthenticationService : implements
```

### Diagram Indentity Model
[microsoft-identity-abstractions-for-dotnet]

---

## ER Diagram

### Metadata
| **ID** | **Description** | Cross Reference links |
|--------|-----------------|-----------------------|
| UC-001-ERD | Sign in using a client certificate - ER Diagram | [Domain Model](#domain-model)<br/>[DCD](#dcd) |

### Diagram

```mermaid
erDiagram
    ApplicationUsers {
        GUID Id PK
        GUID ClientCertificateId FK
    }

    ClientCertificates {
        GUID Id PK
        STRING Subject
        STRING Issuer
        DATETIME ValidFrom
        DATETIME ValidTo
        STRING SerialNumber
    }
        
    ApplicationUsers ||--o| ClientCertificates : has_a
```

**Notes**:

`ApplicationUsers` is a extended `IdentityUser` table to store user information.

---

## Glossary

### Related artifacts

| Element     | From | New Element | To | Description |
|-------------|------|-------------|----|-------------|
| User        | UC-001-DM | ApplicationUser | UC001-DCD | Represents the user entity in the system. |

---

<!-- Links to related artifacts -->
[TR-001]: ../../RiskAnalysis.md#technical-risk
[OR-001]: ../../RiskAnalysis.md#operational-risk
[LCR-001]: ../../RiskAnalysis.md#legal-and-compliance-risk
[DCD-001]: ../../DCD.md
[DM-001]: ../../DomainModel.md

[microsoft-identity-abstractions-for-dotnet]: https://github.com/AzureAD/microsoft-identity-abstractions-for-dotnet/blob/main/README.md#concepts "Microsoft Identity Abstractions for .NET"
