# BNU Student Portal

A modular ASP.NET Core 10 student portal backend built around clean separation of concerns, feature-oriented services, and production-focused infrastructure. The solution combines domain-driven modeling, service abstractions, PostgreSQL persistence, JWT authentication, Redis caching, Cloudinary media management, Docker containerization, and GitHub Actions-based CI/CD to support a real academic portal workflow.

---

## Overview

This project is designed as a university portal API for managing authentication, grades, and administrative workflows through a layered architecture. The solution is split into independent projects so each concern stays isolated: domain rules live in the Domain project, contracts and use cases live in Services, infrastructure concerns live in Persistence and Services-Implementation, transport concerns live in Presentation, and startup wiring lives in the Web host.[cite:8][cite:20]

At the HTTP layer, controllers are loaded from the Presentation assembly using `AddApplicationPart`, while the Web project stays focused on composition root responsibilities such as DI registration, authentication setup, middleware, database migration, and startup seeding.[cite:20] The startup pipeline also enables OpenAPI and Scalar in development, applies a custom exception middleware, runs migrations automatically, seeds identity data, and then enables authentication, authorization, and controllers.[cite:20]

---

## Architecture

The solution uses a layered architecture with a clear dependency flow from outer layers toward inner abstractions. The root contains separate projects for Domain, Persistence, Presentation, Services, Services-Implementation, Shared-Library, and Web, plus Docker assets and the solution file.[cite:8]

### Project structure

```text
BNU-Student-Portal/
├── BNU-Student-Portal-Domain/
├── BNU-Student-Portal-Persistence/
├── BNU-Student-Portal-Presentation/
├── BNU-Student-Portal-Services/
├── BNU-Student-Portal-Services-Implementation/
├── BNU-Student-Portal-Shared-Library/
├── BNU-Student-Portal-Web/
├── Dockerfile
├── docker-compose.yml
└── BNU-Student-Portal.slnx
```

### Layer responsibilities

#### 1. Domain

The Domain project contains the core business model and foundational contracts of the system. It targets .NET 10 and references `Microsoft.AspNetCore.Identity.EntityFrameworkCore`, which strongly suggests the domain model is integrated with ASP.NET Core Identity for user and role management.[cite:19]

This layer should remain free from transport details and database-specific logic. In this solution, it acts as the center of the system and is consumed by higher-level services and infrastructure projects rather than depending on them.[cite:14][cite:17][cite:19]

#### 2. Services

The Services project is the application layer in practice. It contains the use-case contracts, cross-cutting application logic, feature handlers, validation logic, mapping concerns, authentication-related orchestration, email workflows, PDF generation, and caching integration points, based on the package set referenced in the project file.[cite:17]

This layer references the Domain project and the Services-Implementation abstractions project, which indicates the application logic depends on interfaces and contracts instead of concrete infrastructure types. That separation helps keep business workflows testable and easier to evolve.[cite:17]

#### 3. Services-Implementation

The Services-Implementation project contains service abstractions and shared infrastructure-facing contracts used by higher-level workflows. It references the Domain and Shared-Library projects and includes `Microsoft.AspNetCore.Http`, which usually supports file handling, request-scoped abstractions, and upload-related contracts.[cite:14]

This layer appears to sit between pure application behavior and infrastructure capabilities, helping define interfaces for things like file storage, messaging, and external integrations without hard-coupling the core logic to vendor SDKs.[cite:14]

#### 4. Persistence

The Persistence project contains data access and storage-specific implementation. It uses `Microsoft.EntityFrameworkCore`, `Microsoft.EntityFrameworkCore.Tools`, and `Npgsql.EntityFrameworkCore.PostgreSQL`, which means Entity Framework Core is used with PostgreSQL as the relational database provider in the current configuration.[cite:13]

At runtime, the DbContext is registered in the Web project through `UseNpgsql(...)`, and the connection string is read from configuration. The startup sequence also invokes migration and seeding helpers, which means persistence is treated as part of application bootstrap rather than a manual post-step.[cite:20]

#### 5. Presentation

The Presentation project contains the HTTP API surface. Its structure includes a `controllers` folder, a `Filters` folder, and an assembly marker used by the host project to discover controllers dynamically.[cite:11]

The available controllers include `AdminController`, `AuthenticationController`, and `GradesController`, which suggests the API is organized around real portal domains such as administration, identity, and student grading workflows. The project references the Services and Services-Implementation layers and relies on the shared ASP.NET Core framework reference instead of many direct transport packages.[cite:11][cite:12][cite:18]

#### 6. Web

The Web project is the executable entry point and composition root. It uses the ASP.NET Core Web SDK, references `Microsoft.AspNetCore.OpenApi`, `Microsoft.EntityFrameworkCore.Design`, and `Scalar.AspNetCore`, and references the Presentation and Persistence projects.[cite:15]

