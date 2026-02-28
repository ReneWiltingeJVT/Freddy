## Functionele Architectuur

### Chat Flow

```mermaid
sequenceDiagram
    actor User as Zorgmedewerker
    participant App as Freddy App (React PWA)
    participant API as Backend API
    participant AI as AI Service
    participant DB as Database

    User->>App: Typt vraag in chat
    App->>API: POST /api/v1/chat/conversations/{id}/messages
    API->>DB: Sla gebruikersbericht op
    API->>AI: Verwerk vraag
    AI-->>API: Antwoord + bronnen
    API->>DB: Sla antwoord + bronreferenties op
    API-->>App: ChatResponseDto (antwoord, bronnen, confidence)
    App-->>User: Toont antwoord met bronvermelding
```

### Intent Detection Flow

```mermaid
flowchart TD
    A[Gebruikersvraag ontvangen] --> B{Stap 1: Keyword matching}
    B -->|Match gevonden| C[FAQ Template opzoeken]
    C --> D{Confidence > 0.85?}
    D -->|Ja| E[Retourneer template-antwoord + bron]
    D -->|Nee| F[Ga naar RAG]

    B -->|Geen match| F[Stap 2: RAG Pipeline]
    F --> G[Embed vraag naar vector]
    G --> H[Similarity search in pgvector]
    H --> I{Relevante chunks gevonden?}
    I -->|Ja, score > 0.7| J[Construeer prompt met context]
    J --> K[LLM genereert antwoord]
    K --> L{Antwoord bevat bronverwijzing?}
    L -->|Ja| M[Retourneer antwoord + bronnen]
    L -->|Nee| N[Retourneer met disclaimer]

    I -->|Nee, score < 0.7| O[Geen relevant document gevonden]
    O --> P[Ik kan dit niet beantwoorden.<br/>Neem contact op met je leidinggevende.]
```

### Document Retrieval Flow (RAG)

```mermaid
flowchart LR
    subgraph Ingest [Document Ingestie - eenmalig]
        A[PDF/Word Document] --> B[Tekst extractie]
        B --> C[Chunking: 512 tokens, overlap 50]
        C --> D[Embedding generatie]
        D --> E[Opslag in pgvector]
    end

    subgraph Query [Query-tijd]
        F[Gebruikersvraag] --> G[Embed vraag]
        G --> H[Cosine similarity search]
        H --> I[Top-3 relevante chunks]
        I --> J[Prompt constructie]
        J --> K[LLM antwoord generatie]
        K --> L[Antwoord + bronvermelding]
    end
```

### Standaard Antwoord Flow (FAQ/Templates)

```mermaid
flowchart TD
    A[Gebruikersvraag] --> B[Normaliseer tekst]
    B --> C[Match tegen FAQ-index]
    C --> D{Exacte of fuzzy match?}
    D -->|Exact match| E[Retourneer template antwoord]
    D -->|Fuzzy match > 0.85| F[Retourneer template + bevestiging]
    D -->|Geen match| G[Doorsturen naar RAG pipeline]

    E --> H[Voeg bron toe: FAQ-ID + categorie]
    F --> H
    H --> I[Response naar gebruiker]
```

### Admin Upload Flow (Fase 3 — Toekomst)

```mermaid
sequenceDiagram
    actor Admin as Beheerder
    participant Web as Backoffice Web
    participant API as Backend API
    participant Proc as Document Processor
    participant DB as Database

    Admin->>Web: Upload protocol (PDF/Word)
    Web->>API: POST /api/v1/admin/documents
    API->>DB: Sla document metadata op
    API->>Proc: Start async verwerking
    Proc->>Proc: Extractie + chunking + embeddings
    Proc->>DB: Sla chunks + vectors op
    Proc-->>API: Verwerking compleet
    API-->>Web: Document status: geïndexeerd
```
