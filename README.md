[![downloads][downloads-shield]][downloads-url] [![Contributors][contributors-shield]][contributors-url] [![Forks][forks-shield]][forks-url] [![Stargazers][stars-shield]][stars-url] [![Issues][issues-shield]][issues-url] [![License][license-shield]][license-url] [![LinkedIn][linkedin-shield]][linkedin-url]

# ![Logo][Logo] Portfolie
A high‑performance portfolio application built with C# and WebAssembly, running entirely in the browser.

## Table of Contents
- [Features](#features)
- [Getting Started](#getting-started)
  - [Prerequisites](#prerequisites)
  - [Clone the Repository](#clone-the-repository)
  - [Run the Application](#run-the-application)
  - [Add Database Migrations](#add-database-migrations)
- [Configuration](#configuration)
- [Using the Portfolio](#using-the-portfolio)
- [Roadmap / Future Ideas](#roadmap--future-ideas)

The project is primarily aimed at software engineers who want to showcase their work to potential employers, but it is flexible enough to be adapted for other professions as well (designers, data specialists, etc.).
The application supports certificate-based login out of the box and is designed so that additional authentication methods can be added in the future if requested.

## ✨ Features

> See Milestones section for details what is planning.

## 🚀 Getting Started

Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)
- [Docker](https://www.docker.com/get-started) (optional, but recommended for easier setup)

### Clone the Repository and Build

1. Clone the repository:
   ```bash
   git clone https://github.com/TirsvadWeb/DotNet.Portfolio.git
   cd DotNet.Portfolio
   ```
  
2. Restore .NET workloads:
   ```bash
   dotnet workload restore
   ```

3. Build the application (optional if you plan to run with Docker):
   ```bash
   dotnet build
   ```

### Run the Application

1. Run with Docker (recommended)

   If the repository contains a `docker-compose.yml` at the repository root you can build and run all services with:
   ```bash
   docker compose up --build
   ```

   Notes:
   - Adjust ports and environment variables to match your configuration.
   - If you prefer to run locally without Docker, continue with the `dotnet run` instructions below.

### Alternatively, run locally without Docker

1. Apply any pending database migrations:
   ```bash
   dotnet ef database update --project src/Portfolio.Infrastructure --startup-project src/Portfolio/Portfolio --context ApplicationDbContext
   ``` 

2. Run the application (when not using Docker):
   ```bash
   dotnet run --project src/Portfolio/Portfolio
   ```

### Add Database Migrations
Create and apply separate migrations for Development and Production.
The migration files are added to the infrastructure project and the
startup project (which provides configuration/connection strings) is the
`src/Portfolio/Portfolio` project. The ASPNETCORE_ENVIRONMENT setting
controls which environment configuration is used when creating/applying migrations.

```powershell
# Windows PowerShell
# Development migration
$Env:ASPNETCORE_ENVIRONMENT = 'Development'
dotnet ef migrations add InitialCreate.Development --project src/Portfolio.Infrastructure --startup-project src/Portfolio/Portfolio --context ApplicationDbContext
dotnet ef database update --project src/Portfolio.Infrastructure --startup-project src/Portfolio/Portfolio --context ApplicationDbContext

# Production migration
$Env:ASPNETCORE_ENVIRONMENT = 'Production'
dotnet ef migrations add InitialCreate.Production --project src/Portfolio.Infrastructure --startup-project src/Portfolio/Portfolio --context ApplicationDbContext
# Apply production migration (ensure startup project's configuration points to the production DB)
dotnet ef database update --project src/Portfolio.Infrastructure --startup-project src/Portfolio/Portfolio --context ApplicationDbContext

# Bash / macOS
# Development migration
ASPNETCORE_ENVIRONMENT=Development dotnet ef migrations add InitialCreate.Development --project src/Portfolio.Infrastructure --startup-project src/Portfolio/Portfolio --context ApplicationDbContext
ASPNETCORE_ENVIRONMENT=Development dotnet ef database update --project src/Portfolio.Infrastructure --startup-project src/Portfolio/Portfolio --context ApplicationDbContext

# Production migration
ASPNETCORE_ENVIRONMENT=Production dotnet ef migrations add InitialCreate.Production --project src/Portfolio.Infrastructure --startup-project src/Portfolio/Portfolio --context ApplicationDbContext
ASPNETCORE_ENVIRONMENT=Production dotnet ef database update --project src/Portfolio.Infrastructure --startup-project src/Portfolio/Portfolio --context ApplicationDbContext
```

<!-- Notes: keep secrets (connection strings, PFX passwords) out of source control; use user-secrets or environment variables -->

---

### Configuration

Configuration for the application is provided in the project appsettings files. See `src/Portfolio/Portfolio/appsettings.json` and `src/Portfolio/Portfolio/appsettings.Development.json` for the exact values used by the project.

Important keys to review or override:

- `ClientCertificateAuth`
  - `Enabled` (bool) — enable/disable automatic client-certificate support
  - `Namespace` (string) — certificate namespace used by the preloaded certificate lookup (e.g. `TirsvadWebCert`, `TirsvadWebCertDevelopment`)
- `ConnectionStrings:DefaultConnection` — the EF Core connection string (defaults to a local SQLite file)
- `DataProtection:KeyPath` — optional path where data-protection keys are persisted (useful for Docker volumes)
- `Kestrel:Certificates:Default` — file-based certificate configuration (Path / Password) when running Kestrel with a PFX

For development you will usually enable `ClientCertificateAuth` and either configure Kestrel to load a PFX or import the development certificate (`TirsvadWebCertDevelopment`) into your user certificate store. Never commit PFX files or passwords to source control; use user secrets, environment variables or your CI secret store for sensitive values.

For the concrete configuration values used in this repository, open the two appsettings files referenced above.

---

## 🧑‍💼 Using the Portfolio
Once running, you can:
- Log in with your certificate
- Customize your profile: name, title, summary
- Add projects: title, description, tech stack, links, screenshots
- Optionally upload or link your CV (PDF or other supported format)
- Share the URL with potential employers as your personal portfolio
- On Windows, use `certmgr.msc` or the MMC Certificates snap-in to copy the certificate to `Trusted Root Certification Authorities` (only for local dev).
- On macOS, add the certificate to Keychain and mark it as trusted.
- `mkcert` automates trust for local development on macOS and Windows.

---

## 🗺️ Roadmap / Future Ideas
- [ ] v0.1 Basic profile with certificate login
  - [ ] User profile
  - [ ] Profile details (name, title, summary)
  - [ ] Certificate login
- [ ] v0.2 Portfolio basics
  - [ ] Portfolio project management (CRUD operations)
  - [ ] Tags / tech stack
  - [ ] Project details (description, links, screenshots)
- [ ] v0.3 Localization
  - [ ] Localization / multi-language support
  - [ ] English
  - [ ] Danish
- [ ] v0.4 Blog and articles
  - [ ] Blog post management (CRUD operations)
  - [ ] Blog post details (title, content, author, date)
  - [ ] Blog post comments
- [ ] v0.5 Add multiple login options and Role-based access control
  - [ ] Add support for OAuth2 / OpenID Connect providers
  - [ ] Add roles (Admin, Editor and Guest)
- [ ] v0.6 Add export and import functionality
  - [ ] Export portfolio data as JSON
  - [ ] Import portfolio data from JSON
- [ ] v1.0 Stable release
  - [ ] Polish UI/UX
  - [ ] Comprehensive testing (unit, integration, e2e)
  - [ ] Documentation and user guides
  - [ ] Custom themes (light/dark)
 
<!-- LINK REFERENCES -->
[contributors-shield]: https://img.shields.io/github/contributors/TirsvadWeb/DotNet.Portfolio?style=for-the-badge
[contributors-url]: https://github.com/TirsvadWeb/DotNet.Portfolio/graphs/contributors
[forks-shield]: https://img.shields.io/github/forks/TirsvadWeb/DotNet.Portfolio?style=for-the-badge
[forks-url]: https://github.com/TirsvadWeb/DotNet.Portfolio/network/members
[stars-shield]: https://img.shields.io/github/stars/TirsvadWeb/DotNet.Portfolio?style=for-the-badge
[stars-url]: https://github.com/TirsvadWeb/DotNet.Portfolio/stargazers
[issues-shield]: https://img.shields.io/github/issues/TirsvadWeb/DotNet.Portfolio?style=for-the-badge
[issues-url]: https://github.com/TirsvadWeb/DotNet.Portfolio/issues
[license-shield]: https://img.shields.io/github/license/TirsvadWeb/DotNet.Portfolio?style=for-the-badge
[license-url]: https://github.com/TirsvadWeb/DotNet.Portfolio/blob/master/LICENSE.txt
[linkedin-shield]: https://img.shields.io/badge/-LinkedIn-black.svg?style=for-the-badge&logo=linkedin&colorB=555
[linkedin-url]: https://www.linkedin.com/in/jens-tirsvad-nielsen-13b795b9/
[logo]: https://raw.githubusercontent.com/TirsvadCLI/Logo/main/images/logo/32x32/logo.png "Logo"

[downloads-shield]: https://img.shields.io/github/downloads/TirsvadWeb/DotNet.Portfolio/total?style=for-the-badge
[downloads-url]: https://github.com/TirsvadWeb/DotNet.Portfolio/releases
Exact steps may vary with your OS and hosting environment. If you want, I can add a `scripts/` folder with PowerShell/mkcert helper scripts for this repository.

<!-- Github Links -->
[githubIssue-url]: https://github.com/TirsvadWeb/DotNet.Portfolio/issues/
[githubProjectTasks-url]: https://github.com/orgs/TirsvadWeb/projects/7