Its `Program.cs` wires controllers, OpenAPI, Scalar, DbContext registration, application services, JWT authentication, HTTP context accessor, persistence registration, custom middleware, database migration, and seed execution. This is a strong sign of a clean bootstrap layer that keeps startup concerns centralized.[cite:20]

#### 7. Shared Library

The Shared-Library project targets .NET 10 and currently has no external packages. That makes it a clean place for shared DTOs, constants, result wrappers, helper abstractions, and primitives that should be reused without dragging in infrastructure dependencies.[cite:16]

---

## Request flow

A request enters through the Web host, passes through middleware such as the custom exception handler, reaches controllers inside the Presentation project, and then flows into the Services layer for business execution. From there, persistence, caching, identity, media upload, email, or other external concerns can be resolved through abstractions and concrete implementations in the appropriate infrastructure projects.[cite:20][cite:11][cite:17]

This structure keeps the API surface thin while concentrating business behavior in dedicated services. It also makes the application easier to scale because transport, business rules, and infrastructure can evolve separately.[cite:8][cite:20]

---

## Technologies

| Area | Technology | Purpose |
|---|---|---|
| Runtime | .NET 10 | Main target framework across all projects.[cite:12][cite:13][cite:14][cite:15][cite:16][cite:17][cite:19] |
| Web API | ASP.NET Core | Hosting HTTP endpoints and middleware pipeline.[cite:12][cite:15][cite:20] |
| API docs | OpenAPI + Scalar.AspNetCore | Spec generation and interactive API reference in development.[cite:15][cite:20] |
| ORM | Entity Framework Core | Data access abstraction and migrations.[cite:13] |
| Database | PostgreSQL via Npgsql | Main relational database provider in runtime registration.[cite:13][cite:20] |
| Authentication | JWT Bearer | Token-based authentication for secured endpoints.[cite:17][cite:20] |
| Identity | ASP.NET Core Identity | User/role model and identity seed workflow.[cite:19][cite:20] |
| Validation | FluentValidation | Request and business validation pipeline.[cite:17] |
| Mediation | MediatR | Decoupled request/handler architecture for features.[cite:17] |
| Mapping | AutoMapper | Object-to-object mapping between entities and DTOs.[cite:17] |
| Caching | StackExchange.Redis | Redis integration for performance and distributed caching.[cite:17] |
| Media | CloudinaryDotNet | Cloud-based image/file storage integration.[cite:17] |
| Email | MailKit | SMTP/email workflows such as notifications or account flows.[cite:17] |
| Documents | QuestPDF | PDF generation for printable portal outputs.[cite:17] |
| Containers | Docker + Docker Compose | Containerized app and local database orchestration.[cite:8][cite:9][cite:10] |
| CI/CD | GitHub Actions | Repository automation and deployment workflows referenced by project requirements; add workflow files under `.github/workflows/` to build, test, and publish consistently. |

---

## Packages used

Below is a package-by-package explanation based on the project files in the solution.

### Domain

- `Microsoft.AspNetCore.Identity.EntityFrameworkCore` — integrates the domain/user model with ASP.NET Core Identity and EF-backed identity stores.[cite:19]

### Persistence

- `Microsoft.EntityFrameworkCore` — core ORM used for querying and saving relational data.[cite:13]
- `Microsoft.EntityFrameworkCore.Tools` — migration and design-time tooling for EF Core.[cite:13]
- `Npgsql.EntityFrameworkCore.PostgreSQL` — PostgreSQL provider for EF Core.[cite:13]

### Services

- `AutoMapper` — maps domain entities to DTOs and response models, reducing boilerplate mapping code.[cite:17]
- `CloudinaryDotNet` — integrates Cloudinary for image and file upload workflows, useful for profile photos, attachments, or media assets.[cite:17]
- `FluentValidation` — defines expressive validation rules for requests and commands.[cite:17]
- `FluentValidation.DependencyInjectionExtensions` — registers FluentValidation types with the DI container.[cite:17]
- `MailKit` — handles email sending for notifications, password flows, or system communication.[cite:17]
- `MediatR` — enables request/response and command/query handlers for cleaner feature organization.[cite:17]
- `Microsoft.AspNetCore.Authentication.JwtBearer` — validates bearer tokens and secures protected endpoints.[cite:17]
- `Microsoft.AspNetCore.Http.Abstractions` — exposes HTTP abstractions commonly needed in service contracts or request-aware services.[cite:17]
- `Microsoft.Extensions.Logging.Abstractions` — provides logging contracts without coupling to a concrete logger.[cite:17]
- `QuestPDF` — generates PDFs for academic reports, statements, summaries, or printable documents.[cite:17]
- `StackExchange.Redis` — connects the application to Redis for caching and performance optimization.[cite:17]

