# Retrieval Improvements — Feature Branch `feature/freddy-retrieval-improvement`

## Summary

This document describes the improvements made to Freddy's package retrieval pipeline to make it robust against two major failure modes:

1. **Overview/count questions** ("hoeveel protocollen zijn er?") previously fell into the LLM zero-match path and timed out or returned empty results.
2. **Ollama unavailability** caused "AI-service niet beschikbaar" error messages to surface to users, rather than degrading gracefully.

---

## Changes

### 1. OverviewQueryDetector (new)

**Files:**
- `src/Freddy.Application/Common/Interfaces/IOverviewQueryDetector.cs`
- `src/Freddy.Infrastructure/AI/OverviewQueryDetector.cs`

**What it does:**
Regex-based, deterministic detector for Dutch overview queries. Detects four query types without any LLM call:

| `OverviewQueryType` | Example query |
|---|---|
| `CountByCategory` | "hoeveel protocollen zijn er?" |
| `ListByCategory` | "welke werkinstructies zijn er beschikbaar?" |
| `PersonalPlansForClient` | "welke plannen zijn er voor meneer van het Hout?" |
| `ListAll` | "welke pakketten ken je?" |

Extracting `ClientNameHint` from "voor meneer/mevrouw X" phrases or capitalized names.

Returns `OverviewQueryIntent.None` for all other messages — no false positives affect the normal routing path.

**Where it runs:**
Step 3 of `SendMessageCommandHandler.Handle()`, after small-talk detection but before pending-state dispatch. Returns a formatted Dutch response immediately without entering the routing pipeline.

---

### 2. Graceful LLM Fallback in CompositePackageRouter

**File:** `src/Freddy.Infrastructure/AI/CompositePackageRouter.cs`

**Problem:** When Ollama was unreachable, `IsServiceUnavailable = true` propagated all the way to the user as an error message.

**Fix:**

- **Disambiguation path** (2+ ambiguous fast-path candidates → Ollama): if Ollama returns `IsServiceUnavailable`, return the top fast-path candidate with `NeedsConfirmation = true`.
- **Zero-match recovery path** (no fast-path candidates above floor → Ollama): if Ollama returns `IsServiceUnavailable`, return top-3 fast-path suggestions instead of the error.

**Result:** `IsServiceUnavailable` never reaches the handler except as a last-resort defensive guard.

---

### 3. Batch Document Name Loading (N+1 Elimination)

**Files:**
- `src/Freddy.Application/Common/Interfaces/IDocumentRepository.cs` — added `GetNamesByPackageIdsAsync`
- `src/Freddy.Infrastructure/Persistence/Repositories/DocumentRepository.cs` — implemented

**Before:** For each package candidate, the handler called `GetByPackageIdAsync(packageId)` — N database round-trips per chat message.

**After:** Single `WHERE PackageId IN (...)` query returns `Dictionary<Guid, List<string>>` for all candidates at once.

---

### 4. PendingClientId Persistence

**Files:**
- `src/Freddy.Application/Common/Interfaces/IConversationRepository.cs` — added `SetPendingClientIdAsync`
- `src/Freddy.Infrastructure/Persistence/Repositories/ConversationRepository.cs` — implemented

**Problem:** Client detected in turn 1 ("Ik zoek iets voor meneer Jansen") was forgotten in turn 2 ("wat zijn zijn plannen?").

**Fix:** When a new client is detected in `RouteAndBuildResponseAsync`, `SetPendingClientIdAsync` persists it to the conversation row. Subsequent turns read `conversation.PendingClientId` as `effectiveClientId` if no client is mentioned in the current message.

---

### 5. LLM Context Enrichment

**File:** `src/Freddy.Infrastructure/AI/OllamaPackageRouter.cs`

`FormatCandidates()` now includes:
- Top 5 tags for each candidate (was: title + description only)
- First 120 characters of package content as a snippet

This gives the LLM more signal for disambiguation, reducing incorrect routing choices.

---

### 6. GetAllPublishedByCategoryAsync

**Files:**
- `src/Freddy.Application/Common/Interfaces/IPackageRepository.cs` — added `GetAllPublishedByCategoryAsync`
- `src/Freddy.Infrastructure/Persistence/Repositories/PackageRepository.cs` — implemented

Used by `TryHandleOverviewQueryAsync` to fetch all packages of a specific category for count/list responses.

---

## Architecture Impact

The retrieval pipeline now has **deterministic answers at every exit point**:

```
User message
    │
    ▼
SmallTalkDetector ──→ small talk reply (no routing)
    │
    ▼
OverviewQueryDetector ──→ count/list/plan answer (no routing, no LLM)
    │
    ▼
PendingState dispatch
    │
    ▼
RouteAndBuildResponseAsync
    │
    ├─ FastPathRouter (deterministic, <10ms)
    │       │
    │       ├─ ≥HighConfidence → direct answer
    │       ├─ single ambiguity → answer + confirmation
    │       └─ multiple ambiguity → Ollama disambiguation
    │                   │
    │                   └─ Ollama unavailable → top fast-path candidate + confirmation
    │
    └─ Zero-match → Ollama recovery
                │
                └─ Ollama unavailable → top-3 suggestions
```

LLM is now an **enhancement**, not a **gatekeeper**. Freddy always responds.

---

## Tests

| Test file | New tests |
|---|---|
| `tests/Freddy.AI.Tests/OverviewQueryDetectorTests.cs` | 23 tests covering all query types + non-overview messages |
| `tests/Freddy.Application.Tests/.../CompositePackageRouterTests.cs` | 2 graceful fallback tests |

Total: 141 tests, all passing.
