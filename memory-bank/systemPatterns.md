# System Patterns

## Architecture

Clean Architecture with 3 layers:

- **Freddy.Api** — Controllers, middleware, request models, Program.cs
- **Freddy.Application** — Entities, CQRS features (Commands/Queries/DTOs), interfaces, Result<T>
- **Freddy.Infrastructure** — EF Core persistence, AI integrations (Ollama), DI registration

## Key Patterns

### CQRS with MediatR

- Commands for mutations, Queries for reads
- Each command/query in its own file with handler
- FluentValidation validators for Create/Update commands
- MediatR pipeline with ValidationBehavior

### Result<T> Pattern

- `Result<T>.Success(value)`, `Result<T>.NotFound(message)`, `Result<T>.ValidationError(message)`, `Result<T>.Failure(message)`
- Controllers use `result.ToActionResult()` extension method
- Maps to appropriate HTTP status codes

### Entity Design

- Guid IDs (UUIDv7 via `Guid.CreateVersion7()`)
- `CreatedAt`/`UpdatedAt` timestamps with `DateTimeOffset.UtcNow`
- Snake_case column naming in PostgreSQL

### Admin Authentication

- `AdminApiKeyMiddleware` checks `X-Admin-Api-Key` header
- Only applies to paths starting with `/api/admin`
- Key configured in `Admin:ApiKey` appsettings section

### Package Lifecycle

- `IsPublished` boolean (default false) controls visibility
- Only published packages appear in chat routing
- Publish/Unpublish as separate endpoints

## Component Relationships

```
Chat Flow: User → ChatController → SendMessageCommand → PackageRouter (Ollama) → Published Packages
Admin Flow: Admin → AdminPackagesController → CQRS Commands/Queries → PackageRepository → PostgreSQL
```