### Services-Implementation

- `Microsoft.AspNetCore.Http` — full HTTP primitives that often support file uploads or web-aware service implementations.[cite:14]

### Web

- `Microsoft.AspNetCore.OpenApi` — generates OpenAPI documents from ASP.NET Core endpoints.[cite:15]
- `Microsoft.EntityFrameworkCore.Design` — design-time EF services used for migrations and tooling.[cite:15]
- `Scalar.AspNetCore` — provides the Scalar UI for modern API documentation experience.[cite:15]

### Presentation

The Presentation project uses `Microsoft.AspNetCore.App` as a framework reference rather than individual ASP.NET Core packages. That gives it access to the shared ASP.NET Core stack for controller and web API behavior.[cite:12]

---

## Authentication and security

JWT authentication is explicitly registered during application startup through `AddJwtAuthentication(builder.Configuration)`, and the middleware pipeline enables both `UseAuthentication()` and `UseAuthorization()` before mapping controllers.[cite:20] This indicates the API is set up for token-based access control across protected endpoints.

Because the startup also seeds identity data after migrations, the application likely provisions baseline roles and/or default accounts automatically when a new environment is initialized. That is especially useful for admin bootstrap in university systems.[cite:20]

---

## API documentation

The Web host registers OpenAPI generation and exposes both the OpenAPI document and Scalar UI only in development mode. Specifically, `app.MapOpenApi()` serves the spec and `app.MapScalarApiReference()` serves the interactive reference UI.[cite:20]

This keeps the developer experience strong without necessarily exposing internal documentation surfaces in production. It is a clean setup for local development, QA, and onboarding other contributors.[cite:20]

---

## Cloudinary usage

The Services project references `CloudinaryDotNet`, which means media handling is intentionally abstracted into the application/service layer rather than being hard-coded directly inside controllers.[cite:17] In a student portal, Cloudinary is typically the right fit for profile images, assignment attachments, generated asset hosting, and any user-uploaded media that should not live on the API server file system.

A clean way to document its usage in this project is:

- Store `CloudName`, `ApiKey`, and `ApiSecret` in configuration or secrets storage.
- Inject a dedicated Cloudinary service through DI.
- Accept files through controller endpoints or service commands.
- Upload files to Cloudinary from the service layer.
- Save only the returned public URL and metadata in the database.

This pattern keeps the API stateless, avoids bloating the database with binary data, and makes image delivery faster through Cloudinary’s CDN-backed infrastructure. The package presence confirms the integration intent even if the exact implementation class is in deeper folders not enumerated here.[cite:17]

Example configuration block you can keep in the README:

```json
"CloudinarySettings": {
  "CloudName": "your-cloud-name",
  "ApiKey": "your-api-key",
  "ApiSecret": "your-api-secret"
}
```

---

## Redis usage

The Services layer references `StackExchange.Redis`, so Redis is part of the application’s architecture for performance-sensitive workflows.[cite:17] In a portal like this, Redis is a strong fit for caching frequently read data such as course metadata, department lookups, role-based access snapshots, dashboard summaries, or expensive grade/report queries.

The clean architectural benefit is that caching can sit behind service abstractions rather than polluting controller code. That way, handlers and services can decide when to read-through, invalidate, or refresh cached data while keeping endpoint code simple.[cite:17]

---

## Docker usage

The repository includes both a `Dockerfile` and a `docker-compose.yml`, which means containerized development and deployment are first-class concerns.[cite:8] The Dockerfile is a multi-stage build that uses the .NET 10 SDK image for restore, build, and publish, then copies the published output into the smaller ASP.NET Core 10 runtime image and exposes port 8080.[cite:9]

A key optimization in the Dockerfile is that project files are copied first and `dotnet restore` is run before copying the rest of the source. That improves Docker layer caching, so dependency restoration is not repeated unless project dependencies change.[cite:9]

### Docker Compose

The compose file defines two services: `api` and `db`. The API service builds from the repository Dockerfile, exposes port 8080, depends on the database service, and injects runtime environment variables including `ASPNETCORE_ENVIRONMENT` and `ConnectionStrings__DefaultConnection`; the database service uses `mcr.microsoft.com/mssql/server:2022-latest`, exposes port 1433, and persists data in a named volume.[cite:10]

One important detail: the compose file is currently configured for SQL Server, while the application startup and persistence project are configured for PostgreSQL using `UseNpgsql` and the Npgsql EF provider.[cite:10][cite:13][cite:20] That mismatch should be fixed by either switching compose to PostgreSQL or changing the app configuration/provider to SQL Server so local container orchestration matches the actual runtime stack.

