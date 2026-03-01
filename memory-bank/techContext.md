# Tech Context

## Technologies

| Technology | Version | Purpose |
|---|---|---|
| .NET | 9.0 | Runtime |
| C# | 13 | Language |
| ASP.NET Core | 9.0 | Web framework |
| EF Core + Npgsql | 9.0 | ORM + PostgreSQL provider |
| MediatR | 14.x | CQRS mediator |
| FluentValidation | 12.x | Request validation |
| Semantic Kernel | 1.72.0 | AI orchestration |
| Ollama | — | Local LLM (mistral:7b) |
| React | 19 | Frontend framework |
| TypeScript | 5.9 | Frontend language |
| Vite | 6 | Frontend build tool |
| Tailwind CSS | 3 | Styling |
| ky | 1.14 | HTTP client |
| @tanstack/react-query | 5 | Server state management |
| react-router-dom | 7 | Routing |
| react-hook-form | 7 | Form management |
| zod | 4 | Schema validation |
| PostgreSQL | 16 | Database (port 5433) |
| Seq | — | Structured logging (port 8081/5341) |

## Development Setup

- Docker containers: `freddy-db` (5433), `freddy-seq` (8081/5341), `freddy-ollama` (11434)
- API runs on localhost:5000
- Chat frontend runs on localhost:5173 (Vite dev server — `apps/Freddy.Web`)
- Backoffice frontend runs on localhost:5174 (Vite dev server — `apps/Freddy.Backoffice`)
- Admin API key: `freddy-admin-dev-key` (dev only)
- CORS allows: localhost:5173, localhost:5174

## Build Configuration

- `Directory.Build.props`: EnforceCodeStyleInBuild, Meziantou.Analyzer, AnalysisLevel=latest-recommended
- Solution file: `Freddy.sln`
- Test project: `tests/Freddy.Application.Tests`

## Frontend Apps

Both apps share the same Vite + React 19 + TypeScript + Tailwind CSS stack with identical dependencies. They differ only in purpose and API client configuration:

- **Freddy.Web**: Chat interface, uses `/api/v1` prefix with Bearer token auth
- **Freddy.Backoffice**: Admin interface, uses `/api/admin` prefix with X-Admin-Api-Key header

## Technical Constraints

- All async methods use `ConfigureAwait(false)` in Infrastructure layer
- Read operations use `AsNoTracking()`
- PostgreSQL text[] arrays with GIN indexes for tags/synonyms
- jsonb columns for document steps content
- DocumentType enum stored as string in database
- File uploads stored locally in `wwwroot/uploads/documents/` (MVP; replace with blob storage for production)
