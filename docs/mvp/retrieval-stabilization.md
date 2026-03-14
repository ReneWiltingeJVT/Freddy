# MVP Retrieval Stabilization

> **Status:** Geïmplementeerd — maart 2026
> **Branch:** `feature/mvp-retrieval-stabilization`
> **Context:** Vervangt de tijdelijke Conversational RAG Light-architectuur waarbij `llama3.1:8b` de kritieke pad blokkeerde.

---

## Probleemanalyse

### Symptoom

Een simpele vraag als `"ik heb een vraag over de voedselbank"` gaf consequent:

```
Het genereren van een antwoord duurde te lang
```

Daarna werden wel documenten gevonden, maar zonder uitleg.

### Rootcause

De architectuur van `feature/freddy-conversational-rag` riep **altijd** `ChatResponseGenerator` aan — ook wanneer het pakket al gevonden was. `ChatResponseGenerator` gebruikte het `llama3.1:8b` model met:

- Timeout: **30 seconden**
- Context: **alle 30–40 gepubliceerde pakketten** als system prompt injected
- Max tokens: **1024**

Op een standaard machine genereert `llama3.1:8b` ~10–20 tokens/seconde. Een 1024-token response duurt 50–100 seconden — ruim over de 30s timeout. Dit veroorzaakte **elke keer** een `TaskCanceledException`.

Bijkomend probleem: de description scoring in `FastPathRouter` was een "last resort" guard — het pakket "Voedselbank" met het woord "voedselbank" in de description maar niet in de tags scoorde ≤ 0.3, waardoor de vraag in het ambiguïteitsgebied belandde en ook de `OllamaPackageRouter` werd aangeroepen.

---

## Oplossing

### Architectuur

```
Gebruikersvraag
  ↓
SmallTalk check              → template response (ongewijzigd)
  ↓
PendingState dispatch        → bevestigingsafhandeling (ongewijzigd)
  ↓
ClientDetectie               → cliënt uit bericht of conversatiecontext
  ↓
Package load + FastPath      → deterministisch scoren in geheugen (<10ms)
  ↓
CompositePackageRouter       → high-confidence / bevestiging / LLM disambiguation
  ↓
Pakket gevonden?
  ├── JA  → PackageResponseFormatter  (<5ms, geen LLM)
  │         ↓ AppendDocumentOffer
  └── NEE → ChatResponseGenerator     (qwen2.5:1.5b, 8s timeout, slim prompt)
```

### Kernprincipe: LLM staat nooit in het kritieke pad van pakketophaling

Het ophalen én formatteren van een pakket is volledig deterministisch. **Geen LLM-aanroep.** De LLM wordt alleen ingezet voor overzichtsvragen en ambigue vragen waarbij geen pakket gevonden kon worden.

---

## Geïmplementeerde Wijzigingen

### 1. `PackageResponseFormatter` (nieuw)

**Interface:** `IPackageResponseFormatter` (Application layer)  
**Implementatie:** `PackageResponseFormatter` (Infrastructure layer)

Formatteert een gevonden pakket in leesbaar Nederlands:

```
Ik heb het volgende pakket gevonden:

**Voedselbank**

Protocol voor het aanvragen van voedselbankpakketten.

Stap 1: Controleer of cliënt in aanmerking komt.
Stap 2: Vul het aanvraagformulier in.
...
```

- Geen I/O, geen LLM, geen async
- Uitvoeringstijd: < 5ms

### 2. `SendMessageCommandHandler` — routinglogica bijgewerkt

Bij een gevonden pakket (high-confidence of bevestigd):

```csharp
// Vroeger: LLM genereerde het antwoord (30s timeout)
ChatResponseResult chatResult = await chatResponseGenerator.GenerateAsync(...);

// Nu: Deterministische formatter (<5ms, geen timeout-risico)
string formattedResponse = packageResponseFormatter.Format(matchedPackage);
return await AppendDocumentOfferAsync(conversation, matchedPackage, formattedResponse, ct);
```

`KnowledgeContextBuilder` en `ChatResponseGenerator` worden **niet** meer aangeroepen bij een gevonden pakket.

### 3. `ChatResponseGenerator` — van `llama3.1:8b` naar `qwen2.5:1.5b`

| | Oud | Nieuw |
|---|---|---|
| Model | `llama3.1:8b` (keyed: `"chat"`) | `qwen2.5:1.5b` (keyed: `"classifier"`) |
| Timeout | 30s | 8s |
| Context | Alle pakketten + clients + persoonlijke plannen + matched pakket | Alleen pakket-titels + beschrijvingen + cliëntenlijst |
| Gebruik | Altijd | Alleen bij geen match (overzichts-/ambigue vragen) |

De system prompt is ingekort van ~2000 naar ~400 tekens voor overview queries.

### 4. `FastPathRouter` — description scoring verbeterd

De `ScoreDescription`-methode werd voorheen alleen aangeroepen als `score < 0.3` (last-resort). Nu altijd:

| Overlap woorden (≥4 chars, na stopword-filter) | Oud score | Nieuw score |
|---|---|---|
| ≥ 4 | 0.3 (last resort) | **0.5** (boven high-confidence threshold) |
| ≥ 2 | 0.3 (last resort) | **0.4** (boven ambiguity floor, directe match) |
| < 2 | — | 0.0 |

Effect: Een vraag over "voedselbank" wordt nu gescoord via de description als `"voedselbank"` in de description staat maar niet in de tags — score 0.4 → directe route, geen LLM disambiguation nodig.

### 5. `AIOptions` / appsettings

```json
{
  "AI": {
    "TimeoutSeconds": 8,
    "MaxTokens": 512
  }
}
```

---

## Prestaties na implementatie

| Scenario | Eerder | Nu |
|---|---|---|
| Pakketvraag (hoge zekerheid) | ~30s timeout | < 500ms |
| Pakketvraag (na bevestiging) | ~30s timeout | < 500ms |
| Overzichtsvraag ("hoeveel protocollen zijn er?") | ~30s timeout | < 8s |
| Small talk ("hoi") | < 1ms | < 1ms (ongewijzigd) |
| Ambigue vraag zonder match | ~30s timeout | < 8s |

---

## Testdekking

153 tests groen na implementatie:

- 80 tests in `Freddy.AI.Tests` (OverviewQueryDetector — ongewijzigd)
- 73 tests in `Freddy.Application.Tests` (handler, routers, FastPath scoring)
  - `Score_DescriptionOverlap_Returns02`: assertie bijgewerkt van 0.3 → 0.4

---

## Toekomstige stappen (fase 2)

- `llama3.1:8b` blijft geïnstalleerd voor toekomstige "enhanced mode"
- Conversational tone in formatter: optioneel via `IChatResponseFormatter` strategy
- pgvector RAG: alleen nodig als pakketinhoud > 5000 woorden of > 40 pakketten
