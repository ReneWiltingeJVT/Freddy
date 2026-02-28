## ADR-0002: Vector Store Keuze

**Status:** Accepted
**Datum:** 2026-02-28
**Besluitnemers:** Architectuurteam

### Context

Freddy gebruikt RAG (Retrieval-Augmented Generation) waarvoor vectoropslag nodig is
om document-embeddings te bewaren en te doorzoeken.

### Overwogen Opties

| Optie | Kosten | Complexiteit | .NET Support | Schaalbaarheid |
|-------|--------|-------------|-------------|---------------|
| **pgvector** (in PostgreSQL) | €0 | Laag (1 DB) | Goed (Npgsql) | Tot ~100K chunks |
| **Qdrant** | €0 (self-hosted) | Medium (extra service) | Goed | Miljarden vectors |
| **Weaviate** | €0 (self-hosted) | Medium | Matig | Zeer goed |
| **Pinecone** | €70+/mnd | Laag (managed) | Goed | Onbeperkt |
| **ChromaDB** | €0 | Medium | Matig | Beperkt |

### Besluit

**pgvector als extensie in PostgreSQL.**

### Motivatie

- Eén database voor alles: applicatiedata + vectors
- Geen extra service om te beheren (solo developer)
- Voldoende voor MVP scope (~20 docs → ~2000 chunks)
- Schaalt tot ~100K chunks, meer dan genoeg voor voorzienbare toekomst
- Semantic Kernel heeft native PostgreSQL connector

### Consequenties

- **Positief**: simpelste mogelijke setup, geen extra infra, gratis
- **Negatief**: niet de snelste voor miljoenen vectors
- **Migratie**: als we >100K chunks bereiken, migreren naar Qdrant; embeddings zijn
  herbruikbaar, alleen opslag verandert
- **Actie**: monitor query latency in productie; bij >200ms migreerplan opstellen
