# Quality Criteria for Source Code (OOP) and MSSQL
To ensure high-quality source code in our projects, the following quality criteria must be adhered to for C#.

## Metadata
| **ID** | **Description** | Cross Reference links |
|--------|-----------------|-----------------------|
| QC-001 | Quality Criteria for Source Code (OOP) and MSSQL | [KPI-NFR-001] |

## General Criteria
- **Encoding**: All C# files must use UTF-8 encoding without BOM (Byte Order Mark).
- **Language**: Code must be written in English, including comments, variable names, method names and class names.

## C# Quality Criteria
- **Filename**: Filenames must use PascalCase and match the name of the class or interface defined in the file. For example, if the class is named `Customer`, the file must be named `Customer.cs`.
- **Readability**: Each of the members (`Fields`, `Properties`, `Methods`) should be grouped logically and placed in their own `#region`.
    - `Methods` should be grouped by functionality and include a descriptive comment.

### Naming Conventions
- **Interfaces**: Interface names must start with a capital "I" followed by a descriptive name in PascalCase (e.g., `IUserRepository`).
- **Namespaces**: Namespace names must use PascalCase and reflect the project structure (e.g., `ProjectName.Core.Services`).
- **Types**: Type names (e.g., classes, structs, enums) must use PascalCase and describe the type (e.g., `UserStatus`).
- **Attributes**: Attribute names must use PascalCase and describe the purpose of the attribute (e.g., `Required`, `MaxLength`).
- **Classes**: Class names must use PascalCase and describe the class purpose (e.g., `UserService`).
- **Parameters**: Parameter names must use camelCase and describe the purpose of the parameter (e.g., `userName`).
- **Variables**: Variable names must use camelCase and describe their purpose (e.g., `totalAmount`).
- **Private fields**: Private field names must use camelCase and start with an underscore (e.g., `_firstName`).

### Code Quality
- **Method length**: Methods should not exceed 30 lines of code. If a method becomes too long, it should be split into smaller, more focused methods.
- **Comments**: Comments should be used to explain "why" something is done, not "what" is being done. Code should be as self-explanatory as possible.
- **Error handling**: Use exceptions for error handling instead of return codes. Ensure exceptions are caught and handled at appropriate locations.
- **Indentation**: Use consistent indentation with 4 spaces per level.
- **Use of `var`**: Avoid using `var`; use explicit types instead. (e.g., use `int count = 0;` instead of `var count = 0;`)

## MSSQL Quality Criteria
- **Naming conventions**: Use PascalCase for naming tables, columns, procedures and other database objects (e.g., `CustomerOrders`, `GetCustomerById`).
    - Tables should be named in singular form (e.g., `Customer`, not `Customers`).
    - Database objects should use the prefix `usp` for stored procedures and `vw` for views.
    - All tables must have a primary key.

### Code Quality
- **dbo**: All objects should be contained in the `dbo` schema.
- **Indentation**: Use consistent indentation with 4 spaces per level.
- **Comments**: Use comments to explain complex queries or logic in SQL code.
