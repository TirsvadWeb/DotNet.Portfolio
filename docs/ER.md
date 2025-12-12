# Entity-Relationship Diagram
## Metadata
| Element     | Description |
|-------------|-------------|
| Id          | 000-ERD     |
| Title       | Entity-Relationship Diagram |
| Cross References | [Domain model][DM]<br/>[Use Cases 001 ERD][UC001-ERD]<br/> |

## Diagram
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
<!-- Links -->
[DM]: https://github.com/TirsvadWeb/DotNet.Portfolio/blob/main/docs/ER.md
[UC001-ERD]: https://github.com/TirsvadWeb/DotNet.Portfolio/blob/main/docs/UseCases/UC001/Artifacts.md#er-diagram
