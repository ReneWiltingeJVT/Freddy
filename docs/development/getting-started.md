# Freddy lokaal opstarten (development)

Stap-voor-stap guide om de volledige Freddy stack lokaal te draaien en te testen.

## Vereisten

| Tool | Versie | Doel |
|------|--------|------|
| [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) | 9.0+ | Backend API |
| [Node.js](https://nodejs.org/) | 18+ (22 aanbevolen) | React frontend |
| [Docker Desktop](https://www.docker.com/products/docker-desktop/) | Latest | PostgreSQL + Seq |
| [Ollama](https://ollama.ai/) | Latest | Lokaal AI-model |

## Stap 1: Infrastructuur starten

Start PostgreSQL, Ollama en Seq via Docker Compose:

```powershell
cd c:\repos\Freddy
docker compose -f infra/docker-compose.yml up -d
```

Controleer of alles draait:

```powershell
docker ps
```

Je zou drie containers moeten zien: `freddy-db`, `freddy-ollama`, `freddy-seq`.

> **Tip:** Als je Ollama al los geïnstalleerd hebt (niet via Docker), kun je die ook gebruiken. Zorg dat hij draait op `http://localhost:11434`. Verwijder in dat geval de `ollama` service uit docker-compose of start alleen PostgreSQL:
>
> ```powershell
> docker compose -f infra/docker-compose.yml up -d postgres seq
> ```

## Stap 2: AI-model downloaden

Download het Mistral 7B model (≈4 GB, eenmalig):

```powershell
# Als Ollama via Docker draait:
docker exec freddy-ollama ollama pull mistral:7b

# Of als Ollama los geïnstalleerd is:
ollama pull mistral:7b
```

Verifieer dat het model beschikbaar is:

```powershell
# Docker:
docker exec freddy-ollama ollama list

# Of lokaal:
ollama list
```

Je zou `mistral:7b` in de lijst moeten zien.

## Stap 3: Database migratie

Pas de EF Core migratie toe om de database-tabellen aan te maken:

```powershell
cd c:\repos\Freddy
dotnet ef database update --project src/Freddy.Infrastructure --startup-project src/Freddy.Api
```

> **Eerste keer?** De tool `dotnet-ef` is al geconfigureerd in `.config/dotnet-tools.json`. Herstel hem met:
>
> ```powershell
> dotnet tool restore
> ```

## Stap 4: Backend API starten

```powershell
cd c:\repos\Freddy
dotnet run --project src/Freddy.Api
```

De API start op **<http://localhost:5000>**.

Test of de API draait:

```powershell
# In een nieuw terminal:
curl http://localhost:5000/swagger
```

Of open <http://localhost:5000/swagger> in je browser voor de Swagger UI.

## Stap 5: React frontend starten

Open een **nieuw terminal**:

```powershell
cd c:\repos\Freddy\apps\Freddy.Web
npm install    # alleen de eerste keer nodig
npm run dev
```

De frontend start op **<http://localhost:5173>**.

## Stap 6: Testen

Open <http://localhost:5173> in je browser. Dit zou je moeten zien:

1. **Welkomstpagina** met "Welkom bij Freddy" en een tekstveld
2. Typ een vraag (bijv. "Wat is een zorgplan?") en klik **Verstuur**
3. Freddy maakt automatisch een nieuw gesprek aan en stuurt je vraag naar het AI-model
4. In de linkerzijbalk verschijnt het gesprek

### Wat er achter de schermen gebeurt

```
Browser (React)
  → POST /api/v1/auth/dev-token        ← JWT token (automatisch)
  → POST /api/v1/chat/conversations     ← Nieuw gesprek
  → POST /api/v1/chat/conversations/{id}/messages
      → API slaat bericht op in PostgreSQL
      → API stuurt context naar Ollama (mistral:7b)
      → Ollama genereert antwoord
      → API slaat antwoord op in PostgreSQL
  ← Antwoord terug naar browser
```

### Handmatig testen via Swagger

Je kunt de API ook rechtstreeks testen via Swagger UI:

1. Open <http://localhost:5000/swagger>
2. Roep eerst `POST /api/v1/auth/dev-token` aan → kopieer het `token`
3. Klik op **Authorize** (hangslot-icoon) → vul in: `Bearer <token>`
4. Nu kun je alle endpoints aanroepen:
   - `POST /api/v1/chat/conversations` — maak een gesprek
   - `GET /api/v1/chat/conversations` — lijst gesprekken
   - `POST /api/v1/chat/conversations/{id}/messages` — stuur een bericht
   - `GET /api/v1/chat/conversations/{id}/messages` — haal berichten op

## Logs bekijken

- **Console:** De API logt naar de terminal waar `dotnet run` draait
- **Seq dashboard:** Open <http://localhost:8081> voor gestructureerde logs (als Seq draait via Docker)

## Stoppen

```powershell
# Frontend: Ctrl+C in het npm terminal
# Backend: Ctrl+C in het dotnet terminal
# Infrastructuur:
docker compose -f infra/docker-compose.yml down
```

Data blijft bewaard in Docker volumes. Om alles te wissen:

```powershell
docker compose -f infra/docker-compose.yml down -v
```

## Problemen oplossen

| Probleem | Oplossing |
|----------|----------|
| `Connection refused` op poort 5432 | PostgreSQL container draait niet. Check `docker ps` |
| `Connection refused` op poort 11434 | Ollama draait niet. Start via Docker of `ollama serve` |
| `Model not found` foutmelding | `ollama pull mistral:7b` uitvoeren |
| API start niet op | Check of poort 5000 vrij is: `netstat -an \| findstr 5000` |
| Frontend proxy errors | Check of de API draait op poort 5000 |
| EF migration mislukt | `dotnet tool restore` en probeer opnieuw |
| `npm install` fouten | Verwijder `node_modules` en `package-lock.json`, dan opnieuw `npm install` |

## Configuratie

Alle configuratie staat in `src/Freddy.Api/appsettings.json`:

| Setting | Standaard | Beschrijving |
|---------|-----------|-------------|
| `ConnectionStrings:DefaultConnection` | `Host=localhost;Database=freddy;Username=freddy;Password=freddy_dev_password` | PostgreSQL verbinding |
| `AI:Endpoint` | `http://localhost:11434` | Ollama API URL |
| `AI:ModelId` | `mistral:7b` | AI-model naam |
| `Jwt:Key` | Dev signing key | JWT handtekening (wijzig voor productie!) |
| `Cors:AllowedOrigins` | `http://localhost:5173` | Toegestane frontend URL |
