# Use Case 1 - Sign in using a client certificate
| Element     | Description |
|-------------|-------------|
| Use Case ID | UC001       |
| Title       | Sign in using a client certificate |
| Level       | User Goal   |

## Table of Contents
- [User Story](#user-story)
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
- [Related artifacts](#related-artifacts)

---

## Domain Model
This is a initial version of the Domain Model for the use case "Sign in using a client certificate". It captures the key entities, their attributes, and relationships relevant to the authentication process using client certificates.
The maintained Domain Model can be found [here][DM].

### Metadata
| Element     | Description |
|-------------|-------------|
| ID          | UC001-DM    |
| Title       | Sign in using a client certificate - Domain Model |

### Diagram

```mermaid
classDiagram
    class User {
        Email
        Is Active
    }

    class ClientCertificate {
        Subject
        Issuer
        Valid From
        Valid To
        Serial Number
    }

    User "1" o-- "0..1" ClientCertificate : has a
```

---

## User Story
As a user, 
I want to sign in to a web application using a client certificate
so that I can securely authenticate without using a password.

---

## Use Case Brief
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
| Element     | Description |
|-------------|-------------|
| ID          | UC001-C     |
| Title       | Sign in using a client certificate - Casual |

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
| Element     | Description |
|-------------|-------------|
| ID          | UC001-SSD   |
| Title       | Sign in using a client certificate - System Sequence Diagram |
| Cross reference | [Use Case Brief](#use-case-casual) |

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
| Element     | Description |
|-------------|-------------|
| ID          | UC001-OC    |
| Title       | Sign in using a client certificate - Operations Contracts |
| Cross reference | [UC001-SSD](#system-sequence-diagram) |
| Operation | `AuthenticateUserWithClientCertificate()` |
| Preconditions | - User has a valid client certificate installed in their browser/OS.<br/>- System is configured to accept client certificate authentication. |
| Postconditions | - The user is authenticated and granted access to the web application.<br/>- An audit log entry is created for the authentication event. |

---

## Sequence Diagram
### Metadata
| Element     | Description |
|-------------|-------------|
| ID          | UC001-SD    |
| Title       | Sign in using a client certificate - Sequence Diagram |
| Cross reference | [UC001-SSD](#system-sequence-diagram) |

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
This is an initial version of the Domain Class Diagram (DCD) for the use case "Sign in using a client certificate". It captures the key entities, their attributes, and relationships relevant to the authentication process using client certificates.
The maintained DCD can be found [here][DCD].

### Metadata
| Element     | Description |
|-------------|-------------|
| ID          | UC001-DCD   |
| Title       | Sign in using a client certificate - Domain Class Diagram |

### Diagram
```mermaid
classDiagram

  namespace Domain.Abstracts {
    class IEntityBase {
      <<interface>>
      +guid Id
    }
  }

  namespace  Domain.Entities {

    class UserEntity {
      +guid Id
      +string Email
      +X509Certificate Certificate
      +isActive: bool
    }

    class CertificateEntity {
      +string Subject
      +string Issuer
      +DateTime ValidFrom
      +DateTime ValidTo
      +string SerialNumber
    }
  }

  namespace Core.Abstracts {
    
    %% Repositories
    class IRepository~T~ {
      <<interface>>
      +GetByIdAsync(Guid id) Task~TEntity~
      +GetAllAsync() Task~List__TEntity~
      +AddAsync(TEntity entity) Task
      +UpdateAsync(TEntity entity) Task
      +DeleteAsync(Guid id) Task
    }

    class IUserRepository {
      <<interface>>
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

    class UserRepository {
      +FindByCertificateSubject(subject: string): UserEntity?
      +CreateUser(entity: UserEntity): UserEntity
    }
  }

  namespace Portfolio.Client {
    class Todo {
    }
  }

  namespace Portfolio.Server {
    class Login {

    }
  }


  %% Composition
  UserEntity "1" o-- "0..1" CertificateEntity : has a

  %% Associations
  UserRepository --* UserEntity : manages

  %% Inheritance
  UserEntity --|> IEntityBase : inherits
  UserRepository --|> RepositoryBase~UserEntity~ : inherits

  %% Implementation
  RepositoryBase~T~ --|> IRepository~T~ : implements
  IUserRepository --|> IRepository~UserEntity~ : implements
  UserRepository --|> IUserRepository : implements
  AuthenticationService --|> IAuthenticationService : implements
```

---

## ER Diagram
### Metadata
| Element     | Description |
|-------------|-------------|
| ID          | UC001-ERD   |
| Title       | Sign in using a client certificate - ER Diagram |
### Diagram
```mermaid
erDiagram
    User {
        GUID Id PK
        GUID ClientCertificateId FK
        STRING Email "Unique"
        BOOLEAN IsActive
    }

    ClientCertificate {
        GUID Id PK
        STRING Subject
        STRING Issuer
        DATETIME ValidFrom
        DATETIME ValidTo
        STRING SerialNumber
    }
        
    User ||--o| ClientCertificate : has_a
```

---

<!-- Links to related artifacts -->
[TR001]: https://github.com/TirsvadWeb/DotNet.Portfolio/blob/main/docs/RiscAnalyze.md#technical-risk
[OR001]: https://github.com/TirsvadWeb/DotNet.Portfolio/blob/main/docs/RiscAnalyze.md#operational-risk
[LCR001]: https://github.com/TirsvadWeb/DotNet.Portfolio/blob/main/docs/RiscAnalyze.md#legal-and-compliance-risk
[DCD]: https://github.com/TirsvadWeb/DotNet.Portfolio/blob/main/docs/DCD.md
[DM]: https://github.com/TirsvadWeb/DotNet.Portfolio/blob/main/docs/DomainModel.md
