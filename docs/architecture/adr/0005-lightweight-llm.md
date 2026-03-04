# ADR-0005: Lichtgewicht LLM voor Routing

**Status:** Proposed
**Datum:** 2026-03-04
**Besluitnemers:** Architectuurteam

---

## Context

Freddy gebruikt een tweelaans routingsysteem:

- **Fast-path:** deterministische trefwoordmatching (< 10 ms, ~80%+ van alle vragen)
- **Slow-path:** LLM-classificatie via Ollama (alleen bij dubbelzinnigheid tussen pakketten)

Het huidige slow-path model is **Mistral 7B** (`mistral:7b`). Dit model is ontworpen voor
algemene taalverwerking en heeft 7 miljard parameters. De enige taak die het binnen Freddy
uitvoert is **classificatie**: kies 1 pakket uit een lijst van 2–5 kandidaten en geef een
JSON-antwoord terug.

### Probleem

| Aspect | Huidige situatie |
|--------|-----------------|
| Slow-path latency (CPU) | 3–10 seconden |
| RAM-gebruik | ~4,5 GB (Q4) |
| Inference-parameters | Niet geconfigureerd (geen temperature, geen max tokens) |
| HttpClient timeout | 5 minuten (hardcoded) |
| Evenredigheid | 7B parameters voor ~80 tokens output |

**Kernprobleem:** het model is te groot en te traag voor een eenvoudige classificatietaak.
Precies bij dubbelzinnige vragen — het moment dat de gebruiker al twijfelt — is de responstijd
het hoogst. Dit ondermijnt de MVP-ervaring.

Naast het model ontbreken er generatie-instellingen (`Temperature`, `MaxTokens`) waarmee
de output begrensd en deterministischer gemaakt kan worden. De `instructions.md` specificeert
`Temperature: 0.1` en `Max tokens: 512`, maar deze waarden worden niet doorgegeven aan de
Semantic Kernel `IChatCompletionService`.

---

## Overwogen Opties

### Optie A — Qwen 2.5 1.5B (`qwen2.5:1.5b`)

| Eigenschap | Waarde |
|------------|--------|
| Parameters | 1,5 miljard |
| RAM (Q4) | ~1,0 GB |
| Download | ~0,9 GB |
| Snelheid (CPU) | ~30–50 tokens/seconde |
| JSON output | Goed — getraind op instructie-opvolging |
| Licentie | Apache 2.0 |

**Voordelen:** zeer klein, snel, goede instructie-opvolging, breed getest in classificatietaken.
**Nadelen:** bij complexe nuance mogelijk minder accuraat dan grotere modellen.

### Optie B — Phi-3 Mini 3.8B (`phi3:mini`)

| Eigenschap | Waarde |
|------------|--------|
| Parameters | 3,8 miljard |
| RAM (Q4) | ~2,4 GB |
| Download | ~2,3 GB |
| Snelheid (CPU) | ~15–25 tokens/seconde |
| JSON output | Uitstekend — sterk in gestructureerde taken |
| Licentie | MIT |

**Voordelen:** Microsoft-model, extreem goed in gestructureerde output en redenatie.
**Nadelen:** 2,5× zo groot als Qwen 2.5, langzamer op CPU.

### Optie C — Gemma 2 2B (`gemma2:2b`)

| Eigenschap | Waarde |
|------------|--------|
| Parameters | 2,0 miljard |
| RAM (Q4) | ~1,5 GB |
| Download | ~1,4 GB |
| Snelheid (CPU) | ~25–35 tokens/seconde |
| JSON output | Goed |
| Licentie | Gemma Terms of Use |

**Voordelen:** Google Research-model, goede balans grootte/kwaliteit.
**Nadelen:** restrictievere licentie, minder getest in Ollama-ecosysteem voor classificatie.

### Optie D — Model behouden, alleen parameters tunen

Mistral 7B behouden maar `Temperature: 0.1`, `num_predict: 128`, `num_ctx: 2048` instellen.

**Voordelen:** geen modelwissel nodig, minder risico.
**Nadelen:** latency blijft 3–10 seconden, RAM-gebruik blijft ~4,5 GB. Lost het kernprobleem
niet op.

---

## Besluit

**Optie A — Qwen 2.5 1.5B** als primair routing-model, met de volgende configuratie:

| Parameter | Waarde | Toelichting |
|-----------|--------|-------------|
| `AI:ModelId` | `qwen2.5:1.5b` | Vervangt `mistral:7b` |
| `temperature` | `0.1` | Bijna deterministisch — gewenst voor classificatie |
| `num_predict` | `128` | JSON response is ~60–80 tokens; harde cap voorkomt runaway generatie |
| `num_ctx` | `2048` | Ruim genoeg voor systeem-prompt + 5 kandidaten (~800 tokens) |
| `HttpClient.Timeout` | `15 seconden` | was 5 minuten; fail-fast bij problemen |

**Fallback:** indien Qwen 2.5 1.5B onvoldoende nauwkeurig blijkt bij evaluatie, opschalen
naar Phi-3 Mini 3.8B. De Semantic Kernel abstractie maakt een modelwissel triviaal (alleen
`AI:ModelId` in `appsettings.json` aanpassen + `ollama pull`).

---

