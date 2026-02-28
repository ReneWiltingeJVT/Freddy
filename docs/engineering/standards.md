## Engineering Standards

### Language & Framework

- C# 13, .NET 9, ASP.NET Core 9
- Nullable reference types: **enabled** (project-wide)
- File-scoped namespaces: **always**
- Implicit usings: **enabled**

### Project Layering

```
Freddy.Api              → Controllers, middleware, DI registration
Freddy.Application      → Use cases (CQRS), DTOs, validators, AI orchestration
Freddy.Infrastructure   → EF Core, Ollama client, external services
```

**Dependency rule:** Api → Application. Infrastructure → Application.
Application has NO reference to Api or Infrastructure (only interfaces).

### Naming Conventions

| Element | Convention | Example |
|---------|------------|---------|
| Classes/Records | PascalCase | `ChatService`, `MessageDto` |
| Interfaces | I + PascalCase | `IChatService`, `IDocumentRepository` |
| Methods | PascalCase | `GetConversationAsync` |
| Async methods | +Async suffix | `SendMessageAsync` |
| Private fields | _camelCase | `_mediator`, `_logger` |
| Local variables | camelCase | `conversationId` |
| Constants | PascalCase | `MaxTokenCount` |
| Test methods | Method_Scenario_Expected | `SendMessage_EmptyInput_ThrowsValidation` |

### CQRS Structure

```
UseCases/
  {Feature}/
    Commands/Create{Feature}Command.cs
    Queries/Get{Feature}Query.cs
    Handlers/Create{Feature}Handler.cs
    DTOs/{Feature}Dto.cs
    Validators/Create{Feature}CommandValidator.cs
```

### Error Handling

- RFC 7807 ProblemDetails for all API errors
- Result pattern in handlers (no exceptions for expected failures)
- Guard clauses: `ArgumentException.ThrowIfNullOrEmpty()`
- Custom exceptions: `NotFoundException`, `ValidationException`
- Global exception handler middleware

### Logging

- `ILogger<T>` via DI — never `Console.WriteLine`
- Structured logging: `_logger.LogInformation("Processed {MessageId}", id)`
- Never log sensitive data
- Serilog + Seq

### Testing

- xUnit + Moq
- Naming: `Method_Scenario_Expected`
- AAA pattern (Arrange, Act, Assert)
- Testcontainers for integration tests
- Golden test set for AI quality

### No Magic Strings

- Constants for roles, policies, config keys
- Enums for finite sets
- Options pattern for configuration
- Never hardcode URLs, connection strings, model names

### Frontend (React + TypeScript)

- **Components**: functional only, named exports, one per file
- **Hooks**: custom hooks in `hooks/` directory (`useChat.ts`, `useAuth.ts`)
- **State**: TanStack Query for server state — no Redux or global state stores
- **Styling**: Tailwind CSS utility classes — no inline styles, no CSS modules
- **Types**: strict mode, no `any`, API types in `types/` mirroring C# DTOs
- **Forms**: React Hook Form + Zod schemas as single source of truth
- **Folder structure**: feature-based (`features/chat/`, `features/auth/`)
- **Shared UI**: shadcn/ui components in `components/ui/`
