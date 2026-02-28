## Risicoanalyse

### Technische Risico's

| Risico | Impact | Kans | Mitigatie |
|--------|--------|------|----------|
| Ollama CPU inference te traag | Hoog | Medium | Groq API fallback, caching, FAQ-first |
| Tweede tech-stack (Node.js toolchain) | Laag | Hoog | Developer heeft React/TS ervaring; CI valideert beide stacks |
| TypeScript/C# DTO sync | Laag | Medium | OpenAPI code generation optioneel; handmatig bij <20 DTOs |
| pgvector schaalt niet | Laag | Laag | Migreer naar Qdrant bij >100K chunks |
| VPS downtime | Medium | Laag | Hetzner SLA, backups, monitoring |
| Solo dev beschikbaarheid | Hoog | Medium | Goede docs, clean code |

### Juridische Risico's

| Risico | Impact | Kans | Mitigatie |
|--------|--------|------|----------|
| AVG-overtreding | Hoog | Laag | Minimale data, retentiebeleid |
| Aansprakelijkheid verkeerd advies | Hoog | Laag | Disclaimers, "weet ik niet", alleen protocollen |
| NEN 7510 non-compliance | Medium | Laag | Geen patiëntdata, basis security |
| Data breach | Hoog | Laag | Encryptie, lokaal LLM |

### AI-Betrouwbaarheid Risico's

| Risico | Impact | Kans | Mitigatie |
|--------|--------|------|----------|
| Hallucinaties | Hoog | Medium | Strikte prompt, threshold, bronvermelding |
| Verkeerde intentie-detectie | Medium | Medium | FAQ-first, fuzzy thresholds |
| Onveilig advies | Hoog | Laag | Hardcoded checks, review cyclus |
| Slechte Nederlandse embeddings | Medium | Medium | Testen, upgrade model |
| Verouderde antwoorden | Medium | Medium | Document versioning |

### Kosten Risico's

| Risico | Impact | Kans | Mitigatie |
|--------|--------|------|----------|
| Groq stopt free tier | Medium | Medium | Ollama fallback, abstractie laag |
| VPS kosten bij groei | Laag | Laag | Stapsgewijs upgraden |
| Onderhoudskosten | Medium | Medium | Clean code, tests, docs |
