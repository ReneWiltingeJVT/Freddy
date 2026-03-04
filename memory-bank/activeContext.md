# Active Context

## Current Work Focus

Phase 11 (Lightweight LLM + Chitchat) — planning and documentation complete on branch `plan/lightweight-llm-and-chitchat`. This phase optimizes slow-path routing by replacing Mistral 7B with Qwen 2.5 1.5B and adds small talk detection with template responses.

## Recent Changes (Phase 11 — Lightweight LLM & Chitchat Design)

### Documentation Deliverables (Plan Only — No Code)

- `docs/architecture/current-routing-explained.md` — Stakeholder-friendly analysis of current routing: why Mistral 7B is too heavy, missing inference parameters, no small talk handling, latency issues
- `docs/architecture/adr/0005-lightweight-llm.md` — ADR proposing Qwen 2.5 1.5B (1.5B params) to replace Mistral 7B (7B params) for slow-path classification. Includes model comparison, config recommendations (temperature 0.1, num_predict 128, num_ctx 2048, timeout 15s)
- `docs/mvp/chitchat-design.md` — Complete design for SmallTalkDetector: 5 categories (Greeting, HelpIntent, Thanks, Farewell, GenericConfusion), deterministic word-list detection, hardcoded template responses, Mermaid flow diagram, metrics & validation targets, concrete implementation checklist
- `docs/solution/freddy-mvp-solution-overview.md` — Full MVP solution overview (v1.1) updated with small talk layer, lightweight LLM rationale, new latency targets
- `docs/architecture/adr/README.md` — Added ADR-0005 to index

### Key Decisions

- **Model**: Qwen 2.5 1.5B (1.0 GB RAM, ~30-50 tok/s CPU) replaces Mistral 7B (4.5 GB RAM, ~5-10 tok/s CPU)
- **Small talk**: Pure deterministic detection (word lists) — no LLM dependency, <1ms, 100% predictable
- **Pipeline order**: SmallTalk → FastPath → SlowPath (LLM only for multi-ambiguous cases)
- **Inference settings**: Temperature 0.1, MaxTokens 128, Timeout 15s (currently: no settings, 5min timeout)
- **Target latency**: slow-path < 1.5s (was 3-10s), end-to-end p95 < 2s

## Recent Changes (Phase 10 — Fast-Path Routing)

### Two-Lane Routing Architecture

- `PackageCandidate` — Extended with `Tags` and `Synonyms` (IReadOnlyList<string>) to enable deterministic matching
- `IFastPathRouter` — New interface in Application layer for deterministic keyword scoring
- `ScoredCandidate` — New record pairing a PackageCandidate with its fast-path score
- `FastPathRouter` — Deterministic router implementation: title (1.0/0.7), tags (0.6), synonyms (0.6), partial (0.3), description (0.2)
- `CompositePackageRouter` — Orchestrates fast-path → optional Ollama: ≥0.6 direct, 0.3-0.6 single→confirm, 0.3-0.6 multi→Ollama, <0.3 no match
- `RoutingOptions` — Configurable thresholds (HighConfidenceThreshold=0.6, AmbiguityFloorThreshold=0.3) via appsettings.json
- `DependencyInjection` — Updated: IPackageRouter→CompositePackageRouter, IFastPathRouter→FastPathRouter, OllamaPackageRouter registered directly
- `SendMessageCommandHandler` — Updated PackageCandidate construction to include Tags and Synonyms from Package entity

### Test Coverage

- 11 new FastPathRouter tests (exact title, title-in-message, tag match, synonym match, partial, no match, ordering, case-insensitive, description overlap, real-world seed data)
- 6 new CompositePackageRouter tests (no candidates, high confidence, medium confidence, no scores, below floor, multiple ambiguous)
- All 41 tests passing (24 existing + 17 new)

## Recent Changes (Phase 9 — Backoffice Web App, Chat Fixes, Mock Data)

### Chat Fixes

- `PackageRouterResult` — Added `IsServiceUnavailable` bool property
- `OllamaPackageRouter` — Split error handling: HttpRequestException → service unavailable, TaskCanceledException (timeout) → service unavailable, generic Exception → fallback
- `SendMessageCommandHandler` — Added `IDocumentRepository` dependency; checks `IsServiceUnavailable` first and returns Dutch error message; loads and formats documents in high-confidence responses (📎 **Documenten:** section with markdown links)

### File Upload Infrastructure

- `IFileStorageService` — Interface: `UploadAsync(Stream, fileName, ct)`, `DeleteAsync(fileUrl, ct)`
- `LocalFileStorageService` — Stores files in `wwwroot/uploads/documents/{guid}-{sanitizedName}`, returns relative URL
- `AdminDocumentsController` — Added `POST upload` endpoint (multipart/form-data, 50MB limit, auto-detect document type)
- `DeleteDocumentCommandHandler` — Now also calls `IFileStorageService.DeleteAsync` when deleting documents with files
- `DependencyInjection` — `AddFileStorage(string webRootPath)` extension for DI registration
- `Program.cs` — Added `UseStaticFiles()` and `AddFileStorage()` registration

