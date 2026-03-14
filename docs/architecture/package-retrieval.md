# Package Retrieval Architecture

> **Bijgewerkt:** maart 2026  
> **Status:** MVP-productie

---

## Overzicht

Het ophalen en tonen van pakketten verloopt volledig deterministisch — zonder LLM in het kritieke pad. De LLM wordt alleen ingezet als laatste stap bij overzichtsvragen of vragen zonder match.

```
Gebruikersbericht
  │
  ▼
┌─────────────────────┐
│  SmallTalkDetector  │ ─── Begroeting/bedankje ──▶ Template response (hardcoded, <1ms)
└─────────────────────┘
  │ Inhoudelijke vraag
  ▼
┌─────────────────────┐
│  PendingState check │ ─── AwaitingConfirmation/Delivery ──▶ State handlers
└─────────────────────┘
  │ None (nieuw gesprek of nieuwe vraag)
  ▼
┌─────────────────────┐
│  ClientDetector     │     Detecteert cliëntnamen in het bericht
└─────────────────────┘
  │
  ▼
┌─────────────────────┐
│  Package load       │     GetAllPublishedAsync() [+ ClientPackages indien cliënt]
│  (DB query)         │     Batch-load document names (één query)
└─────────────────────┘
  │
  ▼
┌──────────────────────────────────────┐
│  CompositePackageRouter              │
│                                      │
│  1. FastPathRouter.Score()  <10ms    │
│     - Exact title match      → 1.0   │
│     - Title in message       → 0.7   │
│     - Tag/Synonym exact       → 0.6  │
│     - Document name match    → 0.5   │
│     - Description ≥4 words   → 0.5   │
│     - Content keywords ≥3    → 0.4   │
│     - Description ≥2 words   → 0.4   │
│     - N-gram similarity ≥0.3  → 0.35 │
│     - Tag/Synonym partial    → 0.3   │
│     + PersonalPlan boost     → +0.1  │
│                                      │
│  2. Score ≥ 0.6 → Direct match       │
│     Score 0.3–0.6, één kandidaat     │
│              → NeedsConfirmation     │
│     Score 0.3–0.6, meerdere          │
│              → OllamaPackageRouter   │
│     Score 0 → OllamaPackageRouter    │
└──────────────────────────────────────┘
  │
  ▼
Pakket gevonden?
  │
  ├── JA (hoge zekerheid, ≥ 0.6)
  │     │
  │     ├── RequiresConfirmation + lage zekerheid
  │     │     → Bevestigingsvraag ("Bedoel je X?")
  │     │       → PendingState = AwaitingPackageConfirmation
  │     │
  │     └── Direct leveren
  │           → PackageResponseFormatter.Format(package)  <5ms
  │           → AppendDocumentOffer (indien documenten beschikbaar)
  │
  └── NEE (geen match)
        → KnowledgeContextBuilder.BuildAsync()  [gecached 5 min]
        → ChatResponseGenerator.GenerateAsync() [qwen2.5:1.5b, 8s timeout]
        → Overzichtsantwoord of "ik weet het niet"
```

---

## Componenten

### `FastPathRouter`

Deterministisch scoringsalgoritme. Geen I/O, geen LLM. Scoort elk pakket op basis van:

1. **Titel match** (hoogste prioriteit)
2. **Tag en Synonym match** (exact en partieel)
3. **Document naam match**
4. **Description woordoverlap** (altijd gescoord — *niet* als last resort)
5. **Content woordoverlap** (na stopword-filtering)
6. **N-gram bigraamsimilariteit** (voor samengestelde Nederlandse woorden)
7. **PersonalPlan boost** (+0.1 bij clientcontext)

**Drempels:**

| Score | Actie |
|---|---|
| ≥ 0.6 | Direct naar `PackageResponseFormatter` |
| 0.3 – 0.6 | Bevestiging of `OllamaPackageRouter` |
| < 0.3 | `OllamaPackageRouter` (zero-match recovery) |

### `OllamaPackageRouter`

Lichtgewicht LLM-router (`qwen2.5:1.5b`, timeout 8s). Ingezet voor:

- Meerdere kandidaten in het ambiguïteitsgebied (0.3–0.6)
- Zero-match recovery (geen enkele kandidaat ≥ 0.3)

Geeft JSON terug met `chosenPackageId`, `confidence`, `needsConfirmation`. Bij timeout of fout: terugval op top fast-path kandidaat met bevestigingsvraag.

### `PackageResponseFormatter`

Deterministisch formatter. Geen LLM, geen I/O. Uitvoering < 5ms.

Uitvoerformaat:

```markdown
Ik heb het volgende pakket gevonden:

**[Titel]**

[Beschrijving]

[Inhoud]
```

### `ChatResponseGenerator`

Wordt **alleen** ingezet bij geen pakketmatch. Gebruikt `qwen2.5:1.5b` (keyed: `"classifier"`) met een slim system prompt dat alleen pakket-titels, beschrijvingen en cliëntenlijst injecteert (geen volledige inhoud). Timeout: 8s.

### `KnowledgeContextBuilder`

Laadt en formatteert alle gepubliceerde pakketten en actieve cliënten voor de LLM system prompt. Gecached in `IMemoryCache` (5 minuten TTL). Wordt **alleen** aangeroepen bij geen pakketmatch.

---

## Cliëntcontext

Wanneer een cliëntnaam wordt herkend (via `ClientDetector`):

1. Beide pakketsets worden geladen: algemeen (zonder `PersonalPlan`) + persoonlijke plannen van de cliënt
2. `FastPathRouter` geeft persoonlijke plannen een `+0.1` boost zodat ze makkelijker opgepikt worden
3. Het gevonden plan wordt opgemaakt via `PackageResponseFormatter`

---

## Overzichtsvragen

Voor vragen als "hoeveel protocollen zijn er?":

- `FastPathRouter` scoort niets (geen pakketspecifieke termen)
- Pad valt door naar `ChatResponseGenerator` (qwen2.5:1.5b)
- `KnowledgeContextBuilder` injecteert pakket-titels + beschrijvingen als overzicht
- LLM beantwoordt de vraag op basis van de gecachede kennis

---

## Prestaties

| Stap | Tijdschatting |
|---|---|
| SmallTalk | < 1ms |
| DB load (GetAllPublishedAsync) | 10–50ms |
| FastPathRouter.Score() | < 10ms |
| PackageResponseFormatter | < 5ms |
| OllamaPackageRouter (bij twijfel) | < 8s |
| ChatResponseGenerator (geen match) | < 8s |

**Pakketvraag (high-confidence):** totaal < 500ms  
**Overzichts-/ambigue vraag:** totaal < 8s
