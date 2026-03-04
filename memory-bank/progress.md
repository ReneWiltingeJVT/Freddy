# Progress

## Completed Phases

### Phase 1 — Architecture Planning

- Documentation suite (docs/00-10), technical architecture decisions

### Phase 2 — Documentation & Repository Setup

- Clean Architecture solution structure, base infrastructure

### Phase 3 — Frontend Pivot to React

- React 19 + TypeScript + Vite 6 + Tailwind CSS frontend

### Phase 4 — MVP Vertical Slice

- Chat feature end-to-end: conversation CRUD, message sending, AI integration
- Package entity with seed data (Voedselbank)
- Ollama integration with Semantic Kernel

### Phase 5 — Startup & E2E Testing

- Docker infrastructure, startup verification, end-to-end test flow

### Phase 6 — Package-Driven Routing

- Ollama JSON classifier for routing messages to packages
- Package matching with keywords

### Phase 7 — UI/UX Fixes

- Optimistic messages, timeout handling, delete conversation

### Phase 8 — Backoffice Packages API

- Full CRUD for packages and documents
- Publish/unpublish lifecycle
- Admin API key authentication
- Chat integration (only published packages visible)

### Phase 9 — Backoffice Web App + Chat Fixes + Mock Data (COMPLETE)

- Backoffice React app (Vite + Tailwind, port 5174) with package management and document upload
- Chat error handling: AI unavailability detection, Dutch error messages
- Document inclusion in chat responses for matched packages
- File upload infrastructure (LocalFileStorageService, multipart endpoint)
- 3 mock packages seeded with 6 documents total
- 24 tests passing, 0 warnings
- Committed on `feature/total-backoffice` branch

### Phase 10 — Fast-Path Routing (COMPLETE)

- Two-lane routing: deterministic fast-path (<10ms) + Ollama disambiguation (slow-path, only for ambiguous cases)
- FastPathRouter scores candidates via title/tag/synonym/description matching
- CompositePackageRouter orchestrates fast-path→Ollama decision flow
- PackageCandidate extended with Tags and Synonyms from Package entity
- Configurable thresholds via appsettings.json (Routing section)
- 41 tests passing (24 existing + 17 new)
- Branch: `feature/fast-path-routing`

### Phase 11 — Lightweight LLM + Chitchat Design (CURRENT — PLAN COMPLETE)

- Documentation-only phase: analysis, ADR, design docs, solution overview
- ADR-0005: Replace Mistral 7B with Qwen 2.5 1.5B for slow-path classification
- Chitchat design: deterministic small talk detection + template responses
- New conversation flow: SmallTalk → FastPath → Lightweight LLM
- Concrete implementation checklist ready for next phase
- Branch: `plan/lightweight-llm-and-chitchat`

## What Works

- Chat: Create conversation, send messages, AI responds with package routing, documents included in responses
- Chat: AI unavailability surfaced as user-friendly Dutch message
- Packages: Full admin CRUD with publish lifecycle
- Documents: Full admin CRUD + file upload (multipart, 50MB limit) nested under packages
- Auth: API key middleware for admin routes
- AI: Ollama package classification, Semantic Kernel chat completion
- Frontend (Chat): React chat interface with real-time updates
- Frontend (Backoffice): Package list, create/edit, detail view with document management and file upload
- Static files: Uploaded documents served via UseStaticFiles
- Mock data: 3 published packages (Voedselbank, Medicatie in Beheer, Valpreventie) with documents

## Known Issues

- None currently blocking

## What's Left to Build

- **Phase 11 implementation**: model swap, inference params, SmallTalkDetector, pipeline integration, tests
- User authentication (beyond API key)
- Production deployment configuration
- Additional test coverage (integration tests)
- Document search/filtering in backoffice
- Rich text editing for package content
