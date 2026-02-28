## AI / RAG Guidelines

### Core Principles

1. **Grounded responses only**: AI answers must be based on retrieved document context
2. **Always cite sources**: every RAG response includes document reference
3. **Fail safely**: if unsure, say "I don't know" rather than guess
4. **No medical advice**: never advise on individual patient situations
5. **Transparent**: users know they're talking to an AI

### Prompt Engineering

#### System Prompt Rules

- Externalize system prompt in configuration (not hardcoded)
- Include explicit behavioral constraints
- Specify response language (Dutch, B1 level)
- Include grounding instruction: "Answer ONLY based on provided context"
- Include refusal instruction for out-of-scope questions

#### Parameters

| Parameter | Value | Rationale |
|-----------|-------|-----------|
| Temperature | 0.1 | Minimize creativity, maximize factual accuracy |
| Max tokens | 1024 | Prevent runaway generation |
| Top-p | 0.9 | Slightly constrained sampling |
| Frequency penalty | 0.0 | No need for diversity in factual answers |

### RAG Pipeline

#### Document Ingestion

- Chunk size: 512 tokens
- Overlap: 50 tokens
- Embedding model: nomic-embed-text (768 dimensions)
- Store: pgvector with cosine distance

#### Query Time

- Embed user question with same model
- Retrieve top-3 chunks by cosine similarity
- Minimum similarity threshold: 0.7
- Below threshold: return "cannot answer" response

#### Prompt Construction

```
[System prompt with rules]

CONTEXT:
---
[Chunk 1 - Source: {document_title}, Section: {section}]
{chunk_content}
---
[Chunk 2 - Source: {document_title}, Section: {section}]
{chunk_content}
---

USER QUESTION:
{user_question}
```

### Quality Assurance

#### Golden Test Set

- Maintain 50+ question-answer pairs with expected outcomes
- Categories: FAQ match, RAG match, refusal, edge cases
- Run on every prompt change
- Track: correctness, source accuracy, refusal rate

#### Monitoring

- Log: question, retrieved chunks, similarity scores, generated answer
- Track "I don't know" rate (healthy: 10-20%)
- Track user feedback (thumbs up/down) in Phase 2
- Monthly manual review of random sample (20 conversations)

### Safety Rules

| Rule | Implementation |
|------|---------------|
| No patient advice | System prompt + keyword detection |
| Source required | Post-processing validation |
| Confidence gate | Similarity threshold 0.7 |
| Disclaimer | Appended to every RAG response |
| Scope limitation | Only answer about uploaded documents |
| Refusal response | Standardized "I cannot answer" message |