### Mock Data

- Migration `20260301191129_SeedMockPackagesAndDocuments` seeds:
  - Voedselbank (existing): updated synonyms/tags, added 2 documents (PDF + XLSX)
  - Medicatie in Beheer (new): full protocol content, 2 documents (PDF + XLSX)
  - Valpreventie (new): full protocol content, 2 documents (PDF + Steps with JSON)
- 5 placeholder seed-document files in `wwwroot/seed-documents/`

### Backoffice Web App (NEW)

- `apps/Freddy.Backoffice/` — Vite + React 19 + TypeScript + Tailwind CSS (port 5174)
- Same dependency stack as Freddy.Web: ky, @tanstack/react-query, react-router-dom, zod, react-hook-form
- `lib/adminApi.ts` — ky client with X-Admin-Api-Key header, all CRUD + upload endpoints
- `types/admin.ts` — TypeScript interfaces matching backend DTOs
- `hooks/usePackages.ts` — React Query hooks for package CRUD, publish/unpublish
- `hooks/useDocuments.ts` — React Query hooks for document CRUD + file upload
- `components/Layout.tsx` — Header with nav, main content area, footer
- `features/packages/PackageListPage.tsx` — Table view with search, publish toggle, delete
- `features/packages/PackageFormPage.tsx` — Create/edit form with all package fields
- `features/packages/PackageDetailPage.tsx` — Detail view with document list and file upload area

### Configuration

- CORS: Added `http://localhost:5174` to allowed origins
- Upload directory: `wwwroot/uploads/documents/` with .gitignore

## Build & Test Status

- **Build**: 0 errors, 0 warnings
- **Tests**: 24/24 passed (+2 new: DocumentLinks, ServiceUnavailable)
- **TypeScript**: Backoffice compiles and builds (14.59KB CSS, 334KB JS)
- **Seeded data**: 3 published packages with 6 documents total, verified via API

## Next Steps

- **Phase 11 implementation** — execute the implementation checklist from chitchat-design.md:
  1. Pull qwen2.5:1.5b model in Ollama
  2. Update appsettings.json AI:ModelId
  3. Add PromptExecutionSettings to OllamaPackageRouter
  4. Reduce HttpClient timeout to 15s
  5. Create ISmallTalkDetector interface + SmallTalkDetector implementation
  6. Integrate SmallTalkDetector into SendMessageCommandHandler
  7. Add structured logging (routing.lane, routing.latency_ms)
  8. Write SmallTalkDetector unit tests (50+ positive, 20+ negative cases)
  9. Update existing routing tests
- User authentication beyond API key
- Production deployment configuration
- Additional test coverage (integration tests)
- Document search/filtering
- Rich text editing for package content

## Recent Changes (Phase 8 — Backoffice API)

### Entity Layer

- Package entity evolved: `Name→Title`, `Keywords→Tags`, `IsActive→IsPublished` (default false), added `Synonyms`, `RequiresConfirmation`, `Documents` navigation
- Created Document entity with DocumentType enum (Pdf/Steps/Link)

### Infrastructure Layer

- Updated PackageConfiguration with column renames, GIN indexes for tags/synonyms
- Created DocumentConfiguration with snake_case, jsonb for steps_content
- Updated PackageRepository: renamed methods, added CRUD, filter support
- Created DocumentRepository with full CRUD

### Application Layer

- 28 CQRS files for Admin Packages and Documents
- Commands: Create/Update/Delete/Publish/Unpublish packages, Create/Update/Delete documents
- Queries: GetPackage, ListPackages (with filters), ListDocuments
- All with validators for Create/Update operations

### API Layer

- AdminApiKeyMiddleware: path-based auth for `/api/admin/` routes
- AdminPackagesController: 7 endpoints (list, get, create, update, delete, publish, unpublish)
- AdminDocumentsController: 4 endpoints (list, create, update, delete) nested under packages
- Request models: CreatePackageRequest, UpdatePackageRequest, CreateDocumentRequest, UpdateDocumentRequest

### Chat Integration

- SendMessageCommandHandler uses `GetAllPublishedAsync` (only published packages)
- PackageCandidate uses `Title` (was `Name`)
- OllamaPackageRouter updated accordingly

### Migration

- `20260228215243_EvolvePackageAddDocuments`: column renames, new columns, documents table
- Existing packages set to published via SQL migration

## Build & Test Status

- **Build**: 0 errors, 0 warnings
- **Tests**: 22/22 passed
- **E2E**: All endpoints verified (list, create, publish, unpublish, update, delete for both packages and documents, auth rejection)

## Next Steps

- Git commit on `feature/create-backoffice` branch
- Future: Backoffice frontend, more tests, package search improvements
