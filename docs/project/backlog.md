## Product Backlog

### Epics

| Epic | Beschrijving | Fase | Prioriteit |
|------|-------------|------|-----------|
| E01 | Project Setup & Infrastructuur | 1 | Must have |
| E02 | Authenticatie & Gebruikersbeheer | 1 | Must have |
| E03 | Chat Backend | 1 | Must have |
| E04 | AI / RAG Pipeline | 1 | Must have |
| E05 | Chat Frontend (React PWA) | 1 | Must have |
| E06 | Document Ingestie | 1 | Must have |
| E07 | Testing & Quality | 1 | Must have |
| E08 | Deployment & Ops | 1 | Must have |
| E09 | Feedback & Analytics | 2 | Should have |
| E10 | Native App (React Native, optioneel) | 2 | Should have |
| E11 | Backoffice | 3 | Could have |
| E12 | Multi-tenant | 4 | Won't have (MVP) |

### Features per Epic (MVP)

#### E01 — Project Setup

- [ ] Solution structure met Clean Architecture
- [ ] Docker Compose (PostgreSQL + pgvector + Ollama + Seq)
- [ ] CI/CD pipeline (GitHub Actions)
- [ ] EditorConfig + analyzers + formatting
- [ ] Development environment documentatie

#### E02 — Authenticatie

- [ ] ASP.NET Identity setup met PostgreSQL
- [ ] JWT token generatie (access + refresh)
- [ ] Login / register endpoints
- [ ] Role-based authorization (User, TeamLead)
- [ ] Token refresh flow

#### E03 — Chat Backend

- [ ] Conversation CRUD endpoints
- [ ] Message endpoints (create + list)
- [ ] MediatR command/query handlers
- [ ] Real-time of polling response mechanism
- [ ] Conversation title auto-generatie

#### E04 — AI / RAG Pipeline

- [ ] Semantic Kernel setup + Ollama connector
- [ ] FAQ template matching (exact + fuzzy)
- [ ] Embedding service (nomic-embed-text)
- [ ] Vector search in pgvector
- [ ] Prompt construction met context
- [ ] Bronvermelding in responses
- [ ] "Weet ik niet" fallback

#### E05 — Chat Frontend

- [ ] Vite + React 19 + TypeScript project setup
- [ ] Tailwind CSS v4 configuratie + design tokens uit Figma
- [ ] shadcn/ui component library setup
- [ ] Chat interface component
- [ ] Conversatie lijst
- [ ] Login/register pagina's
- [ ] Responsive design (mobile-first)
- [ ] PWA manifest + service worker (vite-plugin-pwa)
- [ ] Bronvermelding weergave

#### E06 — Document Ingestie

- [ ] PDF tekst extractie
- [ ] Word (docx) tekst extractie
- [ ] Chunking algoritme (512 tokens, overlap 50)
- [ ] CLI tool of seed script voor initiële documenten
- [ ] Document metadata opslag

#### E07 — Testing & Quality

- [ ] Unit tests voor handlers
- [ ] Golden test set (50 vragen met verwachte antwoorden)
- [ ] Integration tests met Testcontainers
- [ ] Format + lint checks in CI

#### E08 — Deployment

- [ ] Docker images voor API
- [ ] Docker Compose productie configuratie
- [ ] Nginx reverse proxy + TLS
- [ ] VPS provisioning documentatie
- [ ] Database backup script
- [ ] Health check endpoint
