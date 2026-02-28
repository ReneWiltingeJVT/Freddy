# Freddy — Copilot Coding Instructions

> This file is read automatically by GitHub Copilot / AI assistants.
> It captures architecture rules, coding conventions, and project-specific patterns.

## Project Overview

Freddy is a healthcare chat application for Dutch home-care professionals. It answers questions about care protocols using RAG (Retrieval Augmented Generation) powered by a self-hosted LLM.

**Tech stack:** ASP.NET Core 9 · C# 13 · PostgreSQL 16 + pgvector · Semantic Kernel · Ollama (Mistral 7B) · React 19 + TypeScript + Tailwind CSS (PWA)

## Language & Formatting

- **Language:** All code, comments, and logs in **English**
- **UI text:** Dutch (nl-NL) for user-facing content
- **Line endings:** LF
- **Encoding:** UTF-8
- **Indentation:** 4 spaces (C#), 2 spaces (JSON, YAML, Markdown)

## Architecture Rules

This project follows **Clean Architecture** with a pragmatic 3-layer approach:

```text
src/
  Freddy.Api/            → Controllers, middleware, DI registration
  Freddy.Application/    → CQRS handlers, DTOs, validation, interfaces
  Freddy.Infrastructure/ → EF Core, Ollama client, file storage, external services
apps/
  Freddy.Web/            → React 19 + Vite + TypeScript SPA (PWA)
```

### Dependency Rules (STRICT)

- `Api` → `Application` → `Infrastructure` (dependency inversion via interfaces)
- `Application` **NEVER** references `Infrastructure` or `Api` directly
- `Infrastructure` implements interfaces defined in `Application`
- `Api` only dispatches commands/queries via `IMediator` — no business logic in controllers

### CQRS Pattern

Every feature follows this structure:

```text
Application/Features/{FeatureName}/
  Commands/
    {Action}Command.cs
    {Action}CommandHandler.cs
    {Action}CommandValidator.cs
  Queries/
    Get{Entity}Query.cs
    Get{Entity}QueryHandler.cs
  DTOs/
    {Entity}Dto.cs
```

## C# Conventions

- **Target:** .NET 9 / C# 13
- **Nullable:** Enabled globally — no `null` without explicit `?`
- Use `record` for DTOs and value objects
- Use `sealed` on classes that are not designed for inheritance
- Use primary constructors where appropriate
- Use file-scoped namespaces (`namespace Freddy.Application.Features.Chat;`)
- Use pattern matching and switch expressions
- Guard clauses: `ArgumentNullException.ThrowIfNull(param)`
- Collections: Return `IReadOnlyList<T>`, accept `IEnumerable<T>`
- Async all the way — suffix with `Async`, pass `CancellationToken`
- No `#region` blocks

### Naming

| Element | Convention | Example |
|---------|-----------|---------|
| Class | PascalCase | `ChatMessageHandler` |
| Interface | `I` + PascalCase | `IChatService` |
| Method | PascalCase + Async suffix | `SendMessageAsync` |
| Property | PascalCase | `CreatedAt` |
| Parameter | camelCase | `cancellationToken` |
| Private field | `_camelCase` | `_chatRepository` |
| Constant | PascalCase | `MaxTokenCount` |

## Database & EF Core

- **PostgreSQL 16** with **pgvector** extension
- **Npgsql + EF Core 9** — always use parameterized queries
- Entity configurations in separate `IEntityTypeConfiguration<T>` classes
- Migrations via `dotnet ef` CLI — never edit migration files manually
- Use `DateTimeOffset` (not `DateTime`) for all timestamps
- All entities have `CreatedAt` and `UpdatedAt` audit fields
- Soft delete via `IsDeleted` flag where applicable

## AI / RAG Rules

- Use **Semantic Kernel** as the AI orchestration layer
- LLM provider is abstracted — switchable between Ollama and Groq
- Embeddings: `nomic-embed-text` (768 dimensions) via Ollama
- Vector similarity: cosine distance via pgvector `<=>` operator
- RAG responses **must** include source attribution
- Always provide a "I don't know" fallback — never hallucinate
- System prompts are stored as configuration, not hardcoded
- Temperature: 0.1 for factual Q&A, 0.3 for paraphrasing
- Max tokens: 512 for responses, 2048 for context window budget
- Chunk size: 512 tokens with 50-token overlap
- Top-K retrieval: 5 chunks, similarity threshold ≥ 0.7

## API Design

- RESTful endpoints, resource-based URLs
- Versioned via URL path: `/api/v1/...`
- Standard HTTP status codes (200, 201, 204, 400, 401, 403, 404, 409, 422, 500)
- Problem Details (RFC 9457) for all error responses
- Pagination: `?page=1&pageSize=20` (max 100)
- All endpoints require `[Authorize]` unless explicitly public
- `[ProducesResponseType]` on every action

## Logging

- **Serilog** with structured logging
- Log to console + **Seq** (via `Serilog.Sinks.Seq`)
- Use semantic log messages: `Log.Information("Chat message processed for {UserId}", userId)`
- **Never** log sensitive data (tokens, passwords, patient info)
- Log levels: `Debug` for dev details, `Information` for business events, `Warning` for recoverable issues, `Error` for failures

## Testing

- **xUnit** for all tests
- **FluentAssertions** for assertions
- **NSubstitute** for mocking
- Test naming: `MethodName_Scenario_ExpectedResult`
- Arrange-Act-Assert pattern
- Integration tests use `WebApplicationFactory<Program>` with PostgreSQL testcontainer
- AI tests use golden test set with expected Q&A pairs

## Security Defaults

- Authentication: ASP.NET Identity + JWT Bearer
- Access tokens: 15 min expiry
- Refresh tokens: 7 days expiry
- HTTPS enforced (HSTS in production)
- CORS restricted to known origins
- Rate limiting on all public endpoints
- Input validation via FluentValidation on every command/query
- No patient data — only care protocol documents

## Performance

- Response time target: < 2 seconds for chat (including LLM)
- Use `IMemoryCache` for frequently accessed reference data
- EF Core: Use `.AsNoTracking()` for read queries
- Pagination on all list endpoints
- Background processing via `IHostedService` for document ingestion

## Frontend (React + TypeScript)

- **Framework:** React 19 + TypeScript (strict) + Vite 6
- **Styling:** Tailwind CSS v4 — utility-first, no inline styles, no CSS modules
- **Components:** shadcn/ui + Radix primitives — customized to match Figma design
- **Server state:** TanStack Query v5 — no Redux, no global state for server data
- **Routing:** React Router v7
- **Forms:** React Hook Form + Zod validation
- **HTTP client:** ky (lightweight fetch wrapper)
- **PWA:** vite-plugin-pwa for service worker + manifest

### Component Conventions

- Functional components only (no class components)
- Named exports: `export function ChatMessage() { ... }`
- One component per file, filename matches component name: `ChatMessage.tsx`
- Custom hooks extracted to `hooks/` directory: `useChat.ts`, `useAuth.ts`
- Feature-based folder structure: `features/chat/`, `features/auth/`
- Shared UI components in `components/ui/` (shadcn/ui)

### TypeScript Rules

- Strict mode enabled (`"strict": true`)
- No `any` — use `unknown` and narrow
- Interfaces for object shapes, types for unions/intersections
- API response types in `types/` directory, mirroring backend DTOs
- Zod schemas as single source of truth for form validation
