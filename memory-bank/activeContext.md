# Active Context

## Current Work Focus

Backoffice API implementation is complete (Phase 8). All CRUD operations for packages and documents are working, tested, and verified end-to-end.

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
