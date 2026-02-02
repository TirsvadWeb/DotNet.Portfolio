# Tirsvad Web Portfolio – AI Agent Instructions

## Project Overview
Portfolio website for TirsvadWeb, showcasing web projects, skills, and services. 
Built with modern web technologies for an engaging user experience.

## General Guidelines
1. Be polite and respectful in all interactions.
2. Provide clear and concise responses.
3. Ask clarifying questions if the user's request is ambiguous.
4. Use examples to illustrate complex concepts when necessary.
## Task-Specific Instructions
- For coding-related queries:
    - Provide code snippets in the requested programming language.
    - Explain the code and its functionality.
    - For general knowledge questions:
        - Provide accurate and up-to-date information.
        - Cite sources when applicable.

## Architecture & Structure

### Core Dependencies

#### Framework
- .NET 10 (or later)
- Blazor WebAssembly

#### Libraries
- MediatR (CQRS)
- Entity Framework Core (data access)
- Microsoft Identity (auth)
- TirsvadCLI.Portfolio (domain/business logic)
- FluentValidation (validation)
- AutoMapper (object mapping)

#### Tooling & Testing
- Swashbuckle/Swagger (API docs)
- xUnit (testing)

### Key Projects
- **Domain**: Entities & business rules
- **Core**: Application logic, CQRS, services
- **Infrastructure**: EF Core, external services, email, file access
- **Tests**: Unit & integration tests

## C# Conventions
- Microsoft naming conventions
- `PascalCase` for types/methods
- `camelCase` for parameters/private fields
- Prefix interfaces with `I` (e.g., `IRepository`)
- Suffix async methods with `Async` (e.g., `GetByIdAsync`)
- Prefix private fields with `_` (e.g., `_repository`)
- Use `{}` for blocks except single-line `return`/`throw`
- Keep single-line blocks on one line (e.g., `if (x) return y;`)
- Prefer primary constructors for required dependencies
- Never use primary constructor parameters directly—assign to private fields

### Class Naming & Responsibility
- Use descriptive names; follow Single Responsibility Principle
- **Helper**: Static, pure functions, no state, very small
- **Manager**: Methods for a context, no state outside injected classes, business logic not in domain model
- **Mapper**: Transforms one object to another
- **Service**: Performs operations with side effects or orchestration, may have state
- **Handler**: Responds to requests, executes business logic via other classes

## Key Dependencies & Patterns
- **TirsvadCLI.Portfolio**: Business logic and domain models
- **Blazor WebAssembly**: Client-side web UI framework
- **MediatR**: CQRS pattern implementation for request/response handling
- **Identity**: Authentication and authorization. Use API TirsvadCLI.Portfolio to manage users/roles.

### Central Package Management
- All package versions in `Directory.Packages.props`
- Use `<PackageReference Include="..." />` without Version attribute

## Quality Criteria

### File Naming Conventions
- Markdown: `.md` extension, lowercase, hyphens for spaces, no special characters.
- Mermaid diagrams: in markdown files.
- C# files: match class name, `.cs` extension.

### Folder Structure
```
project-root/
├── docs/                       # Project documentation, quality criteria, use cases, diagrams, and guides
|   ├── architecture-decisions/ # Architecture Decision Records (ADRs)
│   ├── preliminary-analysis/   # Preliminary analysis artifacts (milestones, FURPS+, KPIs, BMC, BPMN)
│   └── UseCases/               # Use case documentation and related artifacts
│       ├── UC-001/             # Use case specific folder
│       └── ...                 # Other use case folders
├── src/                        # Source code for all projects
├── tests/                      # Automated tests for all projects
├── .github/                    # GitHub workflows and configuration files
├── README.md                   # Project overview and getting started guide
├── LICENSE                     # License information
└── ...                         # Other root-level files (e.g., .gitignore, solution files)
```

### Preliminary analysis artifacts
- Milestones: Unique ID, versioned, clear title/description, related KPIs/FURPS+ IDs.
- Furps+: Unique ID, categorized requirements, clear descriptions.
  - ID format: FURPS-X-XXX (X=category initial, XXX=sequential number).
- KPIs: Unique ID, measurable metrics, clear definitions.
  - ID format: KPI-X-XXX (X=category initial, XXX=sequential number).
  - Metrics must be quantifiable and relevant to project goals.
- BMC: Unique ID, versioned, clear sections (key partners, activities, value propositions, customer relationships, channels, customer segments, cost structure, revenue streams).

### Artifact Documentation
- Artifacts must be versioned, stored centrally, and have unique identifiers.
- Artifacts must be written in markdown format where applicable.
- Maintain clear documentation, usage instructions, and dependencies for all artifacts.
- Usecases: Unique ID (UC-XXX), clear title, description, scope, actors, cross-references, reviewed/approved, assumptions documented.
- High Level Design (HLD): Domain Models (unique ID, clear attributes/relationships, versioned, cross-referenced), ERD (unique ID, cross-reference to DM), Use Case Brief (unique ID, overview, only success scenario), Use Case Casual (unique ID, flow of events, alternatives/exceptions), SSD (unique ID), Operation Contract (unique ID, cross-reference to SSD, define inputs/outputs/pre/postconditions).
- Low Level Design (LLD): Sequence Diagram (unique ID, cross-reference to OC/SSD, accurate interactions), Design Class Diagram (unique ID, cross-reference to DM/OC, accurate design).

### Compliance
- Adhere to legal/regulatory requirements.
- Maintain documentation for audits and reviews.
