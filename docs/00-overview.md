## Freddy — Project Overview

**Versie:** 1.0
**Datum:** 28 februari 2026
**Status:** Strategische Planfase

### Wat is Freddy?

Freddy is een AI-gestuurde chatapplicatie waarmee zorgmedewerkers via natuurlijke taal
protocollen, stappenplannen en procedures kunnen opvragen. Het systeem geeft direct
antwoord met bronvermelding, zodat medewerkers altijd weten welk document het antwoord
onderbouwt.

### Kernprincipes

- **Veilig**: geen patiëntdata, lokale AI-verwerking, geen data naar externe partijen
- **Betrouwbaar**: altijd bronvermelding, expliciet "weet ik niet" bij onzekerheid
- **Eenvoudig**: chat-interface op B1 taalniveau, installeerbaar als app op de telefoon
- **Betaalbaar**: open source stack, ~€15/maand hosting

### Technology Stack

| Laag | Keuze |
|------|-------|
| Frontend | React 19 + TypeScript + Tailwind CSS (PWA) |
| Backend | ASP.NET Core 9 + MediatR + Semantic Kernel |
| Database | PostgreSQL 16 + pgvector |
| AI | Ollama (Mistral 7B) + nomic-embed-text |
| Hosting | Hetzner VPS + Docker Compose |
| CI/CD | GitHub Actions |

### MVP Scope

Werkende pilot in 4-6 weken voor 10-50 zorgmedewerkers bij één organisatie.
Chat-interface, FAQ matching, RAG document retrieval, bronvermelding.

Zie de individuele documenten voor volledige details per onderwerp.
