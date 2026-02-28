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
| TypeScript | — | Frontend language |
| Vite | 6 | Frontend build tool |
| Tailwind CSS | — | Styling |
| PostgreSQL | 16 | Database (port 5433) |
| Seq | — | Structured logging (port 8081/5341) |

## Development Setup

- Docker containers: `freddy-db` (5433), `freddy-seq` (8081/5341), `freddy-ollama` (11434)
- API runs on localhost:5000
- Frontend runs on localhost:5173 (Vite dev server)
- Admin API key: `freddy-admin-dev-key` (dev only)

## Build Configuration

- `Directory.Build.props`: EnforceCodeStyleInBuild, Meziantou.Analyzer, AnalysisLevel=latest-recommended
- Solution file: `Freddy.sln`
- Test project: `tests/Freddy.Application.Tests`

## Technical Constraints

- All async methods use `ConfigureAwait(false)`
- Read operations use `AsNoTracking()`
- PostgreSQL text[] arrays with GIN indexes for tags/synonyms
- jsonb columns for document steps content
- DocumentType enum stored as string in database
