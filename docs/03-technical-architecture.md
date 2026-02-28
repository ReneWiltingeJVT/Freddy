## Technische Architectuur

### High-Level Architectuur

```mermaid
flowchart TB
    subgraph Client [Client Layer]
        SPA[React SPA + PWA<br/>iOS / Android / Desktop]
    end

    subgraph Backend [Backend - ASP.NET Core 9]
        API[REST API<br/>Controllers + MediatR]
        SK[Semantic Kernel<br/>AI Orchestratie]
        DocProc[Document Processor<br/>Background Service]
    end

    subgraph Data [Data Layer]
        PG[(PostgreSQL 16<br/>+ pgvector)]
    end

    subgraph AI [AI Layer]
        Ollama[Ollama<br/>Mistral 7B + nomic-embed-text]
    end

    SPA <-->|HTTPS/JSON| API
    API <--> SK
    API <--> PG
    SK <--> Ollama
    SK <--> PG
    DocProc <--> PG
    DocProc <--> Ollama
```

### Backend (.NET)

**Architectuurstijl:** Pragmatische Clean Architecture (3-laags)

```
Freddy.sln
├── src/
│   ├── Freddy.Api/              # Presentation: controllers, middleware
│   ├── Freddy.Application/      # Business logic: CQRS handlers, AI orchestration
│   └── Freddy.Infrastructure/   # External: EF Core, Ollama, services
├── apps/
│   └── Freddy.Web/              # React 19 + Vite + TypeScript SPA (PWA)
└── tests/
```

**Monolith vs Microservices**: Modular Monolith. Eén deployable, maar met duidelijke
module-grenzen (Chat, Documents, AI) die later als service kunnen afsplitsen.

### API Ontwerp

| Endpoint | Methode | Beschrijving | MVP |
|----------|---------|-------------|-----|
| `/api/v1/chat/conversations` | GET | Lijst conversaties | ✅ |
| `/api/v1/chat/conversations` | POST | Nieuwe conversatie | ✅ |
| `/api/v1/chat/conversations/{id}/messages` | GET | Berichten ophalen | ✅ |
| `/api/v1/chat/conversations/{id}/messages` | POST | Bericht sturen (trigger AI) | ✅ |
| `/api/v1/auth/login` | POST | Authenticatie | ✅ |
| `/api/v1/auth/refresh` | POST | Token refresh | ✅ |
| `/api/v1/documents` | GET | Protocollen lijst | ✅ |
| `/api/v1/documents/{id}` | GET | Document details | ✅ |
| `/api/v1/admin/documents` | POST | Upload document | Fase 3 |
| `/api/v1/admin/faq` | CRUD | FAQ beheer | Fase 3 |

### Patterns

| Pattern | Toepassing | Reden |
|---------|-----------|-------|
| CQRS | Alle use cases | Scheiding read/write |
| MediatR | Command/Query dispatch | Loose coupling, pipeline behaviors |
| Result Pattern | Handler return types | Geen exceptions voor flow control |
| Options Pattern | Configuratie | Strongly-typed settings |
| Background Service | Document processing | Non-blocking ingestie |

### Database: PostgreSQL 16 + pgvector

Eén database voor zowel applicatiedata als vectoropslag. Geen extra services.
pgvector is ruim voldoende voor 20-200 documenten (~20.000 chunks).
Bij 100.000+ chunks kan Qdrant worden overwogen.

### Frontend & Mobile

**Fase 1 (MVP):** React 19 + TypeScript + Vite SPA met PWA (vite-plugin-pwa) — installeerbaar via browser, geen App Store nodig.
**Fase 2:** PWA behouden of optioneel React Native (Expo) voor App Store publicatie.
**Fase 3:** React-gebaseerd backoffice, hergebruik van componenten uit de SPA.

### Deployment Architectuur

```mermaid
flowchart TB
    subgraph Internet
        User[Zorgmedewerker<br/>React PWA]
    end

    subgraph Hetzner [Hetzner VPS CX31 - 4 vCPU / 8GB RAM]
        subgraph Docker [Docker Compose]
            Nginx[Nginx + TLS<br/>Static files + Reverse proxy]
            API[Freddy API<br/>ASP.NET Core 9]
            Ollama[Ollama<br/>Mistral 7B]
            PG[(PostgreSQL 16<br/>+ pgvector)]
            Seq[Seq Logging]
        end
    end

    User -->|HTTPS| Nginx
    Nginx --> API
    API --> PG
    API --> Ollama
    API --> Seq
```

### Geschatte Kosten

| Component | Kosten/maand |
|-----------|-------------|
| Hetzner VPS CX31 | ~€12 |
| Backup + domein | ~€3 |
| Alle software (open source) | €0 |
| **Totaal** | **~€15/maand** |
