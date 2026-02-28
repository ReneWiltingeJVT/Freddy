# Freddy

**Slimme zorgassistent voor protocollen en procedures.**

Freddy is een AI-gestuurde chatapplicatie voor zorgmedewerkers. Via een eenvoudige
chat-interface kunnen medewerkers in natuurlijke taal vragen stellen over protocollen,
stappenplannen en procedures. Freddy geeft direct antwoord met bronvermelding.

## Stack

| Laag | Technologie |
|------|------------|
| Frontend | React 19 + TypeScript + Tailwind CSS (PWA) |
| Backend | ASP.NET Core 9, MediatR, Semantic Kernel |
| Database | PostgreSQL 16 + pgvector |
| AI | Ollama (Mistral 7B), nomic-embed-text |
| Hosting | Docker Compose op Hetzner VPS |

## Quick Start

### Vereisten

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Node.js 18+](https://nodejs.org/) (22 aanbevolen voor de React frontend)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) of Docker Engine
- [Ollama](https://ollama.ai/) (voor lokale AI development)

### Stappen

```bash
# 1. Start infrastructuur (PostgreSQL, Ollama, Seq)
docker compose -f infra/docker-compose.yml up -d

# 2. Download AI-model (eenmalig, ≈4 GB)
docker exec freddy-ollama ollama pull mistral:7b

# 3. Restore tools en build
dotnet tool restore
dotnet build

# 4. Database migratie
dotnet ef database update --project src/Freddy.Infrastructure --startup-project src/Freddy.Api

# 5. Start API (terminal 1)
dotnet run --project src/Freddy.Api

# 6. Start React frontend (terminal 2)
cd apps/Freddy.Web
npm install
npm run dev
```

- **API:** <http://localhost:5000> (Swagger: <http://localhost:5000/swagger>)
- **Frontend:** <http://localhost:5173>
- **Seq logs:** <http://localhost:8081>

> Zie [docs/development/getting-started.md](docs/development/getting-started.md) voor de uitgebreide opstartgids met troubleshooting.

### Scripts

```bash
./tools/build.ps1    # Build alles
./tools/test.ps1     # Run alle tests
./tools/format.ps1   # Format + lint check
```

## Project Structure

```
Freddy/
├── src/                    # Backend source code
│   ├── Freddy.Api/         # ASP.NET Core Web API
│   ├── Freddy.Application/ # Business logic, CQRS handlers
│   └── Freddy.Infrastructure/ # EF Core, Ollama, services
├── apps/                   # Client applications
│   └── Freddy.Web/         # React 19 + Vite + TypeScript (PWA)
├── tests/                  # Test projects
├── docs/                   # Documentation
├── infra/                  # Docker Compose, IaC
└── tools/                  # Build & dev scripts
```

## Documentatie

Zie [docs/README.md](docs/README.md) voor de volledige documentatie-index.

## Contributing

Zie [CONTRIBUTING.md](CONTRIBUTING.md) voor development workflow en conventies.

## Licentie

Zie [LICENSE](LICENSE).
