# DCD

## Metadata
| **ID** | **Description** | Cross Reference links |
|--------|-----------------|-----------------------|
| DCD-001 | Data Class Diagram | [Domain model][DM]<br/>[Use Cases 001][UC001-DCD]<br/>  |

## Diagram

### User Authentication with Client Certificate - Data Class Diagram

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

### Microsoft Identifier - Data Class Diagram

[microsoft-identity-abstractions-for-dotnet]

<!-- Links to other documentation files can be added here using the following syntax: -->
[DM]: https://github.com/TirsvadWeb/DotNet.Portfolio/blob/main/docs/DomainModel.md
[UC001-DCD]: https://github.com/TirsvadWeb/DotNet.Portfolio/blob/main/docs/UseCases/UC001/Artifacts.md#dcd

[microsoft-identity-abstractions-for-dotnet]: https://github.com/AzureAD/microsoft-identity-abstractions-for-dotnet/blob/main/README.md#concepts "Microsoft Identity Abstractions for .NET"
