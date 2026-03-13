# System Patterns

## Architecture

Clean Architecture with 3 layers:

- **Freddy.Api** — Controllers, middleware, request models, Program.cs, static file serving
- **Freddy.Application** — Entities, CQRS features (Commands/Queries/DTOs), interfaces, Result<T>
- **Freddy.Infrastructure** — EF Core persistence, AI integrations (Ollama), file storage, DI registration

Two frontend apps:

- **Freddy.Web** — Chat interface (React, port 5173)
- **Freddy.Backoffice** — Admin package management (React, port 5174)

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

### File Storage

- `IFileStorageService` abstraction with `UploadAsync`/`DeleteAsync`
- `LocalFileStorageService` stores in `wwwroot/uploads/documents/`
- Files named `{guid}-{sanitizedName}` to prevent collisions
- Served via ASP.NET Core static files middleware
- Upload endpoint: multipart/form-data, 50MB limit, auto-detect document type

### Chat Response Building (Four-Layer Pipeline)

- **Layer 0 (Small Talk)**: ISmallTalkDetector checks for greetings/thanks/confusion before routing (<1ms, deterministic word-list matching). Returns hardcoded template responses. No AI, no package lookup.
- **Layer 1 (Client Detection)**: IClientDetector scans user message for client names/aliases. Deterministic longest-match-first strategy. When detected, scoped retrieval includes client-specific PersonalPlan packages.
- **Layer 2 (Fast-path)**: FastPathRouter scores candidates deterministically via title/tag/synonym/content/description/n-gram matching (<10ms). PersonalPlan packages get +0.1 category boost. Stopwords filtered.
- **Layer 3 (Slow-path)**: OllamaPackageRouter only called when 2+ candidates in ambiguity zone (0.3-0.6) or for zero-match recovery with suggestions. Model: Qwen 2.5 1.5B, Temperature=0.1, MaxTokens=128, Timeout=15s
- Thresholds configurable via `Routing` section in appsettings.json
- `IsServiceUnavailable` flag for AI connectivity issues
- High-confidence match (≥0.6): returns package content + document links
- Single medium confidence (0.3-0.6): asks for confirmation
- Multiple medium confidence: delegates to lightweight LLM for disambiguation
- Low/no match (<0.3): returns top-3 suggestions if available, else fallback message
- Documents formatted as markdown links in response

### Package Categories

- `PackageCategory` enum: Protocol (0), WorkInstruction (1), PersonalPlan (2)
- Database CHECK constraint: PersonalPlan requires ClientId, others must have ClientId=NULL
- Admin API validates category string and enforces ClientId rules
- PersonalPlan packages only included in chat when client detected in message

### Client Detection + Scoped Retrieval

- `IClientDetector` → `ClientDetector`: Loads all active clients, matches display names (longest first) then aliases (≥3 chars, longest first), case-insensitive substring matching
- Scoped retrieval in `SendMessageCommandHandler.RouteAndBuildResponseAsync()`:
  - Client detected → general packages (non-PersonalPlan) + client-specific PersonalPlan
  - No client → all published packages excluding PersonalPlan

### Audit Logging

- `AuditLog` entity: UserId, Action, EntityType, EntityId, Details (jsonb), Timestamp
- `IAuditLogRepository` → `AuditLogRepository`: LogAsync, GetByEntityAsync
- Infrastructure ready; integration points to be wired into admin operations

## Component Relationships

```
Chat Flow: User → ChatController → SendMessageCommand → SmallTalkDetector → ClientDetector → CompositePackageRouter → FastPathRouter (deterministic, +CategoryBoost) → [optional: OllamaPackageRouter (LLM, zero-match recovery)] → Scoped Published Packages → Documents
Admin Flow: Admin → AdminPackagesController → CQRS Commands/Queries → PackageRepository → PostgreSQL
Client Flow: Admin → AdminClientsController → CQRS Commands/Queries → ClientRepository → PostgreSQL
Upload Flow: Admin → AdminDocumentsController/upload → UploadDocumentCommand → FileStorageService → wwwroot/uploads
Backoffice: React App → ky (X-Admin-Api-Key) → /api/admin/* → AdminControllers
```
