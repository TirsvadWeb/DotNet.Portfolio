# Entity-Relationship Diagram

## Metadata
| **ID** | **Description** | Cross Reference links |
|--------|-----------------|-----------------------|
| ERD-001 | Entity-Relationship Diagram | [UC-001-ERD] |

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
[UC-001-DM]: UseCases/UC001/Artifacts.md#
[UC-001-ERD]: https://github.com/TirsvadWeb/DotNet.Portfolio/blob/main/docs/UseCases/UC001/Artifacts.md#er-diagram
