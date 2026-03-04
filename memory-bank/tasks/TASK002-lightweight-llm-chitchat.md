# [TASK002] — Lightweight LLM & Chitchat Design

**Status:** In Progress
**Added:** 2026-03-04
**Updated:** 2026-03-04

## Original Request

Replace the current Mistral 7B model used for slow-path routing with a lightweight 1B–3B
model. Add a SmallTalkService that handles greetings, thanks, help-intent and generic
confusion via deterministic detection + template responses. Document everything in ADRs and
design docs. No code changes — plan and documentation only.

## Thought Process

The current routing uses Mistral 7B (7B parameters, ~4.5 GB RAM) solely as a classifier to
pick one package from 2–5 candidates. This is architecturally disproportionate — a 1.5B model
can do the same task 5–8× faster on CPU. Additionally, small talk messages (greetings,
thanks) fall through to routing and produce cold "no match" responses.

The approach: replace the model (config-only change due to Semantic Kernel abstraction), add
inference parameters that were missing, reduce timeout, and design a small talk detection
layer that runs before routing. All documented as ADR + design docs.

## Implementation Plan

- [x] Create branch `plan/lightweight-llm-and-chitchat`
- [x] Write current routing analysis (docs/architecture/current-routing-explained.md)
- [x] Write ADR-0005 (docs/architecture/adr/0005-lightweight-llm.md)
- [x] Write chitchat design (docs/mvp/chitchat-design.md)
- [x] Create/update solution overview (docs/solution/freddy-mvp-solution-overview.md)
- [x] Update ADR README index
- [x] Update memory bank files
- [ ] Implementation phase (separate branch — next task)

## Progress Tracking

**Overall Status:** In Progress — 90% (documentation complete, implementation pending)

### Subtasks

| ID | Description | Status | Updated | Notes |
|----|-------------|--------|---------|-------|
| 2.1 | Create git branch | Complete | 2026-03-04 | `plan/lightweight-llm-and-chitchat` from main |
| 2.2 | Current routing analysis doc | Complete | 2026-03-04 | Stakeholder-friendly, covers model, latency, gaps |
| 2.3 | ADR-0005 lightweight LLM | Complete | 2026-03-04 | Qwen 2.5 1.5B recommended, config detailed |
| 2.4 | Chitchat design doc | Complete | 2026-03-04 | 5 categories, templates, Mermaid flow, metrics, checklist |
| 2.5 | Solution overview update | Complete | 2026-03-04 | v1.1 with small talk + lightweight LLM sections |
| 2.6 | ADR README update | Complete | 2026-03-04 | ADR-0005 added to index |
| 2.7 | Memory bank updates | Complete | 2026-03-04 | All 4 context files updated |
| 2.8 | Implementation (code) | Not Started | — | Separate phase per implementation checklist |

## Progress Log

### 2026-03-04

- Created branch `plan/lightweight-llm-and-chitchat` from main
- Researched current codebase: OllamaPackageRouter, CompositePackageRouter, FastPathRouter,
  DependencyInjection, appsettings — confirmed Mistral 7B with no inference parameters, 5min
  timeout, no small talk handling
- Created `docs/architecture/current-routing-explained.md` — explains current routing for
  non-AI-specialists, identifies 4 architectural gaps
- Created `docs/architecture/adr/0005-lightweight-llm.md` — proposes Qwen 2.5 1.5B, compared
  3 alternatives, detailed config recommendations
- Created `docs/mvp/chitchat-design.md` — complete small talk design with detection
  categories, templates, Mermaid flow, metrics targets, concrete implementation checklist
- Created `docs/solution/freddy-mvp-solution-overview.md` — v1.1 incorporating small talk
  layer and lightweight LLM throughout
- Updated ADR README index with ADR-0005
- Updated memory bank: activeContext, progress, systemPatterns, techContext
