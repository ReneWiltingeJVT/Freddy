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

### Phase 11 — Lightweight LLM + Chitchat (COMPLETE)

- **Documentation phase**: ADR-0005, chitchat design, solution overview, routing analysis
- **Implementation**: Model swap (Mistral 7B → Qwen 2.5 1.5B), inference params (Temperature 0.1, NumPredict 128), HTTP timeout (5min → 15s configurable)
- SmallTalkDetector: deterministic Dutch word-list detection for 5 categories with template responses
- Handler integration: small talk detected before LLM call, routing lane logged
- Created missing entity/interface stubs for Package/Document that were broken on main
- 61 tests passing (16 Application + 45 AI)
- Branches: `plan/lightweight-llm-and-chitchat` (docs), `feature/lightweight-llm-and-chitchat` (code)

### Phase 12 — MVP Retrieval Redesign (COMPLETE)

- **Documentation phase**: `docs/mvp/retrieval-redesign.md` with 4-phase plan (A-D)
- **Phase A**: Scoring improvements (stopwords, content, doc names, n-gram, description raise), LLM zero-match recovery, top-3 suggestions
- **Phase B**: PackageCategory enum (Protocol/WorkInstruction/PersonalPlan), Admin API category support, category boost in FastPathRouter (+0.1 for PersonalPlan)
- **Phase C**: Client entity + full CRUD, ClientDetector (deterministic alias matching), scoped retrieval in handler, AuditLog entity + repository
- **Phase D**: DocumentChunk entity for future RAG support (forward-compat, no migration)
- **Infrastructure**: EF migration `AddCategoriesClientsAuditLog`, DI registration, CHECK constraint
- 116 tests passing (88 existing + 28 new)
- Branches: `plan/mvp-retrieval-personal-plans-redesign` (docs), `feature/mvp-retrieval-redesign` (code)

### Phase 13 — Retrieval Improvements (COMPLETE)

- **OverviewQueryDetector**: Deterministic Dutch regex detector for 5 query types (count/list/protocol/werkinstructie/plan/listAll). Bypasses routing pipeline entirely.
- **Graceful LLM fallback**: CompositePackageRouter now handles `IsServiceUnavailable` at both disambiguation and zero-match recovery stages — never surfaces to users
- **N+1 elimination**: Single `GetNamesByPackageIdsAsync` batch query replaces N per-package calls
- **PendingClientId persistence**: Client detected in turn 1 persists to DB, available in turn 2+
- **LLM context enrichment**: OllamaPackageRouter now includes top-5 tags and 120-char content snippet in disambiguation prompts
- **`GetAllPublishedByCategoryAsync`**: New repository method for category-scoped package queries
- **Switch fix**: `ConversationPendingState.None` now correctly routes to `RouteAndBuildResponseAsync`
- 141 tests passing (68 AI + 73 Application)
- Branch: `feature/freddy-retrieval-improvement`

## What Works

- Chat: Create conversation, send messages, AI responds with package routing, documents included in responses
- Chat: AI unavailability at routing level handled gracefully — suggestions or top match returned instead of error
- Chat: Small talk detection with Dutch template responses (greeting, thanks, farewell, help intent, confusion)
- Chat: Client detection — mentions of client names/aliases scope retrieval to include personal plans
- Chat: Client context persists across conversation turns (PendingClientId in DB)
- Chat: Scoped retrieval — PersonalPlan packages only shown when client detected, general packages always included
- Chat: Zero-match recovery with top-3 package suggestions (or deterministic overview answer for count/list questions)
- Chat: Category boost — PersonalPlan packages get +0.1 scoring boost
- Chat: Overview questions (hoeveel/welke/voor meneer X) answered without LLM call
- Packages: Full admin CRUD with publish lifecycle, categories (Protocol/WorkInstruction/PersonalPlan)
- Clients: Full admin CRUD with display names and aliases
- Documents: Full admin CRUD + file upload (multipart, 50MB limit) nested under packages
- Audit logging: Infrastructure in place (entity, repository, EF config)
- Auth: API key middleware for admin routes
- AI: Ollama package classification, Semantic Kernel chat completion (Qwen 2.5 1.5B)
- Frontend (Chat): React chat interface with real-time updates
- Frontend (Backoffice): Package list, create/edit, detail view with document management and file upload
- Static files: Uploaded documents served via UseStaticFiles
- Mock data: 3 published packages (Voedselbank, Medicatie in Beheer, Valpreventie) with documents
- Chat: AI unavailability surfaced as user-friendly Dutch message
- Chat: Small talk detection with Dutch template responses (greeting, thanks, farewell, help intent, confusion)
- Chat: Client detection — mentions of client names/aliases scope retrieval to include personal plans
- Chat: Scoped retrieval — PersonalPlan packages only shown when client detected, general packages always included
- Chat: Zero-match recovery with top-3 package suggestions
- Chat: Category boost — PersonalPlan packages get +0.1 scoring boost
- Packages: Full admin CRUD with publish lifecycle, categories (Protocol/WorkInstruction/PersonalPlan)
- Clients: Full admin CRUD with display names and aliases
- Documents: Full admin CRUD + file upload (multipart, 50MB limit) nested under packages
- Audit logging: Infrastructure in place (entity, repository, EF config)
- Auth: API key middleware for admin routes
- AI: Ollama package classification, Semantic Kernel chat completion (Qwen 2.5 1.5B)
- Frontend (Chat): React chat interface with real-time updates
- Frontend (Backoffice): Package list, create/edit, detail view with document management and file upload
- Static files: Uploaded documents served via UseStaticFiles
- Mock data: 3 published packages (Voedselbank, Medicatie in Beheer, Valpreventie) with documents

## Known Issues

- Edge case: "wat is het protocol als ik beschikbaar ben?" triggers `ExistencePattern` + `isProtocol` → false ListByCategory. Low risk in practice.
- Frontend dev servers (`Freddy.Web`, `Freddy.Backoffice`) exit with code 1 — likely need `npm install`

## Phase 14 — MVP Retrieval Stabilization (COMPLETE)

- **Root cause fixed:** `llama3.1:8b` (30s timeout) was called on every matched-package response
- **`PackageResponseFormatter`**: deterministic Dutch formatter, <5ms, no LLM, no I/O
- **`SendMessageCommandHandler`**: matched packages → formatter only; no-match → qwen2.5:1.5b
- **`ChatResponseGenerator`**: switched to `qwen2.5:1.5b` (keyed: "classifier"), slim prompt
- **`FastPathRouter.ScoreDescription`**: always runs, score 0.4/0.5 (was 0.3 last-resort)
- **`AIOptions`**: `TimeoutSeconds` 30→8, `MaxTokens` 1024→512
- 153 tests green, branch `feature/mvp-retrieval-stabilization`

## What's Left to Build

- Backoffice UI for client management (API is ready)
- Backoffice UI for category selection in package forms
- User authentication (beyond API key)
- Production deployment configuration
- Integration tests
- Document search/filtering in backoffice
- Rich text editing for package content
