## ADR-0001: LLM Provider Keuze

**Status:** Accepted
**Datum:** 2026-02-28
**Besluitnemers:** Architectuurteam

### Context

Freddy heeft een LLM nodig voor het genereren van antwoorden op basis van opgehaalde
documentfragmenten (RAG). De keuze beïnvloedt kosten, privacy, snelheid en schaalbaarheid.

### Overwogen Opties

| Optie | Kosten | Privacy | Snelheid | Complexiteit |
|-------|--------|---------|----------|-------------|
| **Azure OpenAI** | €100+/mnd | Goed (EU) | Zeer snel | Laag |
| **OpenAI API** | €50+/mnd | Matig (US) | Zeer snel | Laag |
| **Ollama (self-hosted)** | €0 | Uitstekend (lokaal) | Matig (CPU) | Medium |
| **Groq API** | €0 (free tier) | Matig (US) | Extreem snel | Laag |
| **vLLM (self-hosted)** | €0 (+ GPU server) | Uitstekend | Snel | Hoog |

### Besluit

**Primair: Ollama met Mistral 7B op VPS. Fallback: Groq API free tier.**

### Motivatie

- Budget constraint (€50-200/mnd totaal) sluit Azure OpenAI uit
- Privacy: alle data blijft op eigen server (sterk argument voor zorgsector)
- Groq als fallback voor snelheid wanneer Ollama CPU te traag is
- Semantic Kernel abstractie maakt provider-switch triviaal

### Consequenties

- **Positief**: geen LLM-kosten, maximale privacy, provider-agnostisch via Semantic Kernel
- **Negatief**: CPU inference langzamer dan cloud API (~5-10 tok/s), beperkt tot kleinere modellen
- **Risico**: als Groq free tier stopt, is CPU-only fallback acceptabel maar langzamer
- **Actie**: benchmark Mistral 7B op CX31 VPS tijdens week 1 van MVP
