# Freddy

**Slimme zorgassistent voor protocollen en procedures.**

Freddy is een AI-gestuurde chatapplicatie voor zorgmedewerkers. Via een eenvoudige
chat-interface kunnen medewerkers in natuurlijke taal vragen stellen over protocollen,
stappenplannen en procedures. Freddy geeft direct antwoord met bronvermelding.

De backoffice-applicatie biedt beheerders de mogelijkheid om kennispakketten en
documenten te beheren die de AI gebruikt voor het beantwoorden van vragen.

## Stack

| Laag | Technologie |
|------|------------|
| Chat frontend | React 19 + TypeScript 5.9 + Vite 6 + Tailwind CSS |
| Backoffice frontend | React 19 + TypeScript 5.9 + Vite 6 + Tailwind CSS |
| Backend | ASP.NET Core 9 (.NET 9), C# 13, MediatR, Semantic Kernel |
| Database | PostgreSQL 16 + pgvector |
| AI | Ollama (Mistral 7B) |
| Logging | Seq (Serilog) |
| Hosting | Docker Compose op Hetzner VPS |

## Quick Start

### Vereisten

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Node.js 22+](https://nodejs.org/)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) of Docker Engine

### 1. Infrastructuur starten

Start PostgreSQL, Ollama en Seq via Docker Compose:

```bash
docker compose -f infra/docker-compose.yml up -d
```

### 2. AI-model downloaden (eenmalig, ≈4 GB)

```bash
docker exec freddy-ollama ollama pull mistral:7b
```

### 3. Backend starten

```bash
# Restore tools en build
dotnet tool restore
dotnet build

# Database migratie uitvoeren
dotnet ef database update --project src/Freddy.Infrastructure --startup-project src/Freddy.Api

# API starten
dotnet run --project src/Freddy.Api
```

De API start met mock data: 3 gepubliceerde kennispakketten (Voedselbank,
Medicatie in Beheer, Valpreventie) met in totaal 6 documenten.

### 4. Chat frontend starten (nieuw terminal)

```bash
cd apps/Freddy.Web
npm install
npm run dev
```

### 5. Backoffice frontend starten (nieuw terminal)

```bash
cd apps/Freddy.Backoffice
npm install
npm run dev
```

### URLs

| Service | URL |
|---------|-----|
| API (Swagger) | <http://localhost:5000/swagger> |
| Chat frontend | <http://localhost:5173> |
| Backoffice | <http://localhost:5174> |
| Seq (logging) | <http://localhost:8081> |

### Admin API key

Het Backoffice en de admin-endpoints gebruiken API-key authenticatie:

- **Header:** `X-Admin-Api-Key`
- **Development key:** `freddy-admin-dev-key`

De backoffice frontend stuurt deze key automatisch mee.

> Zie [docs/development/getting-started.md](docs/development/getting-started.md) voor de uitgebreide opstartgids met troubleshooting.

## Project Structure

```
Freddy/
├── src/                        # Backend (Clean Architecture)
│   ├── Freddy.Api/             # ASP.NET Core Web API, controllers, middleware
│   ├── Freddy.Application/     # CQRS handlers, validators, interfaces
│   └── Freddy.Infrastructure/  # EF Core, Ollama, file storage, services
├── apps/                       # Frontend applicaties
│   ├── Freddy.Web/             # Chat interface (React, port 5173)
│   └── Freddy.Backoffice/      # Beheer interface (React, port 5174)
├── tests/                      # Test projecten
│   ├── Freddy.Api.Tests/       # API integration tests
│   ├── Freddy.Application.Tests/ # Unit tests voor handlers
│   └── Freddy.AI.Tests/        # AI/Ollama integration tests
├── docs/                       # Documentatie & ADRs
├── infra/                      # Docker Compose, nginx config
├── memory-bank/                # Project context & voortgang
└── tools/                      # Build & dev scripts
```

## Features

### Chat (Freddy.Web)

- Gesprekken aanmaken, berichten versturen, AI-antwoorden ontvangen
- Automatische routering naar het juiste kennispakket via Ollama JSON classifier
- Documenten van het gematchte pakket worden meegestuurd als context
- Gebruiksvriendelijke foutmeldingen in het Nederlands bij AI-onbeschikbaarheid
- Optimistic UI updates en real-time gesprekslijst

### Backoffice (Freddy.Backoffice)

- Kennispakketten beheren (CRUD) met publiceer/depubliceer lifecycle
- Documenten beheren per pakket (CRUD + bestand uploaden, max 50 MB)
- Admin API-key authenticatie

### Backend (Freddy.Api)

- Clean Architecture met CQRS (MediatR) en FluentValidation
- `Result<T>` pattern voor foutafhandeling (geen exceptions voor business logic)
- Semantic Kernel + Ollama integratie voor chat en pakketclassificatie
- Bestandsopslag via `LocalFileStorageService`
- Mock data seeding bij eerste startup

## Scripts

```bash
./tools/build.ps1    # Build alles
./tools/test.ps1     # Run alle tests (24 tests)
./tools/format.ps1   # Format + lint check
```

## Docker Services

| Container | Image | Poort |
|-----------|-------|-------|
| freddy-db | pgvector/pgvector:pg16 | 5433 → 5432 |
| freddy-ollama | ollama/ollama:latest | 11434 → 11434 |
| freddy-seq | datalust/seq:latest | 8081 (UI), 5341 (ingestion) |

## Documentatie

Zie [docs/README.md](docs/README.md) voor de volledige documentatie-index.

## Contributing

Zie [CONTRIBUTING.md](CONTRIBUTING.md) voor development workflow en conventies.

## Licentie

Zie [LICENSE](LICENSE).