### Run with Docker

```bash
docker compose up --build
```

Once running, the API will be available on `http://localhost:8080` based on the compose port mapping.[cite:10]

---

## GitHub Actions usage

The repository contents visible here did not expose a `.github/workflows` folder during inspection, so there is no verified workflow file to describe line by line from the repository snapshot I checked.[cite:8] Still, since the project already has Docker assets and a clean multi-project .NET structure, GitHub Actions is the natural place to automate build, restore, test, image creation, and deployment.

A deep README should explain GitHub Actions in the context of this project like this:

### What GitHub Actions should handle

- Restore NuGet dependencies for the full solution.
- Build the solution in Release mode.
- Run automated tests when test projects are added.
- Build the Docker image from the repository `Dockerfile`.
- Optionally push the image to GitHub Container Registry or Docker Hub.
- Deploy to the target host after a successful main-branch pipeline.

### Why it matters here

Because this solution has multiple projects and external dependencies, CI prevents integration drift between layers. It also gives you a repeatable path from commit to deploy, especially important when using Docker and environment-based configuration.

### Suggested workflow stages

1. Checkout code.
2. Setup .NET 10 SDK.
3. Restore solution dependencies.
4. Build the solution.
5. Run tests.
6. Build Docker image.
7. Push image on `main` or tagged releases.
8. Deploy using environment secrets.

### Suggested secrets

- `CONNECTION_STRING`
- `JWT_KEY`
- `CLOUDINARY_CLOUD_NAME`
- `CLOUDINARY_API_KEY`
- `CLOUDINARY_API_SECRET`
- `REDIS_CONNECTION`
- `SMTP_HOST`
- `SMTP_USER`
- `SMTP_PASS`

If workflow files already exist in another branch or are added later, this README section will still stay aligned with the actual architecture because the build, Docker, and secret requirements all come directly from the verified project setup.[cite:9][cite:10][cite:15][cite:17][cite:20]

---

## Startup and dependency injection

The startup logic in `Program.cs` reveals a clean composition-root pattern. Controllers are loaded from the Presentation assembly, OpenAPI is registered, the DbContext is configured with PostgreSQL, a data initializer is registered, application services and JWT auth are added via extension methods, persistence services are registered, and HTTP context access is enabled for downstream services.[cite:20]

This is a good design because each layer owns its own registrations while the Web project remains the central place where the final graph is composed. It keeps infrastructure discoverable and makes onboarding significantly easier for new contributors.[cite:20]

---

## Current API surface

The Presentation layer currently exposes at least the following controllers:

- `AdminController`
- `AuthenticationController`
- `GradesController`
- `ApiBaseController`

That controller set strongly suggests three major domains already implemented in the portal: administrative management, authentication/identity flows, and academic grading workflows.[cite:18]

---

## Running locally

### Prerequisites

- .NET 10 SDK
- PostgreSQL, unless you align the project to SQL Server instead.[cite:13][cite:20]
- Docker Desktop, if you want containerized local development.[cite:8][cite:9][cite:10]

### Standard local run

```bash
dotnet restore BNU-Student-Portal.slnx
dotnet build BNU-Student-Portal.slnx
dotnet run --project BNU-Student-Portal-Web
```

### Recommended configuration keys

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=BNUStudentPortalDb;Username=postgres;Password=yourpassword"
  },
  "JwtSettings": {
    "Key": "your-secret-key",
    "Issuer": "your-issuer",
    "Audience": "your-audience"
  },
  "CloudinarySettings": {
    "CloudName": "your-cloud-name",
    "ApiKey": "your-api-key",
    "ApiSecret": "your-api-secret"
  },
  "Redis": {
    "Configuration": "localhost:6379"
  }
}
```

Because the application migrates and seeds data at startup, the first run can initialize the database schema and baseline identity data automatically if configuration is valid.[cite:20]

---

## Strengths of the solution

- Clear multi-project separation with dedicated Domain, Services, Persistence, Presentation, and Web host layers.[cite:8]
- Modern .NET 10-based stack across the full solution.[cite:12][cite:13][cite:14][cite:15][cite:16][cite:17][cite:19]
- Solid cross-cutting package choices: MediatR, FluentValidation, AutoMapper, JWT, Redis, MailKit, QuestPDF, and Cloudinary.[cite:17]
- Production-minded operational setup with Docker, startup migrations, identity seeding, OpenAPI, Scalar, and exception middleware.[cite:9][cite:10][cite:20]

---

## Notes

There is one architectural/runtime inconsistency worth documenting and fixing: Docker Compose provisions SQL Server, but the actual runtime registration and EF provider are PostgreSQL. Aligning those two parts will make the local developer experience much smoother and avoid confusion for contributors.[cite:10][cite:13][cite:20]
