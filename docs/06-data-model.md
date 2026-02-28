## Conceptueel Datamodel

### Entiteiten

| Entiteit | Beschrijving |
|----------|-------------|
| Organization | Zorginstelling (multi-tenant voorbereiding) |
| User | Zorgmedewerker |
| Conversation | Chat-sessie |
| Message | Individueel bericht |
| MessageSource | Bronverwijzing bij AI-antwoord |
| Document | Protocol/document |
| DocumentChunk | Tekst-fragment met embedding |
| FaqTemplate | Gestructureerd vraag-antwoord paar |
| AuditLog | Audit trail |
| Category | Documentcategorie |

### ERD

```mermaid
erDiagram
    Organization ||--o{ User : has
    Organization ||--o{ Document : owns
    Organization ||--o{ FaqTemplate : owns

    User ||--o{ Conversation : creates
    Conversation ||--o{ Message : contains

    Message ||--o{ MessageSource : references
    MessageSource }o--|| DocumentChunk : points_to

    Document ||--o{ DocumentChunk : split_into
    Document }o--|| Category : belongs_to

    User ||--o{ AuditLog : generates

    Organization {
        uuid Id PK
        string Name
        string Slug
        jsonb Settings
        boolean IsActive
        datetime CreatedAt
    }

    User {
        uuid Id PK
        uuid OrganizationId FK
        string Email
        string DisplayName
        string Role
        string PasswordHash
        boolean IsActive
        datetime CreatedAt
    }

    Conversation {
        uuid Id PK
        uuid UserId FK
        string Title
        datetime CreatedAt
        datetime LastMessageAt
    }

    Message {
        uuid Id PK
        uuid ConversationId FK
        string Role
        text Content
        datetime CreatedAt
    }

    MessageSource {
        uuid Id PK
        uuid MessageId FK
        uuid DocumentChunkId FK
        float RelevanceScore
    }

    Document {
        uuid Id PK
        uuid OrganizationId FK
        string Title
        string FileName
        string FileType
        string CategoryId
        string Status
        datetime UploadedAt
        datetime ProcessedAt
    }

    DocumentChunk {
        uuid Id PK
        uuid DocumentId FK
        text Content
        int ChunkIndex
        vector Embedding
    }

    FaqTemplate {
        uuid Id PK
        uuid OrganizationId FK
        text Question
        text Answer
        jsonb Keywords
        string Category
        boolean IsActive
        datetime CreatedAt
    }

    AuditLog {
        uuid Id PK
        uuid UserId FK
        string Action
        string EntityType
        string EntityId
        jsonb Details
        datetime Timestamp
    }

    Category {
        uuid Id PK
        string Name
        string Description
        int SortOrder
    }
```
