# Active Context

## Current Work Focus

Phase 13 (Retrieval Improvements) — implementation complete and pushed on branch `feature/freddy-retrieval-improvement`. All 141 tests passing. Ready for PR review.

## Recent Changes (Phase 13 — Retrieval Improvements)

### Overview Query Fast-Path (no LLM)

- **`IOverviewQueryDetector`**: Interface + `OverviewQueryType` enum (None/CountByCategory/ListByCategory/PersonalPlansForClient/ListAll) + `OverviewQueryIntent` record with QueryType, Category, ClientNameHint
- **`OverviewQueryDetector`**: Regex-based Dutch detector using `[GeneratedRegex]` source generators. Handles count/list/protocol/werkinstructie/plan/package queries deterministically
- **`SendMessageCommandHandler`**: Step 3 now calls `overviewQueryDetector.Detect()` before pending-state dispatch. `TryHandleOverviewQueryAsync` provides formatted Dutch responses for all 4 query types
- **`CategoryDisplayName()`**: Static helper mapping `PackageCategory` to Dutch display names

### Graceful LLM Fallback

- **`CompositePackageRouter`**: When Ollama returns `IsServiceUnavailable` during disambiguation → returns top fast-path candidate with `NeedsConfirmation = true`. During zero-match recovery → returns top-3 suggestions. `IsServiceUnavailable` no longer surfaces to users.

### N+1 Elimination

- **`IDocumentRepository.GetNamesByPackageIdsAsync`**: New method returns `Dictionary<Guid, List<string>>` for a batch of package IDs
- **`DocumentRepository`**: Implemented with single `WHERE PackageId IN (...)` EF Core query
- **`SendMessageCommandHandler`**: Replaced per-package `GetByPackageIdAsync` loop with single batch call

### PendingClientId Persistence

- **`IConversationRepository.SetPendingClientIdAsync`**: New method persists detected client across conversation turns
- **`ConversationRepository`**: Implemented with `ExecuteUpdateAsync`
- **`SendMessageCommandHandler`**: Reads `conversation.PendingClientId` as fallback when no client in current message; calls `SetPendingClientIdAsync` when new client detected

### LLM Context Enrichment

- **`OllamaPackageRouter.FormatCandidates()`**: Now includes top 5 tags and first 120 chars of content in addition to title/description

### Repository Extension

- **`IPackageRepository.GetAllPublishedByCategoryAsync`**: New method for category-filtered queries
- **`PackageRepository`**: Implemented with `WHERE IsPublished AND Category = X ORDER BY Title`

### Test Coverage

- **`OverviewQueryDetectorTests.cs`**: 23 new tests covering all query types + non-overview messages (Dutch)
- **`CompositePackageRouterTests.cs`**: 2 new graceful fallback tests with `HttpRequestException` mocking
- **`SendMessageCommandHandlerTests.cs`**: Updated constructor with `IOverviewQueryDetector` mock; default `GetNamesByPackageIdsAsync` mock; updated `Handle_ServiceUnavailable` test to match new user-friendly fallback behavior
- **Total: 141 tests, all passing**

## Build & Test Status

- **Build**: Application + Infrastructure: 0 errors, 0 warnings
- **Tests**: 141/141 passed (68 AI + 73 Application)
- **Branch**: `feature/freddy-retrieval-improvement` pushed to origin

## Next Steps

- Create PR: `feat: improve package retrieval — overview queries, LLM fallback resilience, client context persistence`
- PR review and merge
- Phase 14: User authentication beyond API key
- Phase 14: Integration tests for overview query responses
- SmallTalkDetector: Consider removing "ik heb een vraag" from `HelpIntentPhrases` (too broad — would swallow legitimate questions if user says exactly that phrase, though compound phrases like "ik heb een vraag over wassen" are safe due to length check)



## Recent Changes (Phase 12 — MVP Retrieval Redesign)

### Phase A — Scoring Improvements (FastPathRouter)

- **Stopword filtering**: Dutch stopwords excluded from scoring to reduce noise
- **Content overlap scoring**: New scoring dimension for package content words (0.15 for ≥3 overlap words)
- **Document name scoring**: New scoring dimension for document names (0.15 for overlap)
- **N-gram similarity**: Bigram similarity scoring for fuzzy partial matches (threshold 0.3)
- **Description overlap raised**: From 0.2 to 0.3

### Phase A — LLM Zero-Match Recovery + Suggestions

- **CompositePackageRouter**: Complete rewrite with `HandleZeroMatchRecoveryAsync` for LLM fallback when no FastPath match
- **`SuggestedPackage` record**: Title + Description for top-3 suggestions
- **`PackageRouterResult.SuggestedPackages`**: Populated when no match found
- **`SendMessageCommandHandler.BuildSuggestionResponse()`**: Formats top-3 suggestions for user

### Phase B — Package Categories

- **`PackageCategory` enum**: Protocol (0), WorkInstruction (1), PersonalPlan (2)
- **Package entity**: Added `Category` and `ClientId` properties with Client navigation
- **Admin API**: DTOs, Commands, Validators, Controller, and QueryHandlers all updated with Category/ClientId
- **Validators**: Category must be Protocol/WorkInstruction/PersonalPlan; PersonalPlan requires ClientId
- **Repository**: `GetAllAsync` accepts optional `PackageCategory?` filter; new `GetPublishedByClientIdAsync`
- **EF Config**: CHECK constraint `ck_packages_category_client`, indexes on category and client_id

### Phase B — Category Boost

- **FastPathRouter**: +0.1 score boost for PersonalPlan packages (capped at 1.0, only on nonzero scores)

### Phase C — Client Entity + CRUD

- **Client entity**: Id, DisplayName, Aliases (text[]), IsActive, timestamps
- **Full CQRS**: List/Get/Create/Update/Delete with Commands, Handlers, Validators, DTOs
- **AdminClientsController**: Full REST CRUD at `api/admin/clients`
- **ClientRepository**: CRUD + `FindByAliasAsync` with `EF.Functions.ILike`
- **EF Config**: GIN index on aliases, display_name index, is_active index

### Phase C — Client Detection + Scoped Retrieval

- **IClientDetector + ClientDetector**: Deterministic alias matching — longest display name first, then longest alias (≥3 chars), case-insensitive
- **SendMessageCommandHandler**: Rewrote `RouteAndBuildResponseAsync` — when client detected, merges general packages (non-PersonalPlan) + client-specific PersonalPlan; when no client, excludes PersonalPlan entirely

### Phase C — Audit Logging Infrastructure

- **AuditLog entity**: Id, UserId (Guid), Action, EntityType, EntityId, Details (jsonb), Timestamp
- **IAuditLogRepository + AuditLogRepository**: LogAsync and GetByEntityAsync
- **EF Config**: Indexes on user_id, entity_type, timestamp

### Phase D — Forward Compatibility

- **DocumentChunk entity**: Prepared for future RAG support (no migration yet)

### Infrastructure

- **DI Registration**: IClientRepository, IAuditLogRepository, IClientDetector all registered as scoped
- **EF Migration**: `AddCategoriesClientsAuditLog` — clients table, audit_logs table, packages.category + client_id columns, conversations.pending_client_id, CHECK constraint, FK, indexes
- **Test Coverage**: 116 tests passing (28 new tests added for category boost, client detection, client validation, client handler, package category validation)

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
