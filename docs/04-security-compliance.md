## Security & Compliance

### Context

Freddy verwerkt **geen patiëntgegevens** — uitsluitend zorgprotocollen en
organisatiedocumenten. Alle AI-verwerking vindt lokaal plaats op de eigen server.

### Authenticatie

| Aspect | MVP | Toekomst |
|--------|-----|---------|
| Methode | ASP.NET Identity + JWT Bearer | Azure AD B2C / Keycloak |
| Token | Access (15 min) + Refresh (7 dagen) | OIDC compliant |
| MFA | Optioneel | Verplicht |
| Kosten | €0 | €50+/maand |

### Autorisatiemodel

| Rol | Rechten | Fase |
|-----|--------|------|
| User | Chat, eigen conversaties | MVP |
| TeamLead | User + team conversaties | MVP |
| Admin | Alles + document upload + gebruikersbeheer | Fase 3 |
| SuperAdmin | Admin + tenant beheer | Fase 4 |

Claims-based authorization via ASP.NET Core policies.

### Audit Logging

| Event | Gelogd | Opslag |
|-------|--------|--------|
| Login/logout | ✅ | Database |
| Chat berichten (metadata) | ✅ | Database |
| Document uploads | ✅ | Database |
| Admin acties | ✅ | Database |
| API errors | ✅ | Seq |

### Dataclassificatie

| Categorie | Classificatie | Maatregelen |
|-----------|-------------|-------------|
| Protocollen | Vertrouwelijk | Toegangscontrole, encryptie at rest |
| Gebruikersgegevens | Persoonsgegevens (AVG) | Minimale verzameling, verwijderrecht |
| Chatgeschiedenis | Intern | Retentiebeleid (90 dagen) |
| Systeemdata | Intern | Cleanup (30 dagen) |

### Encryptie

| Laag | Maatregel |
|------|----------|
| Transport | TLS 1.3 (Nginx) |
| Data at rest | Encrypted volumes (Hetzner) |
| Wachtwoorden | Argon2id (ASP.NET Identity) |
| JWT tokens | RS256 signing |
| Backups | AES-256 |

### AVG Risicoanalyse

| Verwerking | Grondslag | Risico | Maatregel |
|-----------|-----------|--------|----------|
| Gebruikersaccount | Gerechtvaardigd belang | Laag | Recht op verwijdering |
| Chatgeschiedenis | Gerechtvaardigd belang | Laag-Medium | Retentiebeleid, geen patiëntdata |
| LLM verwerking | Gerechtvaardigd belang | Medium | Lokaal model, geen externe API |
| Logging | Gerechtvaardigd belang | Laag | Anonimiseer na 30 dagen |

### Multi-tenant Voorbereiding

- `TenantId` kolom op alle entiteiten (nullable in MVP)
- Global query filter in EF Core
- Tenant resolution via JWT claim
- Row-level security als extra laag
