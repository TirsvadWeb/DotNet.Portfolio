# DCD

## Metadata
| Element     | Description |
|-------------|-------------|
| Title       | Data Class Diagram |
| Cross References | [Domain model][DM]<br/>[Use Cases 001][UC001-DCD]<br/> |

## Diagram
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

<!-- Links to other documentation files can be added here using the following syntax: -->
[DM]: https://github.com/TirsvadWeb/DotNet.Portfolio/blob/main/docs/DomainModel.md
[UC001-DCD]: https://github.com/TirsvadWeb/DotNet.Portfolio/blob/main/docs/UseCases/UC001/Artifacts.md#dcd