## Motivatie

### Waarom dit voldoende is voor classificatie

De slow-path LLM voert uitsluitend deze taak uit:

1. Lees een systeem-prompt (~200 tokens)
2. Lees een gebruikersvraag (~20 tokens)
3. Lees 2–5 kandidaat-pakketten met naam en beschrijving (~100–300 tokens)
4. Kies het best passende pakket
5. Geef antwoord als JSON (~60–80 tokens)

Dit is een **gesloten classificatietaak** met gestructureerde output. Onderzoek toont aan dat
modellen in de 1B–3B range deze taken betrouwbaar uitvoeren wanneer:

- De taak duidelijk gedefinieerd is (dat is het: kies 1 uit N)
- De output gestructureerd is (JSON-format geforceerd in prompt)
- De temperature laag is (0.1 — bijna deterministisch)
- De output kort is (< 128 tokens)

Een 7B model voegt hier geen waarde toe — het extra vermogen wordt niet benut.

### Waarom dit sneller is

| Model | Tokens/seconde (CPU) | Tijd voor ~80 tokens output |
|-------|---------------------|----------------------------|
| Mistral 7B | 5–10 tok/s | **8–16 seconden** |
| Qwen 2.5 1.5B | 30–50 tok/s | **1,5–2,5 seconden** |

**Verwachte latencyverbetering slow-path: 5–8× sneller.**

Gecombineerd met `num_predict: 128` (voorkomt lange output) en `temperature: 0.1` (minder
sampling overhead), verwachten we een slow-path latency van **< 1,5 seconden** op een
standaard VPS.

### Waarom dit goedkoper is

| Eigenschap | Mistral 7B | Qwen 2.5 1.5B | Besparing |
|------------|-----------|---------------|-----------|
| RAM | ~4,5 GB | ~1,0 GB | **3,5 GB vrijgemaakt** |
| Download | ~4,1 GB | ~0,9 GB | Snellere provisioning |
| CPU-belasting per request | Hoog | Laag | Meer headroom voor overige services |

Op een VPS met 8 GB RAM is het verschil significant: 4,5 GB voor alleen het AI-model versus
1,0 GB laat veel meer ruimte voor PostgreSQL, de .NET applicatie en logging.

### Waarom dit veiliger is

1. **Kleiner model = minder kans op creatieve afwijking.** Een 1.5B model met lage temperature
   volgt instructies strikt — het mist de capaciteit voor uitgebreide "hallucinate".
2. **`num_predict: 128` voorkomt runaway generatie.** Het model stopt na maximaal 128 tokens,
   ongeacht de invoer. De huidige situatie heeft geen limiet.
3. **Kortere timeout (15s vs 5min) detecteert problemen sneller.** De gebruiker krijgt binnen
   15 seconden een foutmelding in plaats van 5 minuten te wachten.
4. **Het model genereert nog steeds geen antwoorden.** De fundamentele veiligheidsmaatregel
   (model kiest alleen een pakket, genereert geen zorgadvies) blijft volledig intact.

---

## Consequenties

### Positief

- Slow-path latency daalt van 3–10 seconden naar < 1,5 seconde
- RAM-gebruik AI-model daalt met ~3,5 GB
- Inference-parameters worden expliciet geconfigureerd (was: niet ingesteld)
- Timeout wordt realistisch (15 seconden vs 5 minuten)
- Modelwissel is triviaal dankzij Semantic Kernel abstractie (alleen config)
- Fast-path blijft ongewijzigd (< 10 ms)

### Negatief

- Classificatie-nauwkeurigheid kan marginaal lager zijn bij complexe dubbelzinnigheid
- Validatie nodig met bestaande test suite + handmatige evaluatie
- Team moet `ollama pull qwen2.5:1.5b` draaien op alle development machines

### Risico's

| Risico | Impact | Mitigatie |
|--------|--------|-----------|
| Model niet nauwkeurig genoeg | Medium | Opschalen naar Phi-3 Mini 3.8B (config change) |
| JSON output onbetrouwbaar | Medium | Bestaande JSON-parsing + fallback in `OllamaPackageRouter` vangt dit op |
| Ollama ondersteunt model niet goed | Laag | Qwen 2.5 is een van de meest gebruikte modellen in Ollama |

### Acties

1. `ollama pull qwen2.5:1.5b` op development en staging
2. `appsettings.json`: `AI:ModelId` → `qwen2.5:1.5b`
3. `OllamaPackageRouter`: voeg `PromptExecutionSettings` toe met `Temperature=0.1`,
   `MaxTokens=128`
4. `DependencyInjection.cs`: `HttpClient.Timeout` → `TimeSpan.FromSeconds(15)`
5. Evaluatie met bestaande 17 routingtests + handmatige test met seed data
6. Documentatie bijwerken (solution overview, tech context)

---

## Gerelateerde documenten

- [ADR-0001: LLM Provider Keuze](0001-llm-provider-choice.md) — oorspronkelijke keuze voor
  Ollama + Mistral 7B
- [Huidige Routing Analyse](../current-routing-explained.md) — gedetailleerde beschrijving
  huidige situatie
- [Chitchat Design](../../mvp/chitchat-design.md) — aanvullende UX-verbetering via small talk
  detectie
